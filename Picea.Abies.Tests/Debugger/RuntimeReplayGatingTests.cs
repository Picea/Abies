using System.Reflection;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Elements;

namespace Picea.Abies.Tests.Debugger;

public sealed class RuntimeReplayGatingTests
{
    [Test]
    public async Task ReplayMode_DoesNotExecuteInterpreterCommands()
    {
        ReplayProbeProgram.Reset();
        var interpreterCalls = 0;

        static void Apply(IReadOnlyList<Patch> _)
        { }

        ValueTask<Result<Message[], PipelineError>> Interpreter(Command command)
        {
            if (command is ReplayEffectCommand)
            {
                interpreterCalls++;
            }

            return ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));
        }

        using var runtime = await StartRuntimeInReplayMode(Apply, Interpreter, _ => { });

        await runtime.Dispatch(new ReplayTick());

        await Assert.That(interpreterCalls).IsEqualTo(0);
    }

    [Test]
    public async Task ReplayMode_DoesNotStartOrUpdateSubscriptions()
    {
        ReplayProbeProgram.Reset();

        static void Apply(IReadOnlyList<Patch> _)
        { }

        static ValueTask<Result<Message[], PipelineError>> Interpreter(Command _) =>
            ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

        using var runtime = await StartRuntimeInReplayMode(Apply, Interpreter, _ => { });

        await runtime.Dispatch(new ReplayTick());

        await Assert.That(ReplayProbeProgram.SubscriptionStarts).IsEqualTo(0);
    }

    [Test]
    public async Task ReplayMode_DoesNotInvokeNavigationExecutor()
    {
        ReplayProbeProgram.Reset();
        var navigationCalls = 0;

        static void Apply(IReadOnlyList<Patch> _)
        { }

        static ValueTask<Result<Message[], PipelineError>> Interpreter(Command _) =>
            ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

        using var runtime = await StartRuntimeInReplayMode(
            Apply,
            Interpreter,
            _ => navigationCalls++);

        await runtime.Dispatch(new ReplayTick());

        await Assert.That(navigationCalls).IsEqualTo(0);
    }

    private static async Task<Runtime<ReplayProbeProgram, ReplayProbeModel, Unit>> StartRuntimeInReplayMode(
        Apply apply,
        Interpreter<Command, Message> interpreter,
        Action<NavigationCommand> navigationExecutor)
    {
        var runtimeType = typeof(Runtime<ReplayProbeProgram, ReplayProbeModel, Unit>);
        var startMethod = runtimeType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m =>
                m.Name == nameof(Runtime<ReplayProbeProgram, ReplayProbeModel, Unit>.Start)
                && m.GetParameters().Any(p => p.Name == "replay" && p.ParameterType == typeof(bool)));

        if (startMethod is null)
        {
            throw new InvalidOperationException(
                "Missing replay opt-in on Runtime.Start. Expected parameter: bool replay = false.");
        }

        var startParameters = startMethod.GetParameters();
        var invocationArguments = new object?[startParameters.Length];

        for (var index = 0; index < startParameters.Length; index++)
        {
            invocationArguments[index] = startParameters[index].Name switch
            {
                "apply" => apply,
                "interpreter" => interpreter,
                "argument" => default(Unit),
                "titleChanged" => null,
                "navigationExecutor" => navigationExecutor,
                "subscriptionFaulted" => null,
                "initialUrl" => null,
                "threadSafe" => false,
                "replay" => true,
                _ => throw new InvalidOperationException(
                    $"Unsupported Runtime.Start parameter for replay invocation: {startParameters[index].Name}")
            };
        }

        if (startMethod.Invoke(null, invocationArguments) is not Task<Runtime<ReplayProbeProgram, ReplayProbeModel, Unit>> startTask)
        {
            throw new InvalidOperationException("Runtime.Start replay invocation returned an unexpected task type.");
        }

        return await startTask;
    }

    private sealed record ReplayProbeModel(int Version);

    private sealed record ReplayTick : Message;

    private sealed record ReplayEffectCommand : Command;

    private sealed class ReplayProbeProgram : Program<ReplayProbeModel, Unit>
    {
        private static int _subscriptionStarts;

        public static int SubscriptionStarts => _subscriptionStarts;

        public static void Reset() => _subscriptionStarts = 0;

        public static (ReplayProbeModel, Command) Initialize(Unit argument) =>
            (
                new ReplayProbeModel(0),
                Commands.Batch(new ReplayEffectCommand(), Navigation.PushUrl(Url.Root))
            );

        public static (ReplayProbeModel, Command) Transition(ReplayProbeModel model, Message message) =>
            message switch
            {
                ReplayTick =>
                    (
                        model with { Version = model.Version + 1 },
                        Commands.Batch(new ReplayEffectCommand(), Navigation.ReplaceUrl(Url.Root))
                    ),
                _ => (model, Commands.None)
            };

        public static Document View(ReplayProbeModel model) =>
            new("Replay Probe", div([], [text($"version:{model.Version}")]));

        public static Subscription Subscriptions(ReplayProbeModel model) =>
            SubscriptionModule.Create(
                $"replay:probe:{model.Version}",
                (_, _) =>
                {
                    Interlocked.Increment(ref _subscriptionStarts);
                    return Task.CompletedTask;
                });
    }
}
