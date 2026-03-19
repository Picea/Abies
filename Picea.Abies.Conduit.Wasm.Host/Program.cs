// =============================================================================
// Conduit WASM Host — ASP.NET Core Server for Abies Conduit (InteractiveWasm)
// =============================================================================
// Hosts the Conduit MVU application in InteractiveWasm mode:
//
//     1. Serves server-rendered HTML at GET / (fast first paint)
//     2. Serves WASM bundle files at /_framework/* (dotnet.js, DLLs, etc.)
//     3. Browser downloads WASM, boots the .NET runtime client-side
//     4. Client-side MVU runtime takes over interactivity
//
// API calls made by the WASM app use window.location.origin as the base URL
// (via Picea.Abies.Browser.Runtime.GetOrigin()). This host proxies all
// /api/** requests to the conduit-api service so the WASM app works without
// any CORS or cross-origin concerns.
//
// Under .NET Aspire, the API URL is injected via the `services__conduit-api`
// environment variables set by WithReference(conduitApi) in the AppHost.
//
// Usage (standalone):
//     dotnet run --project Picea.Abies.Conduit.Wasm.Host
//     → http://localhost:5200
//
// Prerequisites:
//     The WASM project is automatically published during build via MSBuild.
// =============================================================================

using Picea.Abies.Conduit.App;
using Picea.Abies.Server;
using Picea.Abies.Server.Kestrel;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ── API URL resolution ────────────────────────────────────────────────────
// Under Aspire, WithReference(conduitApi) injects the env var
// services__conduit-api__http__0, which ASP.NET Core's env-var provider
// normalizes to the config key "services:conduit-api:http:0" (__ → :).
// The appsettings.json default (http://localhost:5179) covers standalone runs.
var conduitApiUrl = builder.Configuration["services:conduit-api:http:0"]
    ?? builder.Configuration["ConduitApiUrl"]
    ?? "http://localhost:5179";

// ── API proxy via YARP ────────────────────────────────────────────────────
// The WASM app uses window.location.origin as its API base (GetOrigin()).
// YARP proxies /api/** from this host to the conduit-api service so the WASM
// app works without any CORS configuration.
builder.Services
    .AddReverseProxy()
    .LoadFromMemory(
        routes:
        [
            new RouteConfig
            {
                RouteId = "conduit-api-route",
                ClusterId = "conduit-api",
                Match = new RouteMatch { Path = "/api/{**catch-all}" }
            }
        ],
        clusters:
        [
            new ClusterConfig
            {
                ClusterId = "conduit-api",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["default"] = new DestinationConfig { Address = conduitApiUrl }
                }
            }
        ]);

var app = builder.Build();

// Serve the WASM AppBundle files (_framework/dotnet.js, managed DLLs, etc.)
var configuration =
#if DEBUG
    "Debug";
#else
    "Release";
#endif

var wasmAppBundlePath = Path.GetFullPath(Path.Combine(
    builder.Environment.ContentRootPath, "..",
    "Picea.Abies.Conduit.Wasm", "bin", configuration,
    "net10.0", "browser-wasm", "AppBundle"));

app.UseAbiesWasmFiles(wasmAppBundlePath);
app.UseAbiesStaticFiles();

// Forward /api/** to the conduit-api service
app.MapReverseProxy();

// Serve the initial HTML page in InteractiveWasm mode (fast first paint + WASM takeover)
app.MapAbies<ConduitProgram, Model, ConduitStartup>(
    "/{**catch-all}",
    new RenderMode.InteractiveWasm(),
    interpreter: ConduitInterpreter.Interpret,
    argument: new ConduitStartup(conduitApiUrl));

app.Run();
