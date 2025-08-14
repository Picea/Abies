using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject("conduit-api", "../Abies.Conduit.Api/Abies.Conduit.Api.csproj");

builder.AddProject("conduit-app", "../Abies.Conduit/Abies.Conduit.csproj")
       .WithReference(api);

builder.Build().Run();
