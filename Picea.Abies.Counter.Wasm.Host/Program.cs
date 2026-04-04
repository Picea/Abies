// =============================================================================
// Counter WASM Host — ASP.NET Core Server for Abies Counter (InteractiveWasm)
// =============================================================================
// Hosts the Counter MVU application in InteractiveWasm mode:
//
//     1. Serves server-rendered HTML at GET / (fast first paint)
//     2. Serves WASM bundle files at /_framework/* (dotnet.js, DLLs, etc.)
//     3. Browser downloads WASM, boots the .NET runtime client-side
//     4. Client-side MVU runtime takes over interactivity
//
// This is the InteractiveWasm hosting model — the server provides the initial
// HTML and the WASM payload, but all interactivity runs in the browser.
// No WebSocket connection is maintained after page load.
//
// The CounterProgram is defined in the shared Picea.Abies.Counter library and is
// platform-agnostic. The same program runs in both WASM (Picea.Abies.Counter.Wasm)
// and server-hosted WASM (this project).
//
// Architecture: Server prerender + WASM takeover. The server renders the
// initial view (including button handlers), then the WASM runtime boots
// and re-renders the UI client-side. This is NOT hydration — the WASM
// runtime replaces the server-rendered DOM entirely via OP_ADD_ROOT.
// The benefit is fast first paint (no WASM download wait).
//
// Usage:
//     dotnet run --project Picea.Abies.Counter.Wasm.Host
//     → http://localhost:5000
//
// Prerequisites:
//     The WASM project is automatically published during build via MSBuild.
// =============================================================================

using Picea;
using Picea.Abies.Counter;
using Picea.Abies.Server;
using Picea.Abies.Server.Kestrel;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Serve the WASM AppBundle files (_framework/dotnet.js, managed DLLs, etc.)
// The path points to the published output of the Picea.Abies.Counter.Wasm project.
// ContentRootPath is the project directory when using `dotnet run`.
var configuration =
#if DEBUG
    "Debug";
#else
    "Release";
#endif

var wasmAppBundlePath = Path.GetFullPath(Path.Combine(
    builder.Environment.ContentRootPath, "..",
    "Picea.Abies.Counter.Wasm", "bin", configuration,
    "net10.0", "browser-wasm", "AppBundle"));

app.UseStaticFiles();
app.UseAbiesWasmFiles(wasmAppBundlePath);

// OTEL: Proxy browser traces to server's OTLP endpoint
app.MapOtlpProxy();

// Map the Counter app in InteractiveWasm mode — serves initial HTML, no WebSocket.
app.MapAbies<CounterProgram, CounterModel, Unit>(
    "/{**catch-all}",
    new RenderMode.InteractiveWasm());

app.Run();
