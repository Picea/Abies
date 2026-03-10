// =============================================================================
// Settings Transition Tests — Pure State Machine Verification
// =============================================================================
// Tests for the Settings page transitions in the Conduit MVU program.
// Verifies form input handling, submission, API response processing,
// and logout behavior — all as pure function tests.
// =============================================================================

using Picea.Abies.Conduit.App;
using FluentAssertions;

namespace Picea.Abies.Conduit.Wasm.Tests;

public class SettingsTransitionTests
{
    private static readonly Session _testSession = new(
        Token: "test-token",
        Username: "testuser",
        Email: "test@example.com",
        Bio: "Test bio",
        Image: "https://example.com/avatar.jpg");

    /// <summary>Creates a model with the Settings page pre-populated from session.</summary>
    private static Model CreateSettingsModel() =>
        new(
            Page: new Page.Settings(new SettingsModel(
                Image: _testSession.Image ?? "",
                Username: _testSession.Username,
                Bio: _testSession.Bio,
                Email: _testSession.Email,
                Password: "",
                Errors: [],
                IsSubmitting: false)),
            Session: _testSession,
            ApiUrl: "http://localhost:5000");

    [Fact]
    public void ImageChanged_UpdatesImageField()
    {
        var model = CreateSettingsModel();
        var (newModel, command) = ConduitProgram.Transition(model, new SettingsImageChanged("https://new-image.jpg"));

        var settings = newModel.Page.Should().BeOfType<Page.Settings>().Subject;
        settings.Data.Image.Should().Be("https://new-image.jpg");
        command.Should().Be(Commands.None);
    }

    [Fact]
    public void UsernameChanged_UpdatesUsernameField()
    {
        var model = CreateSettingsModel();
        var (newModel, _) = ConduitProgram.Transition(model, new SettingsUsernameChanged("newname"));

        var settings = newModel.Page.Should().BeOfType<Page.Settings>().Subject;
        settings.Data.Username.Should().Be("newname");
    }

    [Fact]
    public void BioChanged_UpdatesBioField()
    {
        var model = CreateSettingsModel();
        var (newModel, _) = ConduitProgram.Transition(model, new SettingsBioChanged("New bio text"));

        var settings = newModel.Page.Should().BeOfType<Page.Settings>().Subject;
        settings.Data.Bio.Should().Be("New bio text");
    }

    [Fact]
    public void EmailChanged_UpdatesEmailField()
    {
        var model = CreateSettingsModel();
        var (newModel, _) = ConduitProgram.Transition(model, new SettingsEmailChanged("new@email.com"));

        var settings = newModel.Page.Should().BeOfType<Page.Settings>().Subject;
        settings.Data.Email.Should().Be("new@email.com");
    }

    [Fact]
    public void PasswordChanged_UpdatesPasswordField()
    {
        var model = CreateSettingsModel();
        var (newModel, _) = ConduitProgram.Transition(model, new SettingsPasswordChanged("newpass123"));

        var settings = newModel.Page.Should().BeOfType<Page.Settings>().Subject;
        settings.Data.Password.Should().Be("newpass123");
    }

    [Fact]
    public void Submitted_SetsSubmittingAndClearsErrors()
    {
        var model = CreateSettingsModel();
        var settingsPage = (Page.Settings)model.Page;
        var withErrors = model with
        {
            Page = new Page.Settings(settingsPage.Data with { Errors = ["old error"] })
        };

        var (newModel, command) = ConduitProgram.Transition(withErrors, new SettingsSubmitted());

        var settings = newModel.Page.Should().BeOfType<Page.Settings>().Subject;
        settings.Data.IsSubmitting.Should().BeTrue();
        settings.Data.Errors.Should().BeEmpty();
        command.Should().BeOfType<UpdateUser>();
    }

    [Fact]
    public void Submitted_SendsUpdateUserCommand_WithFormData()
    {
        var model = CreateSettingsModel();
        var settingsPage = (Page.Settings)model.Page;
        var withUpdatedFields = model with
        {
            Page = new Page.Settings(settingsPage.Data with
            {
                Image = "https://new.jpg",
                Username = "updated",
                Bio = "Updated bio",
                Email = "updated@test.com",
                Password = "newpass"
            })
        };

        var (_, command) = ConduitProgram.Transition(withUpdatedFields, new SettingsSubmitted());

        var updateCmd = command.Should().BeOfType<UpdateUser>().Subject;
        updateCmd.Image.Should().Be("https://new.jpg");
        updateCmd.Username.Should().Be("updated");
        updateCmd.Bio.Should().Be("Updated bio");
        updateCmd.Email.Should().Be("updated@test.com");
        updateCmd.Password.Should().Be("newpass");
        updateCmd.Token.Should().Be(_testSession.Token);
    }

    [Fact]
    public void Submitted_EmptyPassword_SendsNullPassword()
    {
        var model = CreateSettingsModel();

        var (_, command) = ConduitProgram.Transition(model, new SettingsSubmitted());

        var updateCmd = command.Should().BeOfType<UpdateUser>().Subject;
        updateCmd.Password.Should().BeNull();
    }

    [Fact]
    public void Submitted_WhitespacePassword_SendsNullPassword()
    {
        var model = CreateSettingsModel();
        var settingsPage = (Page.Settings)model.Page;
        var withWhitespace = model with
        {
            Page = new Page.Settings(settingsPage.Data with { Password = "   " })
        };

        var (_, command) = ConduitProgram.Transition(withWhitespace, new SettingsSubmitted());

        var updateCmd = command.Should().BeOfType<UpdateUser>().Subject;
        updateCmd.Password.Should().BeNull();
    }

    [Fact]
    public void UserUpdated_UpdatesSessionAndResetsForm()
    {
        var model = CreateSettingsModel();
        var updatedSession = new Session("new-token", "newuser", "new@test.com", "New bio", "https://new.jpg");

        var (newModel, _) = ConduitProgram.Transition(model, new UserUpdated(updatedSession));

        newModel.Session.Should().Be(updatedSession);
        var settings = newModel.Page.Should().BeOfType<Page.Settings>().Subject;
        settings.Data.Username.Should().Be("newuser");
        settings.Data.Email.Should().Be("new@test.com");
        settings.Data.Bio.Should().Be("New bio");
        settings.Data.Image.Should().Be("https://new.jpg");
        settings.Data.Password.Should().BeEmpty();
        settings.Data.IsSubmitting.Should().BeFalse();
    }

    [Fact]
    public void ApiError_ShowsErrorsAndStopsSubmitting()
    {
        var model = CreateSettingsModel();
        var settingsPage = (Page.Settings)model.Page;
        var submitting = model with
        {
            Page = new Page.Settings(settingsPage.Data with { IsSubmitting = true })
        };

        var errors = new List<string> { "Username taken", "Email invalid" };
        var (newModel, _) = ConduitProgram.Transition(submitting, new ApiError(errors));

        var settings = newModel.Page.Should().BeOfType<Page.Settings>().Subject;
        settings.Data.Errors.Should().BeEquivalentTo(errors);
        settings.Data.IsSubmitting.Should().BeFalse();
    }

    [Fact]
    public void Logout_ClearsSessionAndNavigatesToHome()
    {
        var model = CreateSettingsModel();

        var (newModel, _) = ConduitProgram.Transition(model, new Logout());

        newModel.Session.Should().BeNull();
        newModel.Page.Should().BeOfType<Page.Home>();
    }
}
