using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Abies.Conduit.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Configure shared defaults for all services.
    /// </summary>
    public static void AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // Add services required by all applications here.
        builder.Services.AddHealthChecks();
    }

    /// <summary>
    /// Map common health endpoints.
    /// </summary>
    public static void MapDefaultEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
    }
}
