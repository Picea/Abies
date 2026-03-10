// =============================================================================
// User Domain — Constrained Types (Smart Constructors)
// =============================================================================
// Each type encodes its invariants at construction time, making illegal
// states unrepresentable. Construction returns Result<T, UserError> so
// validation failures are values, not exceptions.
//
// These types form the ubiquitous language of the User bounded context:
//   EmailAddress, Username, Password, PasswordHash, Bio, ImageUrl, Token
// =============================================================================

using System.Text.RegularExpressions;
using Picea;

namespace Picea.Abies.Conduit.Domain.User;

/// <summary>
/// A validated email address.
/// </summary>
/// <param name="Value">The normalized email string.</param>
public readonly record struct EmailAddress(string Value)
{
    // Simple regex — sufficient for the Conduit spec. Production systems
    // should use a more comprehensive check or just send a verification email.
    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Creates a validated <see cref="EmailAddress"/>.
    /// </summary>
    public static Result<EmailAddress, UserError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result<EmailAddress, UserError>.Err(new UserError.Validation("Email is required."))
            : !Pattern.IsMatch(value.Trim())
                ? Result<EmailAddress, UserError>.Err(new UserError.Validation("Email format is invalid."))
                : Result<EmailAddress, UserError>.Ok(new EmailAddress(value.Trim().ToLowerInvariant()));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// A validated username (1–20 characters, alphanumeric, hyphens, and underscores).
/// </summary>
/// <param name="Value">The normalized username string.</param>
public readonly record struct Username(string Value)
{
    private static readonly Regex Pattern = new(
        @"^[a-zA-Z0-9][a-zA-Z0-9_-]{0,19}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Creates a validated <see cref="Username"/>.
    /// </summary>
    public static Result<Username, UserError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result<Username, UserError>.Err(new UserError.Validation("Username is required."))
            : !Pattern.IsMatch(value.Trim())
                ? Result<Username, UserError>.Err(new UserError.Validation(
                    "Username must be 1-20 characters: letters, numbers, hyphens, underscores."))
                : Result<Username, UserError>.Ok(new Username(value.Trim()));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// A plain-text password (minimum 8 characters). Exists only transiently —
/// never stored. Immediately hashed via a capability function at the boundary.
/// </summary>
/// <param name="Value">The plain-text password.</param>
public readonly record struct Password(string Value)
{
    private const int MinLength = 8;

    /// <summary>
    /// Creates a validated <see cref="Password"/>.
    /// </summary>
    public static Result<Password, UserError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result<Password, UserError>.Err(new UserError.Validation("Password is required."))
            : value.Length < MinLength
                ? Result<Password, UserError>.Err(new UserError.Validation(
                    $"Password must be at least {MinLength} characters."))
                : Result<Password, UserError>.Ok(new Password(value));

    /// <inheritdoc />
    public override string ToString() => "****";
}

/// <summary>
/// A hashed password. Created by the password-hashing capability at the boundary.
/// The domain never sees the raw password — only this opaque hash.
/// </summary>
/// <param name="Value">The hashed password string (e.g., bcrypt output).</param>
public readonly record struct PasswordHash(string Value)
{
    /// <inheritdoc />
    public override string ToString() => "****";
}

/// <summary>
/// A user biography (optional, max 300 characters).
/// </summary>
/// <param name="Value">The bio text.</param>
public readonly record struct Bio(string Value)
{
    private const int MaxLength = 300;

    /// <summary>
    /// Creates a validated <see cref="Bio"/>.
    /// </summary>
    public static Result<Bio, UserError> Create(string value) =>
        value.Length > MaxLength
            ? Result<Bio, UserError>.Err(new UserError.Validation(
                $"Bio must be at most {MaxLength} characters."))
            : Result<Bio, UserError>.Ok(new Bio(value.Trim()));

    /// <summary>
    /// An empty bio.
    /// </summary>
    public static readonly Bio Empty = new(string.Empty);

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// A validated image URL for the user's avatar.
/// </summary>
/// <param name="Value">The image URL string.</param>
public readonly record struct ImageUrl(string Value)
{
    /// <summary>
    /// Creates a validated <see cref="ImageUrl"/>.
    /// </summary>
    public static Result<ImageUrl, UserError> Create(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result<ImageUrl, UserError>.Err(new UserError.Validation("Image URL is required."))
            : !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
              || (uri.Scheme != "http" && uri.Scheme != "https")
                ? Result<ImageUrl, UserError>.Err(new UserError.Validation("Image URL must be a valid HTTP/HTTPS URL."))
                : Result<ImageUrl, UserError>.Ok(new ImageUrl(uri.ToString()));

    /// <summary>
    /// An empty image URL (no avatar set).
    /// </summary>
    public static readonly ImageUrl Empty = new(string.Empty);

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// A JWT authentication token. Opaque to the domain — created and verified
/// at the infrastructure boundary.
/// </summary>
/// <param name="Value">The JWT string.</param>
public readonly record struct Token(string Value)
{
    /// <inheritdoc />
    public override string ToString() => "jwt.***";
}
