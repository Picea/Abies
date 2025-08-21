using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Aspire Dashboard can be launched by the Aspire CLI/workload; desired port can be set via env var
builder.Configuration["DOTNET_DASHBOARD_PORT"] = "18888";

// Backend API
var api = builder.AddProject("conduit-api", "../Abies.Conduit.Api/Abies.Conduit.Api.csproj");

// Frontend app
builder.AddProject("conduit-app", "../Abies.Conduit/Abies.Conduit.csproj")
       .WithReference(api);

builder.Build().Run();
