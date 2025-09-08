using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Abies.Conduit.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Configure shared defaults for all services.
    /// </summary>
    public static void AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // Health checks available to all
        builder.Services.AddHealthChecks();

        // OpenTelemetry (Aspire sets OTLP env vars; exporter picks them up)
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(builder.Environment.ApplicationName))
            .WithTracing(t => t
                .AddSource("Abies")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());
    }

    /// <summary>
    /// Map common health endpoints.
    /// </summary>
    public static void MapDefaultEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
    }
}
