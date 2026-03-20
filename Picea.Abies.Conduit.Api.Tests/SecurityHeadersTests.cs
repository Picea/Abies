using System.Net;

namespace Picea.Abies.Conduit.Api.Tests;

public sealed class SecurityHeadersTests : IAsyncDisposable
{
    private readonly ConduitApiFactory _factory = new();

    [Test]
    public async Task ApiResponses_IncludeSecurityHeaders()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tags");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("X-Content-Type-Options")).IsTrue();
        await Assert.That(response.Headers.Contains("X-Frame-Options")).IsTrue();
        await Assert.That(response.Headers.Contains("Referrer-Policy")).IsTrue();
        await Assert.That(response.Headers.Contains("Permissions-Policy")).IsTrue();
        await Assert.That(response.Headers.Contains("Content-Security-Policy")).IsTrue();
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();
}
