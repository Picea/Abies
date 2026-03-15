// =============================================================================
// User Types Tests — Constrained Type Validation
// =============================================================================
// Tests the smart constructors for all User domain types. Each type guards
// its invariants at construction time, returning Result<T, UserError>.
// =============================================================================

using Picea.Abies.Conduit.Domain.User;

namespace Picea.Abies.Conduit.Tests;

public class UserTypesTests
{
    // =========================================================================
    // EmailAddress
    // =========================================================================

    [Test]
    [Arguments("user@example.com")]
    [Arguments("USER@EXAMPLE.COM")]
    [Arguments("a@b.c")]
    [Arguments("test.user+tag@domain.co.uk")]
    public async Task EmailAddress_ValidEmails_ReturnsOk(string email)
    {
        var result = EmailAddress.Create(email);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo(email.Trim().ToLowerInvariant());
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task EmailAddress_EmptyOrWhitespace_ReturnsErr(string? email)
    {
        var result = EmailAddress.Create(email!);

        await Assert.That(result.IsErr).IsTrue();
        var error = await Assert.That(result.Error).IsTypeOf<UserError.Validation>();
        await Assert.That(error.Message).Contains("required");
    }

    [Test]
    [Arguments("notanemail")]
    [Arguments("@missing-local.com")]
    [Arguments("missing-domain@")]
    [Arguments("missing@.com")]
    public async Task EmailAddress_InvalidFormat_ReturnsErr(string email)
    {
        var result = EmailAddress.Create(email);

        await Assert.That(result.IsErr).IsTrue();
        var error = await Assert.That(result.Error).IsTypeOf<UserError.Validation>();
        await Assert.That(error.Message).Contains("format");
    }

    [Test]
    public async Task EmailAddress_NormalizesToLowercase()
    {
        var result = EmailAddress.Create("USER@EXAMPLE.COM");

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo("user@example.com");
    }

    // =========================================================================
    // Username
    // =========================================================================

    [Test]
    [Arguments("alice")]
    [Arguments("bob123")]
    [Arguments("user-name")]
    [Arguments("user_name")]
    [Arguments("a")]
    public async Task Username_ValidNames_ReturnsOk(string username)
    {
        var result = Username.Create(username);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo(username.Trim());
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task Username_EmptyOrWhitespace_ReturnsErr(string? username)
    {
        var result = Username.Create(username!);

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    [Arguments("-startswithhyphen")]
    [Arguments("_startsunderscore")]
    [Arguments("has spaces")]
    [Arguments("has@special!chars")]
    [Arguments("toolongusernamethatexceedstwentycharacters")]
    public async Task Username_InvalidFormat_ReturnsErr(string username)
    {
        var result = Username.Create(username);

        await Assert.That(result.IsErr).IsTrue();
    }

    // =========================================================================
    // Password
    // =========================================================================

    [Test]
    [Arguments("password")]
    [Arguments("12345678")]
    [Arguments("a very long password with many characters")]
    public async Task Password_ValidPasswords_ReturnsOk(string password)
    {
        var result = Password.Create(password);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo(password);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task Password_EmptyOrWhitespace_ReturnsErr(string? password)
    {
        var result = Password.Create(password!);

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    [Arguments("short")]
    [Arguments("1234567")]
    public async Task Password_TooShort_ReturnsErr(string password)
    {
        var result = Password.Create(password);

        await Assert.That(result.IsErr).IsTrue();
        var error = await Assert.That(result.Error).IsTypeOf<UserError.Validation>();
        await Assert.That(error.Message).Contains("8");
    }

    [Test]
    public async Task Password_ToString_MasksValue()
    {
        var result = Password.Create("supersecret");

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.ToString()).IsEqualTo("****");
    }

    // =========================================================================
    // Bio
    // =========================================================================

    [Test]
    public async Task Bio_ValidText_ReturnsOk()
    {
        var result = Bio.Create("I am a developer.");

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo("I am a developer.");
    }

    [Test]
    public async Task Bio_EmptyString_ReturnsOk()
    {
        var result = Bio.Create("");

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Value).IsEqualTo("");
    }

    [Test]
    public async Task Bio_TooLong_ReturnsErr()
    {
        var longBio = new string('a', 301);

        var result = Bio.Create(longBio);

        await Assert.That(result.IsErr).IsTrue();
        var error = await Assert.That(result.Error).IsTypeOf<UserError.Validation>();
        await Assert.That(error.Message).Contains("300");
    }

    // =========================================================================
    // ImageUrl
    // =========================================================================

    [Test]
    [Arguments("https://example.com/avatar.png")]
    [Arguments("http://example.com/image.jpg")]
    public async Task ImageUrl_ValidUrls_ReturnsOk(string url)
    {
        var result = ImageUrl.Create(url);

        await Assert.That(result.IsOk).IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(null)]
    public async Task ImageUrl_EmptyOrWhitespace_ReturnsErr(string? url)
    {
        var result = ImageUrl.Create(url!);

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    [Arguments("ftp://example.com/file")]
    [Arguments("not a url")]
    [Arguments("file:///etc/passwd")]
    public async Task ImageUrl_InvalidScheme_ReturnsErr(string url)
    {
        var result = ImageUrl.Create(url);

        await Assert.That(result.IsErr).IsTrue();
    }

    // =========================================================================
    // Token
    // =========================================================================

    [Test]
    public async Task Token_ToString_MasksValue()
    {
        var token = new Token("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload.signature");

        await Assert.That(token.ToString()).IsEqualTo("jwt.***");
    }
}
