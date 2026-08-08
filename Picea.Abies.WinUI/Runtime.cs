// =============================================================================
// WinUI Runtime Bootstrap
// =============================================================================
// One-line entry point mirroring Picea.Abies.Browser.Runtime.Run: wires the
// patch interpreter + WinUI backend into the Abies runtime.
//
// Threading: the Abies runtime invokes its Apply delegate on whatever thread
// dispatched the message (subscriptions run on the thread pool), so Apply
// marshals to the window's DispatcherQueue. threadSafe: true serializes the
// Picea core for the same reason.
// =============================================================================

using Picea.Abies.Native.Rendering;
using Picea.Abies.Subscriptions;

namespace Picea.Abies.WinUI;

/// <summary>
/// Bootstraps an Abies program onto a WinUI window.
/// </summary>
public static class Runtime
{
    /// <summary>
    /// Starts an Abies program rendering into <paramref name="rootHost"/>.
    /// </summary>
    /// <param name="rootHost">The panel that receives the root control (e.g., the window's content grid).</param>
    /// <param name="window">The hosting window; receives Document.Title updates.</param>
    /// <param name="argument">The program's initialization argument.</param>
    /// <param name="interpreter">Command interpreter for the program's effects; defaults to a no-op.</param>
    /// <param name="subscriptionFaulted">Callback for subscription failures.</param>
    /// <param name="renderFaulted">
    /// Receives rendering and dispatch failures: a patch batch that threw, a
    /// batch dropped because the dispatcher queue is shutting down, or a
    /// message dispatch that faulted. Defaults to writing to standard error —
    /// these failures are otherwise invisible, and the symptom is a window that
    /// silently stops updating.
    /// </param>
    public static async Task<Runtime<TProgram, TModel, TArgument>> Run<TProgram, TModel, TArgument>(
        Panel rootHost,
        Window window,
        TArgument argument = default!,
        Interpreter<Command, Message>? interpreter = null,
        Action<SubscriptionFault>? subscriptionFaulted = null,
        Action<Exception>? renderFaulted = null)
        where TProgram : Program<TModel, TArgument>
    {
        var dispatcherQueue = rootHost.DispatcherQueue;

        void Report(Exception exception) =>
            (renderFaulted ?? DefaultRenderFaulted).Invoke(exception);

        var backend = new WinUIBackend(rootHost, brushFaulted: Report);
        var patchInterpreter = new PatchInterpreter<FrameworkElement>(backend);

        Runtime<TProgram, TModel, TArgument>? runtime = null;
        patchInterpreter.Dispatch = async message =>
        {
            if (runtime is not null)
                await runtime.Dispatch(message);
        };
        patchInterpreter.Faulted = Report;

        // A patch batch that throws leaves the control tree out of sync with
        // the model, but letting it escape would take down the UI thread with
        // no diagnostic at all. Report and keep the window alive.
        void ApplyGuarded(IReadOnlyList<Patch> patches)
        {
            try
            {
                patchInterpreter.Apply(patches);
            }
            catch (Exception ex)
            {
                Report(new NativeRenderException(
                    "Applying a patch batch failed; the native view no longer reflects the model.", ex));
            }
        }

        void Apply(IReadOnlyList<Patch> patches)
        {
            if (dispatcherQueue.HasThreadAccess)
            {
                ApplyGuarded(patches);
                return;
            }

            // TryEnqueue returns false once the queue is shutting down. Ignoring
            // it silently drops the batch and desyncs the view.
            if (!dispatcherQueue.TryEnqueue(() => ApplyGuarded(patches)))
            {
                Report(new NativeRenderException(
                    $"Dropped a batch of {patches.Count} patch(es): the dispatcher queue is shutting down."));
            }
        }

        void TitleChanged(string title)
        {
            if (dispatcherQueue.HasThreadAccess)
                window.Title = title;
            else if (!dispatcherQueue.TryEnqueue(() => window.Title = title))
                Report(new NativeRenderException("Dropped a title update: the dispatcher queue is shutting down."));
        }

        runtime = await Runtime<TProgram, TModel, TArgument>.Start(
            Apply,
            interpreter ?? NoOpInterpreter,
            argument,
            titleChanged: TitleChanged,
            subscriptionFaulted: subscriptionFaulted,
            threadSafe: true);

        return runtime;
    }

    /// <summary>
    /// Starts a program assembled from a shared core and a native view, which is
    /// the normal shape for a native app: the model and update logic are shared
    /// with the web and server hosts, and only <c>View</c> is platform-specific.
    /// </summary>
    /// <typeparam name="TCore">The shared program core.</typeparam>
    /// <typeparam name="TView">The native view.</typeparam>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TArgument">The initialization argument type.</typeparam>
    /// <param name="rootHost">The panel that receives the root control.</param>
    /// <param name="window">The hosting window; receives Document.Title updates.</param>
    /// <param name="argument">The program's initialization argument.</param>
    /// <param name="interpreter">Command interpreter for the program's effects; defaults to a no-op.</param>
    /// <param name="subscriptionFaulted">Callback for subscription failures.</param>
    /// <param name="renderFaulted">Callback for rendering and dispatch failures.</param>
    public static Task<Runtime<WithView<TCore, TView, TModel, TArgument>, TModel, TArgument>>
        RunWithView<TCore, TView, TModel, TArgument>(
            Panel rootHost,
            Window window,
            TArgument argument = default!,
            Interpreter<Command, Message>? interpreter = null,
            Action<SubscriptionFault>? subscriptionFaulted = null,
            Action<Exception>? renderFaulted = null)
        where TCore : ProgramCore<TModel, TArgument>
        where TView : ProgramView<TModel>
        => Run<WithView<TCore, TView, TModel, TArgument>, TModel, TArgument>(
            rootHost, window, argument, interpreter, subscriptionFaulted, renderFaulted);

    private static ValueTask<Result<Message[], PipelineError>> NoOpInterpreter(Command _) =>
        ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

    private static void DefaultRenderFaulted(Exception exception) =>
        Console.Error.WriteLine($"Abies native render fault: {exception}");
}

/// <summary>
/// A failure in the native rendering path: applying a patch batch threw, or a
/// batch could not be delivered to the UI thread.
/// </summary>
public sealed class NativeRenderException : Exception
{
    /// <param name="message">Description of the failure.</param>
    public NativeRenderException(string message) : base(message) { }

    /// <param name="message">Description of the failure.</param>
    /// <param name="innerException">The underlying failure.</param>
    public NativeRenderException(string message, Exception innerException) : base(message, innerException) { }
}
