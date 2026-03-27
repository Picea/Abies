// =============================================================================
// Browser Runtime — Centralized WASM Bootstrap
// =============================================================================
// One-line entry point for running an Abies MVU application in the browser.
//
// This replaces the ~30 lines of boilerplate that every WASM consumer
// previously needed in their Program.cs:
//   - JSHost.ImportAsync
//   - RenderBatchWriter creation
//   - BrowserApply closure with MemoryMarshal
//   - SetupEventDelegation / SetupNavigation
//   - Dispatch callback wiring
//   - URL-changed callback wiring
//   - Core Runtime.Start with all parameters
//   - Task.Delay(Timeout.Infinite)
//
// All of this is now a single line:
//   await Runtime.Run<CounterProgram, CounterModel, Unit>();
//
// Boot chain:
//   1. Consumer index.html has: <script type="module" src="abies.js"></script>
//   2. abies.js defines exports, imports dotnet.js, calls dotnet.run() (NO await)
//      → Module evaluation completes immediately (exports available)
//      → dotnet.run() fires asynchronously in the background
//   3. .NET runtime starts → Program.Main() → Runtime.Run<>()
//   4. Runtime.Run() calls JSHost.ImportAsync("Abies", "../abies.js")
//      → Module already fully evaluated → returns cached namespace instantly
//   5. Runtime.Run() wires callbacks, sets up events/navigation
//   6. MVU loop starts, initial render paints the UI
//
// CRITICAL: dotnet.run() must NOT be awaited in abies.js.
//   If it were, the module evaluation would block until dotnet.run() returns.
//   But dotnet.run() → Runtime.Run() → JSHost.ImportAsync("abies.js") which
//   waits for the module evaluation to finish → DEADLOCK.
//
// InteractiveWasm/Auto guard:
//   In server-rendered mode (Page.cs), the server inlines its own bootstrap
//   <script> that sets globalThis.__ABIES_DOTNET_STARTED = true before calling
//   dotnet.run(). When Runtime.Run() later imports abies.js (first evaluation),
//   the guard at the bottom of abies.js skips dotnet.run() to avoid
//   double-bootstrap.
//
// The consumer's index.html only needs:
//   <script type="module" src="abies.js"></script>
//
// No separate main.js or dotnet.js script tag is required — abies.js handles
// the entire WASM bootstrap internally.
//
// See also:
//   - abies.js — browser-side runtime + WASM bootstrap
//   - Picea.Abies/Runtime.cs — platform-agnostic MVU execution loop
//   - Interop.cs — JavaScript ↔ .NET bridge
// =============================================================================

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
#if DEBUG
using Picea.Abies.Debugger;
#endif

namespace Picea.Abies.Browser;

/// <summary>
/// Browser runtime entry point for Abies MVU applications.
/// </summary>
/// <remarks>
/// <para>
/// Provides a single <see cref="Run{TProgram,TModel,TArgument}"/> method that
/// handles all browser-specific wiring:
/// </para>
/// <list type="bullet">
///   <item>Loading the <c>abies.js</c> JavaScript module</item>
///   <item>Creating the binary batch writer and <see cref="Apply"/> delegate</item>
///   <item>Wiring event delegation callbacks (DOM events → .NET)</item>
///   <item>Wiring navigation callbacks (URL changes → .NET)</item>
///   <item>Starting the platform-agnostic <see cref="Runtime{TProgram,TModel,TArgument}"/></item>
///   <item>Keeping the WASM process alive indefinitely</item>
/// </list>
/// <example>
/// <code>
/// // Entire Program.cs for a browser application:
/// await Picea.Abies.Browser.Runtime.Run&lt;CounterProgram, CounterModel, Unit&gt;();
/// </code>
/// </example>
/// </remarks>
[SupportedOSPlatform("browser")]
public static class Runtime
{
    /// <summary>
    /// Imports the Abies JavaScript module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this before <see cref="Run{TProgram,TModel,TArgument}"/> if you need access
    /// to browser interop (e.g., <see cref="GetOrigin"/>) before the MVU loop starts.
    /// The subsequent <c>ImportAsync</c> inside <c>Run</c> is a no-op because ES modules
    /// are cached by the browser.
    /// </para>
    /// </remarks>
    public static async Task ImportModule() =>
        await JSHost.ImportAsync("Abies", "../abies.js");

    /// <summary>
    /// Returns the browser window's origin (e.g., <c>"http://localhost:5000"</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Abies module must be loaded first via <see cref="ImportModule"/>.
    /// Typical use: pass the origin as the API base URL so that the WASM
    /// <see cref="HttpClient"/> can build absolute request URIs.
    /// </para>
    /// <example>
    /// <code>
    /// await Runtime.ImportModule();
    /// var apiUrl = Runtime.GetOrigin();
    /// await Runtime.Run&lt;MyProgram, MyModel, string&gt;(argument: apiUrl);
    /// </code>
    /// </example>
    /// </remarks>
    /// <returns>The window origin, or empty string if unavailable.</returns>
    public static string GetOrigin() => Interop.GetOrigin();

    public static string? GetSessionStorageItem(string key) => Interop.GetSessionStorageItem(key);

    public static void SetSessionStorageItem(string key, string value) => Interop.SetSessionStorageItem(key, value);

    public static void RemoveSessionStorageItem(string key) => Interop.RemoveSessionStorageItem(key);

    public static string GetCurrentUrl() => Interop.GetCurrentUrl();

    /// <summary>
    /// Runs an Abies MVU application in the browser.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs the complete browser bootstrap sequence:
    /// <list type="number">
    ///   <item>Loads <c>abies.js</c> via <c>JSHost.ImportAsync</c></item>
    ///   <item>Wires the dispatch callback (DOM events → .NET <c>DispatchDomEvent</c>)</item>
    ///   <item>Wires the URL-changed callback (navigation → .NET <c>OnUrlChanged</c>)</item>
    ///   <item>Sets up event delegation at the document level</item>
    ///   <item>Sets up navigation interception (popstate + link clicks)</item>
    ///   <item>Creates a <see cref="RenderBatchWriter"/> and <see cref="Apply"/> delegate</item>
    ///   <item>Parses the current browser URL for initial routing</item>
    ///   <item>Starts the core <see cref="Runtime{TProgram,TModel,TArgument}"/></item>
    ///   <item>Blocks indefinitely to keep the WASM process alive</item>
    /// </list>
    /// </para>
    /// <para>
    /// The <paramref name="interpreter"/> is optional. When omitted, a no-op interpreter
    /// is used (all commands return empty message arrays). This is suitable for applications
    /// that have no side effects beyond DOM updates and navigation.
    /// </para>
    /// </remarks>
    /// <typeparam name="TProgram">
    /// The program type implementing <see cref="Program{TModel,TArgument}"/>.
    /// </typeparam>
    /// <typeparam name="TModel">The application model (state).</typeparam>
    /// <typeparam name="TArgument">Initialization parameters for <c>TProgram.Initialize</c>.</typeparam>
    /// <param name="argument">
    /// Initialization parameters passed to <c>TProgram.Initialize</c>.
    /// Defaults to <c>default!</c> (typically <see cref="Unit"/> for parameterless apps).
    /// </param>
    /// <param name="interpreter">
    /// Converts commands into feedback messages. When <c>null</c>, a no-op interpreter
    /// is used that returns empty message arrays for all commands.
    /// </param>
    /// <returns>A task that never completes (keeps the WASM process alive).</returns>
    public static async Task Run<TProgram, TModel, TArgument>(
        TArgument argument = default!,
        Interpreter<Command, Message>? interpreter = null)
        where TProgram : Program<TModel, TArgument>
    {
        // Step 1: Load the abies.js module.
        // Path is relative to the calling module (_framework/dotnet.runtime.js),
        // so "../abies.js" navigates up from _framework/ to the wwwroot root.
        await JSHost.ImportAsync("Abies", "../abies.js");

        // Step 2: Wire callbacks from JS → .NET.
        // The .NET side wires callbacks directly via JSImport:
        //   - DOM events fire → abies.js calls dispatchDomEvent → DispatchDomEvent [JSExport]
        //   - URL changes → abies.js calls onUrlChangedCallback → OnUrlChanged [JSExport]
        Interop.SetDispatchCallback(Interop.DispatchDomEvent);
        Interop.SetOnUrlChangedCallback(Interop.OnUrlChanged);

        // Step 3: Set up event delegation and navigation interception.
        Interop.SetupEventDelegation();
        Interop.SetupNavigation();

        // Step 4: Create the binary batch writer and browser Apply delegate.
        // patches → binary batch → JS interop → DOM mutations
        var batchWriter = new RenderBatchWriter();

        void BrowserApply(IReadOnlyList<Patch> patches)
        {
            var binaryData = batchWriter.Write(patches);

            // ReadOnlyMemory<byte> → Span<byte> for JSType.MemoryView interop.
            // The underlying store is ArrayBufferWriter<byte>'s byte[], so
            // MemoryMarshal.TryGetArray always succeeds.
            MemoryMarshal.TryGetArray(binaryData, out var segment);
            Interop.ApplyBinaryBatch(segment.Array.AsSpan(segment.Offset, segment.Count));
        }

        // Step 5: Create the navigation executor that maps NavigationCommands
        // to JS interop calls.
        void NavigationExecutor(NavigationCommand command)
        {
            switch (command)
            {
                case NavigationCommand.Push push:
                    Interop.NavigateTo(push.Url.ToRelativeUri());
                    break;
                case NavigationCommand.Replace replace:
                    Interop.ReplaceUrl(replace.Url.ToRelativeUri());
                    break;
                case NavigationCommand.GoBack:
                    Interop.HistoryBack();
                    break;
                case NavigationCommand.GoForward:
                    Interop.HistoryForward();
                    break;
                case NavigationCommand.External ext:
                    Interop.ExternalNavigate(ext.Href);
                    break;
            }
        }

        // Step 6: Parse the current browser URL for initial routing.
        var currentUrl = Interop.GetCurrentUrl();
        var initialUrl = Navigation.ParseUrl(currentUrl);

        // Step 7: Use the provided interpreter or a no-op default.
        var effectiveInterpreter = interpreter ?? NoOpInterpreter;

        // Step 8: Start the core MVU runtime.
        var runtime = await Runtime<TProgram, TModel, TArgument>.Start(
            apply: BrowserApply,
            interpreter: effectiveInterpreter,
            argument: argument,
            titleChanged: Interop.SetTitle,
            navigationExecutor: NavigationExecutor,
            initialUrl: initialUrl);

        // Step 9: Wire the runtime's handler registry to the browser interop layer.
        // The [JSExport] DispatchDomEvent method is static, so it needs a static
        // reference to the runtime's handler registry. Safe because WASM is
        // single-threaded - one runtime, one registry per browser tab.
        //
        // This wiring must happen before any optional debugger bootstrap so that
        // the UI stays interactive even if debugger startup fails.
        Interop.Handlers = runtime.Handlers;

#if DEBUG
        if (DebuggerConfiguration.Default.Enabled)
        {
            try
            {
                await JSHost.ImportAsync("AbiesDebugger", "../debugger.js");
                runtime.UseDebugger();
                Interop.Debugger = runtime.Debugger;
                Interop.SetRuntimeBridge(Interop.DispatchDebuggerMessage);
                Interop.MountDebugger();
            }
            catch
            {
                // Debugger bootstrap is optional and must never block normal app input.
            }
        }
#endif

        // Step 10: Keep the WASM process alive indefinitely.
        // Main must not return or the .NET runtime is torn down.
        await Task.Delay(Timeout.Infinite);
    }

    /// <summary>
    /// Default no-op interpreter: returns empty message arrays for all commands.
    /// </summary>
    private static ValueTask<Result<Message[], PipelineError>> NoOpInterpreter(Command _) =>
        new(Result<Message[], PipelineError>.Ok([]));
}
