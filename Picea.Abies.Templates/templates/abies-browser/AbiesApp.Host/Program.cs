using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Picea.Abies.Server.Kestrel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing
        .AddSource("Picea.Abies.Server.Kestrel.OtlpProxy")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

var wasmAppBundlePath = ResolveWasmAppBundlePath(builder.Environment.ContentRootPath);

app.UseAbiesWasmFiles(wasmAppBundlePath);
app.MapOtlpProxy();

var indexPath = Path.Combine(wasmAppBundlePath, "index.html");
app.MapGet("/", () => Results.File(indexPath, "text/html; charset=utf-8"));
app.MapFallback(() => Results.File(indexPath, "text/html; charset=utf-8"));

app.Run();

static string ResolveWasmAppBundlePath(string contentRootPath)
{
    var preferredConfiguration =
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    var configurationsToProbe = preferredConfiguration == "Debug"
        ? new[] { "Debug", "Release" }
        : new[] { "Release", "Debug" };

    foreach (var configuration in configurationsToProbe)
    {
        var candidate = Path.GetFullPath(Path.Combine(
            contentRootPath,
            "..",
            "bin",
            configuration,
            "net10.0",
            "browser-wasm",
            "AppBundle"));

        if (Directory.Exists(candidate))
        {
            return candidate;
        }
    }

    throw new DirectoryNotFoundException(
        $"WASM AppBundle directory not found under Debug/Release output paths for {contentRootPath}. " +
        "Ensure the WASM project has been built or published before starting the host.");
}
