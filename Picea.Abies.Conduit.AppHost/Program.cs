var builder = DistributedApplication.CreateBuilder(args);

// Aspire Dashboard can be launched by the Aspire CLI/workload; desired port can be set via env var
builder.Configuration["DOTNET_DASHBOARD_PORT"] = "18888";

// ─── Infrastructure containers ─────────────────────────────────────────────
var kurrentdb = builder.AddKurrentDB("kurrentdb");

var conduitdb = builder.AddPostgres("postgres")
    .AddDatabase("conduitdb");

// Backend API – pass the *database* resource so Aspire injects
// ConnectionStrings:conduitdb (not ConnectionStrings:postgres).
builder.AddProject("conduit-api", "../Picea.Abies.Conduit.Api/Picea.Abies.Conduit.Api.csproj")
    .WithReference(kurrentdb)
    .WithReference(conduitdb)
    .WaitFor(kurrentdb)
    .WaitFor(conduitdb);

builder.Build().Run();