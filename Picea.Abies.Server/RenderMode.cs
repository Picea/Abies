// =============================================================================
// Render Mode — Discriminated Union for All Abies Render Modes
// =============================================================================
// Models the four rendering strategies available in Abies, mirroring the
// Blazor render mode spectrum but implemented as pure computation:
//
//     Static            → One-shot HTML generation, no interactivity
//     InteractiveServer → Server holds state, patches sent over transport
//     InteractiveWasm   → Static page bootstraps client-side WASM
//     InteractiveAuto   → Server-first, transitions to WASM when ready
//
// This is a closed discriminated union — exhaustive pattern matching is
// required. The render mode is a pure data type with no behavior; the
// rendering logic lives in Page and Session.
//
// Principle: Each mode maps to a different combination of two orthogonal
// concerns: (1) where the initial HTML comes from, and (2) where the
// MVU loop runs after initial render.
//
//     Mode              │ Initial HTML  │ MVU Loop
//     ──────────────────┼───────────────┼───────────────
//     Static            │ Server        │ None
//     InteractiveServer │ Server        │ Server (WebSocket)
//     InteractiveWasm   │ Server        │ Client (WASM)
//     InteractiveAuto   │ Server        │ Server → Client
//
// =============================================================================

namespace Picea.Abies.Server;

/// <summary>
/// Specifies how an Abies application should be rendered and where
/// interactivity is hosted.
/// </summary>
/// <remarks>
/// <para>
/// All four modes share the same initial HTML rendering pipeline
/// (<see cref="Page.Render"/>). They differ in what bootstrap scripts
/// are injected and whether a server-side MVU session is maintained.
/// </para>
/// <para>
/// This is a closed sum type — use exhaustive pattern matching:
/// </para>
/// <example>
/// <code>
/// var scripts = mode switch
/// {
///     RenderMode.Static => "",
///     RenderMode.InteractiveServer s => s.WebSocketUrl,
///     RenderMode.InteractiveWasm => "_framework/dotnet.js",
///     RenderMode.InteractiveAuto a => a.WebSocketUrl,
///     _ => throw new UnreachableException()
/// };
/// </code>
/// </example>
/// </remarks>
public interface RenderMode
{
    /// <summary>
    /// Static server-side rendering — produces HTML with no client interactivity.
    /// The page is rendered once and served as-is. No WebSocket, no WASM.
    /// </summary>
    /// <remarks>
    /// Ideal for content pages, SEO-critical pages, or any page where
    /// interactivity is unnecessary. The lightest mode — zero JavaScript
    /// payload for the Abies framework.
    /// </remarks>
    sealed record Static : RenderMode;

    /// <summary>
    /// Interactive server rendering — the MVU loop runs on the server,
    /// DOM patches are sent to the client over a WebSocket transport.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The server maintains a per-session <see cref="Session"/> that holds
    /// the <see cref="Runtime{TProgram,TModel,TArgument}"/>. User events
    /// arrive via the transport; patches flow back through the same transport
    /// using the binary batch protocol.
    /// </para>
    /// <para>
    /// Tradeoffs: instant interactivity (no WASM download), but requires
    /// persistent server connection and incurs network latency per interaction.
    /// </para>
    /// </remarks>
    /// <param name="WebSocketPath">
    /// The relative path for the WebSocket endpoint (e.g., <c>"/_abies/ws"</c>).
    /// The hosting adapter maps this to an actual WebSocket listener.
    /// </param>
    sealed record InteractiveServer(string WebSocketPath = "/_abies/ws") : RenderMode;

    /// <summary>
    /// Interactive WebAssembly rendering — the page is server-rendered,
    /// then the client-side WASM runtime takes over.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The server produces the initial HTML (including the body from
    /// <c>TProgram.View</c>). The page includes the .NET WASM bootstrap
    /// script. Once WASM loads, the client-side runtime diffs against the
    /// server-rendered DOM and takes over event handling.
    /// </para>
    /// <para>
    /// Tradeoffs: no persistent server connection needed after initial load,
    /// but the user waits for the WASM bundle to download before interactivity.
    /// First paint is fast (server-rendered HTML).
    /// </para>
    /// </remarks>
    sealed record InteractiveWasm : RenderMode;

    /// <summary>
    /// Interactive Auto rendering — starts with server-side interactivity,
    /// transitions to WASM once the client runtime is ready.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Combines the instant interactivity of <see cref="InteractiveServer"/>
    /// with the connectionless scalability of <see cref="InteractiveWasm"/>.
    /// The server holds the MVU session initially; once WASM is loaded and
    /// hydrated, the server session is disposed and the client takes over.
    /// </para>
    /// <para>
    /// Tradeoffs: most complex mode — requires server session management,
    /// WASM handoff protocol, and state transfer. Best user experience
    /// (fast interactivity + no persistent connection).
    /// </para>
    /// </remarks>
    /// <param name="WebSocketPath">
    /// The relative path for the WebSocket endpoint during the server phase.
    /// </param>
    sealed record InteractiveAuto(string WebSocketPath = "/_abies/ws") : RenderMode;
}
