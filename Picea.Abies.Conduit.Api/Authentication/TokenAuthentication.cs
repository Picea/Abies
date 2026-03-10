// =============================================================================
// Custom "Token" Authentication Handler
// =============================================================================
// The Conduit spec mandates "Authorization: Token jwt.token.here" — not the
// standard Bearer scheme. This handler extracts the JWT from the "Token"
// prefix, validates it via JwtTokenService, and populates the ClaimsPrincipal.
//
// Registered as authentication scheme "Token" in Program.cs.
// =============================================================================

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Picea.Abies.Conduit.Api.Authentication;

/// <summary>
/// Options for the Token authentication scheme (no custom settings needed).
/// </summary>
public sealed class TokenAuthenticationOptions : AuthenticationSchemeOptions;

/// <summary>
/// Extracts and validates JWTs from the "Authorization: Token ..." header.
/// </summary>
/// <remarks>
/// This is the Conduit-specific auth handler. It delegates JWT validation
/// to <see cref="Infrastructure.JwtTokenService"/> and sets the claims
/// principal on success.
/// </remarks>
public sealed class TokenAuthenticationHandler(
    IOptionsMonitor<TokenAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    Infrastructure.JwtTokenService jwtTokenService)
    : AuthenticationHandler<TokenAuthenticationOptions>(options, logger, encoder)
{
    private const string TokenPrefix = "Token ";

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var headerValue = authHeader.ToString();

        if (!headerValue.StartsWith(TokenPrefix, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = headerValue[TokenPrefix.Length..].Trim();

        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.Fail("Empty token.");

        var principal = await jwtTokenService.ValidateToken(token).ConfigureAwait(false);

        if (principal is null)
            return AuthenticateResult.Fail("Invalid or expired token.");

        var identity = principal.Identity as ClaimsIdentity ?? new ClaimsIdentity(principal.Claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
