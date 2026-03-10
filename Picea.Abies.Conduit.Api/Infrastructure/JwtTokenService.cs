// =============================================================================
// JWT Token Service — Sign, Verify, and Extract Claims
// =============================================================================
// Provides the Token capability function for the domain boundary.
// Uses HMAC-SHA256 symmetric signing — the secret is injected via
// configuration ("Jwt:Secret").
//
// The Conduit spec uses "Authorization: Token jwt.token.here" — NOT Bearer.
// This service handles the JWT payload; the custom auth handler handles
// the "Token" scheme extraction.
// =============================================================================

using System.Diagnostics;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Picea.Abies.Conduit.Api.Infrastructure;

/// <summary>
/// JWT token generation and validation service.
/// </summary>
public sealed class JwtTokenService
{
    private readonly SymmetricSecurityKey _signingKey;
    private readonly SigningCredentials _credentials;
    private readonly JsonWebTokenHandler _handler = new();
    private readonly TokenValidationParameters _validationParameters;
    private readonly string _issuer;
    private readonly TimeSpan _expiration;

    /// <summary>
    /// Creates a new JWT token service.
    /// </summary>
    /// <param name="secret">The HMAC-SHA256 signing secret (minimum 32 UTF-8 bytes).</param>
    /// <param name="issuer">The JWT issuer claim.</param>
    /// <param name="expiration">Token expiration duration. Defaults to 7 days.</param>
    public JwtTokenService(string secret, string issuer = "conduit", TimeSpan? expiration = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        if (System.Text.Encoding.UTF8.GetByteCount(secret) < 32)
            throw new ArgumentException("JWT secret must be at least 32 bytes (UTF-8 encoded).", nameof(secret));

        _signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        _credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        _issuer = issuer;
        _expiration = expiration ?? TimeSpan.FromDays(7);

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }

    /// <summary>
    /// Generates a JWT token for the given user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="username">The user's display name.</param>
    /// <param name="email">The user's email address.</param>
    /// <returns>The signed JWT string.</returns>
    public string GenerateToken(Guid userId, string username, string email)
    {
        using var activity = ApiDiagnostics.Source.StartActivity("Jwt.Generate");
        activity?.SetTag("jwt.user.id", userId);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("username", username),
                new Claim("email", email)
            ]),
            Expires = DateTime.UtcNow.Add(_expiration),
            Issuer = _issuer,
            SigningCredentials = _credentials
        };

        var token = _handler.CreateToken(descriptor);

        activity?.SetStatus(ActivityStatusCode.Ok);
        return token;
    }

    /// <summary>
    /// Validates a JWT token and extracts the claims principal.
    /// </summary>
    /// <param name="token">The JWT string to validate.</param>
    /// <returns>
    /// The claims principal on success, or null if validation fails.
    /// </returns>
    public async ValueTask<ClaimsPrincipal?> ValidateToken(string token)
    {
        using var activity = ApiDiagnostics.Source.StartActivity("Jwt.Validate");

        var result = await _handler.ValidateTokenAsync(token, _validationParameters)
            .ConfigureAwait(false);

        if (result.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
            return new ClaimsPrincipal(result.ClaimsIdentity);
        }

        activity?.SetTag("jwt.validation.error", result.Exception?.Message);
        activity?.SetStatus(ActivityStatusCode.Error, result.Exception?.Message);
        return null;
    }

    /// <summary>
    /// Extracts the user ID from a validated claims principal.
    /// </summary>
    public static Guid? GetUserId(ClaimsPrincipal? principal)
    {
        var sub = principal?.FindFirstValue("sub");
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
