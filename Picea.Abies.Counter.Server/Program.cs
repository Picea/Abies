// =============================================================================
// Counter Server — ASP.NET Core Host for the Abies Counter
// =============================================================================
// Hosts the Counter MVU application in InteractiveServer mode:
//
//     1. Serves server-rendered HTML at GET /
//     2. Maintains WebSocket sessions for live interactivity
//     3. Each browser tab gets its own isolated MVU runtime
//
// This is the server-side equivalent of Picea.Abies.Counter.Wasm — same program,
// different host. The CounterProgram is defined in the shared Picea.Abies.Counter
// library and is platform-agnostic.
//
// Usage:
//     dotnet run --project Picea.Abies.Counter.Server
//     → http://localhost:5000
// =============================================================================

using Picea;
using Picea.Abies.Counter;
using Picea.Abies.Server;
using Picea.Abies.Server.Kestrel;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();
app.UseStaticFiles();
app.UseAbiesStaticFiles();
app.MapAbies<CounterProgram, CounterModel, Unit>(
    "/{**catch-all}",
    new RenderMode.InteractiveServer());

app.Run();
