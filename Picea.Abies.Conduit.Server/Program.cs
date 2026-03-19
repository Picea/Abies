// =============================================================================
// Conduit Server — ASP.NET Core Host for the Abies Conduit Application
// =============================================================================
// Hosts the Conduit MVU application in InteractiveServer mode:
//
//     1. Serves server-rendered HTML at GET /
//     2. Maintains WebSocket sessions for live interactivity
//     3. Each browser tab gets its own isolated MVU runtime
//     4. HTTP commands are interpreted server-side via ConduitInterpreter
//
// This is the server-side equivalent of Picea.Abies.Conduit.Wasm — same program,
// different host. The ConduitProgram is defined in the shared Picea.Abies.Conduit.App
// library and is platform-agnostic.
//
// Usage:
//     dotnet run --project Picea.Abies.Conduit.Server
//     → http://localhost:5100
// =============================================================================

using Picea.Abies.Conduit.App;
using Picea.Abies.Server;
using Picea.Abies.Server.Kestrel;

var builder = WebApplication.CreateBuilder(args);
var conduitApiUrl = builder.Configuration["services:conduit-api:http:0"]
    ?? builder.Configuration["ConduitApiUrl"]
    ?? "http://localhost:5000";
var app = builder.Build();

app.UseWebSockets();
app.UseAbiesStaticFiles();
app.MapAbies<ConduitProgram, Model, ConduitStartup>(
    "/{**catch-all}",
    new RenderMode.InteractiveServer(),
    interpreter: ConduitInterpreter.Interpret,
    argument: new ConduitStartup(conduitApiUrl));

app.Run();
