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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    public static async Task<Picea.Abies.Runtime<TProgram, TModel, TArgument>> Run<TProgram, TModel, TArgument>(
        Panel rootHost,
        Window window,
        TArgument argument = default!,
        Interpreter<Command, Message>? interpreter = null,
        Action<SubscriptionFault>? subscriptionFaulted = null)
        where TProgram : Program<TModel, TArgument>
    {
        var dispatcherQueue = rootHost.DispatcherQueue;
        var backend = new WinUIBackend(rootHost);
        var patchInterpreter = new PatchInterpreter<FrameworkElement>(backend);

        Picea.Abies.Runtime<TProgram, TModel, TArgument>? runtime = null;
        patchInterpreter.Dispatch = async message =>
        {
            if (runtime is not null)
                await runtime.Dispatch(message);
        };

        void Apply(IReadOnlyList<Patch> patches)
        {
            if (dispatcherQueue.HasThreadAccess)
                patchInterpreter.Apply(patches);
            else
                dispatcherQueue.TryEnqueue(() => patchInterpreter.Apply(patches));
        }

        void TitleChanged(string title)
        {
            if (dispatcherQueue.HasThreadAccess)
                window.Title = title;
            else
                dispatcherQueue.TryEnqueue(() => window.Title = title);
        }

        runtime = await Picea.Abies.Runtime<TProgram, TModel, TArgument>.Start(
            Apply,
            interpreter ?? NoOpInterpreter,
            argument,
            titleChanged: TitleChanged,
            subscriptionFaulted: subscriptionFaulted,
            threadSafe: true);

        return runtime;
    }

    private static ValueTask<Result<Message[], PipelineError>> NoOpInterpreter(Command _) =>
        ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));
}
