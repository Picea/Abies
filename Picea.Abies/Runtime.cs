using System.Diagnostics;
using Picea.Abies.DOM;
using Picea.Abies.Html;
using Picea.Abies.Subscriptions;
#if DEBUG
using Picea.Abies.Debugger;
#endif

namespace Picea.Abies;

/// <summary>
/// Applies a list of DOM patches to the platform's rendering surface.
/// </summary>
public delegate void Apply(IReadOnlyList<Patch> patches);

/// <summary>
/// Pure functions for diffing <see cref="HeadContent"/> arrays between renders.
/// </summary>
public static class HeadDiff
{
    public static IReadOnlyList<Patch> Diff(
        ReadOnlySpan<HeadContent> oldHead,
        ReadOnlySpan<HeadContent> newHead)
    {
        if (oldHead.Length == 0 && newHead.Length == 0)
            return [];

        if (oldHead.Length == 0)
        {
            var adds = new Patch[newHead.Length];
            for (var i = 0; i < newHead.Length; i++)
                adds[i] = new AddHeadElement(newHead[i]);
            return adds;
        }

        if (newHead.Length == 0)
        {
            var removes = new Patch[oldHead.Length];
            for (var i = 0; i < oldHead.Length; i++)
                removes[i] = new RemoveHeadElement(oldHead[i].Key);
            return removes;
        }

        var oldByKey = new Dictionary<string, HeadContent>(oldHead.Length);
        for (var i = 0; i < oldHead.Length; i++)
            oldByKey[oldHead[i].Key] = oldHead[i];

        var patches = new List<Patch>();
        var seenKeys = new HashSet<string>(newHead.Length);

        for (var i = 0; i < newHead.Length; i++)
        {
            var item = newHead[i];
            seenKeys.Add(item.Key);

            if (oldByKey.TryGetValue(item.Key, out var existing))
            {
                if (!existing.Equals(item))
                    patches.Add(new UpdateHeadElement(item));
            }
            else
            {
                patches.Add(new AddHeadElement(item));
            }
        }

        for (var i = 0; i < oldHead.Length; i++)
        {
            if (!seenKeys.Contains(oldHead[i].Key))
                patches.Add(new RemoveHeadElement(oldHead[i].Key));
        }

        return patches;
    }
}

/// <summary>
/// The MVU runtime: wires the Automaton kernel to View, Diff, and Subscriptions.
/// </summary>
public sealed class Runtime<TProgram, TModel, TArgument> : IDisposable
#if DEBUG
    , IHotReloadRuntime
#endif
    where TProgram : Program<TModel, TArgument>
{
    private static readonly ActivitySource _activitySource = new("Picea.Abies.Runtime");

    private AutomatonRuntime<TProgram, TModel, Message, Command, TArgument> _core = null!;
    private readonly Apply _apply;
    private readonly Action<string>? _titleChanged;
    private readonly Action<NavigationCommand>? _navigationExecutor;
    private readonly Action<SubscriptionFault>? _subscriptionFaulted;
    private readonly HandlerRegistry _handlerRegistry;
    private readonly bool _replay;
    private readonly string _viewCacheScope = $"runtime:{Guid.CreateVersion7()}";
    private readonly Lock _renderGate = new();
    private Document? _currentDocument;
    private SubscriptionState _subscriptionState = SubscriptionState.Empty;
#if DEBUG
    private IDisposable? _hotReloadRegistration;
    private DebuggerMachine? _debuggerMachine;
#endif

    public TModel Model => _core.State;

    public Document? CurrentDocument => _currentDocument;

    public HandlerRegistry Handlers => _handlerRegistry;

#if DEBUG
    public DebuggerMachine? Debugger => _debuggerMachine;
#endif

    private Runtime(
        Apply apply,
        Action<string>? titleChanged,
        Action<NavigationCommand>? navigationExecutor,
        Action<SubscriptionFault>? subscriptionFaulted,
        bool replay) =>
        (_apply, _titleChanged, _navigationExecutor, _subscriptionFaulted, _handlerRegistry, _replay) =
            (apply, titleChanged, navigationExecutor, subscriptionFaulted, new HandlerRegistry(), replay);

#if DEBUG
    public void UseDebugger(int capacity = 10000)
    {
        _debuggerMachine = new DebuggerMachine(capacity);
        DebuggerRuntimeRegistry.CurrentDebugger = _debuggerMachine;
    }
#endif

    private ValueTask<Result<Unit, PipelineError>> Observe(TModel state, Message _, Command __)
    {
        Render(state);

        return PipelineResult.Ok;
    }

    private void Render(TModel state)
    {
        using var renderActivity = _activitySource.StartActivity("Picea.Abies.Render");

        lock (_renderGate)
        {
            Document newDocument;
            using (Elements.EnterViewCacheScope(_viewCacheScope))
            {
                newDocument = TProgram.View(state);
            }

            var patches = Operations.Diff(_currentDocument?.Body, newDocument.Body);

            var headPatches = HeadDiff.Diff(
                _currentDocument?.Head ?? [],
                newDocument.Head);

            List<Patch>? mergedPatches = null;
            if (headPatches.Count > 0)
            {
                mergedPatches = [.. patches, .. headPatches];
            }

            var allPatches = mergedPatches is not null
                ? mergedPatches
                : patches;

            UpdateHandlerRegistry(allPatches);

            if (allPatches.Count > 0)
            {
                _apply(allPatches);
            }

            if (_currentDocument is null || _currentDocument.Title != newDocument.Title)
            {
                _titleChanged?.Invoke(newDocument.Title);
            }

            _currentDocument = newDocument;

            if (!_replay)
            {
                var desiredSubscriptions = TProgram.Subscriptions(state);
                _subscriptionState = SubscriptionManager.Update(
                    _subscriptionState,
                    desiredSubscriptions,
                    DispatchFromSubscription,
                    ObserveSubscriptionFault);
            }

            renderActivity?.SetTag("abies.patches", patches.Count);
        }

        renderActivity?.SetStatus(ActivityStatusCode.Ok);
    }

    private void UpdateHandlerRegistry(IReadOnlyList<Patch> patches)
    {
        foreach (var patch in patches)
        {
            switch (patch)
            {
                case AddHandler p:
                    _handlerRegistry.Register(p.Handler);
                    break;

                case RemoveHandler p:
                    _handlerRegistry.Unregister(p.Handler.CommandId);
                    break;

                case UpdateHandler p:
                    _handlerRegistry.Unregister(p.OldHandler.CommandId);
                    _handlerRegistry.Register(p.NewHandler);
                    break;

                case AddChild p:
                    _handlerRegistry.RegisterHandlers(p.Child);
                    break;

                case AddRoot p:
                    _handlerRegistry.RegisterHandlers(p.Element);
                    break;

                case ReplaceChild p:
                    _handlerRegistry.UnregisterHandlers(p.OldElement);
                    _handlerRegistry.RegisterHandlers(p.NewElement);
                    break;

                case RemoveChild p:
                    _handlerRegistry.UnregisterHandlers(p.Child);
                    break;

                case ClearChildren p:
                    foreach (var child in p.OldChildren)
                    {
                        _handlerRegistry.UnregisterHandlers(child);
                    }
                    break;

                case SetChildrenHtml p:
                    foreach (var child in p.Children)
                    {
                        _handlerRegistry.RegisterHandlers(child);
                    }
                    break;

                case AppendChildrenHtml p:
                    foreach (var child in p.Children)
                    {
                        _handlerRegistry.RegisterHandlers(child);
                    }
                    break;
            }
        }
    }

    private void DispatchFromSubscription(Message message) =>
        _ = _replay
            ? default
            : _core.Dispatch(message);

    private void ObserveSubscriptionFault(SubscriptionFault fault)
    {
        using var activity = _activitySource.StartActivity("Picea.Abies.SubscriptionFault");
        activity?.SetTag("subscription.key", fault.Key.Value);
        activity?.SetStatus(ActivityStatusCode.Error, fault.Exception.Message);

        try
        {
            _subscriptionFaulted?.Invoke(fault);
        }
        catch (Exception)
        {
            // Fault observation must not interfere with runtime execution.
        }
    }

    public static async Task<Runtime<TProgram, TModel, TArgument>> Start(
        Apply apply,
        Interpreter<Command, Message> interpreter,
        TArgument argument = default!,
        Action<string>? titleChanged = null,
        Action<NavigationCommand>? navigationExecutor = null,
        Action<SubscriptionFault>? subscriptionFaulted = null,
        Url? initialUrl = null,
        bool threadSafe = false,
        bool replay = false)
    {
        using var activity = _activitySource.StartActivity("Picea.Abies.Start");
        activity?.SetTag("abies.program", typeof(TProgram).Name);

        var runtime = new Runtime<TProgram, TModel, TArgument>(
            apply,
            titleChanged,
            navigationExecutor,
            subscriptionFaulted,
            replay);

        runtime._handlerRegistry.Dispatch = runtime.DispatchFromSubscription;

        var (model, initialCommand) = TProgram.Initialize(argument);

        Document document;
        using (Elements.EnterViewCacheScope(runtime._viewCacheScope))
        {
            document = TProgram.View(model);
        }
        var bodyPatches = Operations.Diff(null, document.Body);
        var headPatches = HeadDiff.Diff([], document.Head);

        IReadOnlyList<Patch> allPatches;
        if (headPatches.Count > 0)
        {
            var merged = new List<Patch>(bodyPatches.Count + headPatches.Count);
            merged.AddRange(bodyPatches);
            merged.AddRange(headPatches);
            allPatches = merged;
        }
        else
        {
            allPatches = bodyPatches;
        }

        runtime._handlerRegistry.RegisterHandlers(document.Body);

        if (allPatches.Count > 0)
        {
            apply(allPatches);
        }

        runtime._currentDocument = document;

        titleChanged?.Invoke(document.Title);

        Interpreter<Command, Message> wrappedInterpreter = command =>
            InterpretCommand(command, interpreter, runtime._navigationExecutor, replay);

        static async ValueTask<Result<Message[], PipelineError>> InterpretCommand(
            Command command,
            Interpreter<Command, Message> interpreter,
            Action<NavigationCommand>? navigationExecutor,
            bool replay)
        {
            if (replay)
            {
                return Result<Message[], PipelineError>.Ok([]);
            }

            switch (command)
            {
                case Command.None:
                    return Result<Message[], PipelineError>.Ok([]);

                case Command.Batch batch:
                {
                    var allMessages = new List<Message>();
                    foreach (var sub in batch.Commands)
                    {
                        var result = await InterpretCommand(sub, interpreter, navigationExecutor, replay);
                        if (result.IsErr)
                            return result;
                        if (result.Value.Length > 0)
                            allMessages.AddRange(result.Value);
                    }
                    return Result<Message[], PipelineError>.Ok(allMessages.ToArray());
                }

                case NavigationCommand navCommand:
                    navigationExecutor?.Invoke(navCommand);
                    return Result<Message[], PipelineError>.Ok([]);

                default:
                    return await interpreter(command);
            }
        }

        runtime._core = new AutomatonRuntime<TProgram, TModel, Message, Command, TArgument>(
            model, runtime.Observe, wrappedInterpreter,
            threadSafe: threadSafe,
            trackEvents: false);

#if DEBUG
        runtime._hotReloadRegistration =
            HotReloadRuntimeRegistry.Register(typeof(TProgram).Assembly, runtime);
#endif

        if (!replay)
        {
            var initialSubscriptions = TProgram.Subscriptions(model);
            runtime._subscriptionState = SubscriptionManager.Start(
                initialSubscriptions,
                runtime.DispatchFromSubscription,
                runtime.ObserveSubscriptionFault);

            await runtime._core.InterpretEffect(initialCommand);
        }

        if (initialUrl is not null)
        {
            await runtime.Dispatch(new UrlChanged(initialUrl));
        }

        activity?.SetStatus(ActivityStatusCode.Ok);

        return runtime;
    }

    public ValueTask<Result<Unit, PipelineError>> Dispatch(
        Message message, CancellationToken cancellationToken = default)
    {
#if DEBUG
        // Capture message to the runtime-owned debugger instance if enabled.
        if (_debuggerMachine != null)
        {
            var modelSnapshot = GenerateModelSnapshot(_core.State);
            _debuggerMachine.CaptureMessage(message, modelSnapshot);
        }
#endif
        return _core.Dispatch(message, cancellationToken);
    }

#if DEBUG
    private string GenerateModelSnapshot(TModel model)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Serialize(model);
        }
        catch
        {
            try
            {
                return model?.ToString() ?? "null";
            }
            catch
            {
                return "{}";
            }
        }
    }
#endif

    public void Dispose()
    {
        using var activity = _activitySource.StartActivity("Picea.Abies.Stop");

#if DEBUG
        _hotReloadRegistration?.Dispose();
        _hotReloadRegistration = null;
        if (ReferenceEquals(DebuggerRuntimeRegistry.CurrentDebugger, _debuggerMachine))
        {
            DebuggerRuntimeRegistry.CurrentDebugger = null;
        }

        _debuggerMachine = null;
#endif

        SubscriptionManager.Stop(_subscriptionState);
        Elements.RemoveViewCacheScope(_viewCacheScope);
        _handlerRegistry.Dispatch = null;
        _handlerRegistry.Clear();
        _core.Dispose();

        activity?.SetStatus(ActivityStatusCode.Ok);
    }

#if DEBUG
    void IHotReloadRuntime.RefreshViewFromCurrentModel() =>
        Render(_core.State);
#endif
}
