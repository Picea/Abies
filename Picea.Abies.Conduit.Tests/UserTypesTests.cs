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

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("a@b.c")]
    [InlineData("test.user+tag@domain.co.uk")]
    public void EmailAddress_ValidEmails_ReturnsOk(string email)
    {
        var result = EmailAddress.Create(email);

        Assert.True(result.IsOk);
        Assert.Equal(email.Trim().ToLowerInvariant(), result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmailAddress_EmptyOrWhitespace_ReturnsErr(string? email)
    {
        var result = EmailAddress.Create(email!);

        Assert.True(result.IsErr);
        var error = Assert.IsType<UserError.Validation>(result.Error);
        Assert.Contains("required", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@missing-local.com")]
    [InlineData("missing-domain@")]
    [InlineData("missing@.com")]
    public void EmailAddress_InvalidFormat_ReturnsErr(string email)
    {
        var result = EmailAddress.Create(email);

        Assert.True(result.IsErr);
        var error = Assert.IsType<UserError.Validation>(result.Error);
        Assert.Contains("format", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmailAddress_NormalizesToLowercase()
    {
        var result = EmailAddress.Create("USER@EXAMPLE.COM");

        Assert.True(result.IsOk);
        Assert.Equal("user@example.com", result.Value.Value);
    }

    // =========================================================================
    // Username
    // =========================================================================

    [Theory]
    [InlineData("alice")]
    [InlineData("bob123")]
    [InlineData("user-name")]
    [InlineData("user_name")]
    [InlineData("a")]
    public void Username_ValidNames_ReturnsOk(string username)
    {
        var result = Username.Create(username);

        Assert.True(result.IsOk);
        Assert.Equal(username.Trim(), result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Username_EmptyOrWhitespace_ReturnsErr(string? username)
    {
        var result = Username.Create(username!);

        Assert.True(result.IsErr);
    }

    [Theory]
    [InlineData("-startswithhyphen")]
    [InlineData("_startsunderscore")]
    [InlineData("has spaces")]
    [InlineData("has@special!chars")]
    [InlineData("toolongusernamethatexceedstwentycharacters")]
    public void Username_InvalidFormat_ReturnsErr(string username)
    {
        var result = Username.Create(username);

        Assert.True(result.IsErr);
    }

    // =========================================================================
    // Password
    // =========================================================================

    [Theory]
    [InlineData("password")]
    [InlineData("12345678")]
    [InlineData("a very long password with many characters")]
    public void Password_ValidPasswords_ReturnsOk(string password)
    {
        var result = Password.Create(password);

        Assert.True(result.IsOk);
        Assert.Equal(password, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Password_EmptyOrWhitespace_ReturnsErr(string? password)
    {
        var result = Password.Create(password!);

        Assert.True(result.IsErr);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Password_TooShort_ReturnsErr(string password)
    {
        var result = Password.Create(password);

        Assert.True(result.IsErr);
        var error = Assert.IsType<UserError.Validation>(result.Error);
        Assert.Contains("8", error.Message);
    }

    [Fact]
    public void Password_ToString_MasksValue()
    {
        var result = Password.Create("supersecret");

        Assert.True(result.IsOk);
        Assert.Equal("****", result.Value.ToString());
    }

    // =========================================================================
    // Bio
    // =========================================================================

    [Fact]
    public void Bio_ValidText_ReturnsOk()
    {
        var result = Bio.Create("I am a developer.");

        Assert.True(result.IsOk);
        Assert.Equal("I am a developer.", result.Value.Value);
    }

    [Fact]
    public void Bio_EmptyString_ReturnsOk()
    {
        var result = Bio.Create("");

        Assert.True(result.IsOk);
        Assert.Equal("", result.Value.Value);
    }

    [Fact]
    public void Bio_TooLong_ReturnsErr()
    {
        var longBio = new string('a', 301);

        var result = Bio.Create(longBio);

        Assert.True(result.IsErr);
        var error = Assert.IsType<UserError.Validation>(result.Error);
        Assert.Contains("300", error.Message);
    }

    // =========================================================================
    // ImageUrl
    // =========================================================================

    [Theory]
    [InlineData("https://example.com/avatar.png")]
    [InlineData("http://example.com/image.jpg")]
    public void ImageUrl_ValidUrls_ReturnsOk(string url)
    {
        var result = ImageUrl.Create(url);

        Assert.True(result.IsOk);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ImageUrl_EmptyOrWhitespace_ReturnsErr(string? url)
    {
        var result = ImageUrl.Create(url!);

        Assert.True(result.IsErr);
    }

    [Theory]
    [InlineData("ftp://example.com/file")]
    [InlineData("not a url")]
    [InlineData("file:///etc/passwd")]
    public void ImageUrl_InvalidScheme_ReturnsErr(string url)
    {
        var result = ImageUrl.Create(url);

        Assert.True(result.IsErr);
    }

    // =========================================================================
    // Token
    // =========================================================================

    [Fact]
    public void Token_ToString_MasksValue()
    {
        var token = new Token("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload.signature");

        Assert.Equal("jwt.***", token.ToString());
    }
}
