var builder = DistributedApplication.CreateBuilder(args);

// Aspire Dashboard can be launched by the Aspire CLI/workload; desired port can be set via env var
builder.Configuration["DOTNET_DASHBOARD_PORT"] = "18888";

// ─── Infrastructure containers ─────────────────────────────────────────────
var kurrentdb = builder.AddKurrentDB("kurrentdb");

var conduitdb = builder.AddPostgres("postgres")
    .AddDatabase("conduitdb");

// ─── Backend API ────────────────────────────────────────────────────────────
// Pass the *database* resource so Aspire injects ConnectionStrings:conduitdb.
var conduitApi = builder.AddProject("conduit-api", "../Picea.Abies.Conduit.Api/Picea.Abies.Conduit.Api.csproj")
    .WithReference(kurrentdb)
    .WithReference(conduitdb)
    .WaitFor(kurrentdb)
    .WaitFor(conduitdb);

// ─── Frontend: InteractiveServer ────────────────────────────────────────────
// Server-rendered HTML + WebSocket sessions. WithReference injects
// services__conduit-api__http__0 so the server can resolve the API URL.
builder.AddProject("conduit-server", "../Picea.Abies.Conduit.Server/Picea.Abies.Conduit.Server.csproj")
    .WithReference(conduitApi)
    .WaitFor(conduitApi);

// ─── Frontend: InteractiveWasm ──────────────────────────────────────────────
// Serves the initial HTML + WASM bundle. All /api/** calls from the browser
// are proxied to conduit-api via YARP running inside the Wasm host.
// WithReference injects services__conduit-api__http__0 for the YARP cluster URL.
builder.AddProject("conduit-wasm", "../Picea.Abies.Conduit.Wasm.Host/Picea.Abies.Conduit.Wasm.Host.csproj")
    .WithReference(conduitApi)
    .WaitFor(conduitApi);

builder.Build().Run();
