// =============================================================================
// Password Hasher — BCrypt Capability at the Boundary
// =============================================================================
// The domain never sees raw passwords or hashing algorithms. This module
// provides the two capability functions injected at the API boundary:
//
//   HashPassword : Password → PasswordHash
//   VerifyPassword : (string rawPassword, PasswordHash hash) → bool
//
// Uses BCrypt.Net-Next, which produces hashes in the Modular Crypt Format:
//   $2a$12$... (algorithm, cost, salt, hash)
// =============================================================================

using Picea.Abies.Conduit.Domain.User;

namespace Picea.Abies.Conduit.Api.Infrastructure;

/// <summary>
/// BCrypt-based password hashing capability functions.
/// </summary>
public static class PasswordHasher
{
    private const int WorkFactor = 12;

    /// <summary>
    /// Hashes a validated <see cref="Password"/> into a <see cref="PasswordHash"/>.
    /// </summary>
    public static PasswordHash Hash(Password password) =>
        new(BCrypt.Net.BCrypt.EnhancedHashPassword(password.Value, WorkFactor));

    /// <summary>
    /// Verifies a raw password string against a stored <see cref="PasswordHash"/>.
    /// </summary>
    public static bool Verify(string rawPassword, PasswordHash hash) =>
        BCrypt.Net.BCrypt.EnhancedVerify(rawPassword, hash.Value);
}
