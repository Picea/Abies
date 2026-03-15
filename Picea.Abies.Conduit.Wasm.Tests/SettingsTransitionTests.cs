// =============================================================================
// Settings Transition Tests — Pure State Machine Verification
// =============================================================================
// Tests for the Settings page transitions in the Conduit MVU program.
// Verifies form input handling, submission, API response processing,
// and logout behavior — all as pure function tests.
// =============================================================================

using Picea.Abies.Conduit.App;

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

    [Test]
    public async Task ImageChanged_UpdatesImageField()
    {
        var model = CreateSettingsModel();
        var (newModel, command) = ConduitProgram.Transition(model, new SettingsImageChanged("https://new-image.jpg"));

        var settings = await Assert.That(newModel.Page).IsTypeOf<Page.Settings>();
        await Assert.That(settings!.Data.Image).IsEqualTo("https://new-image.jpg");
        await Assert.That(command).IsEqualTo(Commands.None);
    }

    [Test]
    public async Task UsernameChanged_UpdatesUsernameField()
    {
        var model = CreateSettingsModel();
        var (newModel, _) = ConduitProgram.Transition(model, new SettingsUsernameChanged("newname"));

        var settings = await Assert.That(newModel.Page).IsTypeOf<Page.Settings>();
        await Assert.That(settings!.Data.Username).IsEqualTo("newname");
    }

    [Test]
    public async Task BioChanged_UpdatesBioField()
    {
        var model = CreateSettingsModel();
        var (newModel, _) = ConduitProgram.Transition(model, new SettingsBioChanged("New bio text"));

        var settings = await Assert.That(newModel.Page).IsTypeOf<Page.Settings>();
        await Assert.That(settings!.Data.Bio).IsEqualTo("New bio text");
    }

    [Test]
    public async Task EmailChanged_UpdatesEmailField()
    {
        var model = CreateSettingsModel();
        var (newModel, _) = ConduitProgram.Transition(model, new SettingsEmailChanged("new@email.com"));

        var settings = await Assert.That(newModel.Page).IsTypeOf<Page.Settings>();
        await Assert.That(settings!.Data.Email).IsEqualTo("new@email.com");
    }

    [Test]
    public async Task PasswordChanged_UpdatesPasswordField()
    {
        var model = CreateSettingsModel();
        var (newModel, _) = ConduitProgram.Transition(model, new SettingsPasswordChanged("newpass123"));

        var settings = await Assert.That(newModel.Page).IsTypeOf<Page.Settings>();
        await Assert.That(settings!.Data.Password).IsEqualTo("newpass123");
    }

    [Test]
    public async Task Submitted_SetsSubmittingAndClearsErrors()
    {
        var model = CreateSettingsModel();
        var settingsPage = (Page.Settings)model.Page;
        var withErrors = model with
        {
            Page = new Page.Settings(settingsPage.Data with { Errors = ["old error"] })
        };

        var (newModel, command) = ConduitProgram.Transition(withErrors, new SettingsSubmitted());

        var settings = await Assert.That(newModel.Page).IsTypeOf<Page.Settings>();
        await Assert.That(settings!.Data.IsSubmitting).IsTrue();
        await Assert.That(settings.Data.Errors).IsEmpty();
        await Assert.That(command).IsTypeOf<UpdateUser>();
    }

    [Test]
    public async Task Submitted_SendsUpdateUserCommand_WithFormData()
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

        var updateCmd = await Assert.That(command).IsTypeOf<UpdateUser>();
        await Assert.That(updateCmd!.Image).IsEqualTo("https://new.jpg");
        await Assert.That(updateCmd.Username).IsEqualTo("updated");
        await Assert.That(updateCmd.Bio).IsEqualTo("Updated bio");
        await Assert.That(updateCmd.Email).IsEqualTo("updated@test.com");
        await Assert.That(updateCmd.Password).IsEqualTo("newpass");
        await Assert.That(updateCmd.Token).IsEqualTo(_testSession.Token);
    }

    [Test]
    public async Task Submitted_EmptyPassword_SendsNullPassword()
    {
        var model = CreateSettingsModel();

        var (_, command) = ConduitProgram.Transition(model, new SettingsSubmitted());

        var updateCmd = await Assert.That(command).IsTypeOf<UpdateUser>();
        await Assert.That(updateCmd!.Password).IsNull();
    }

    [Test]
    public async Task Submitted_WhitespacePassword_SendsNullPassword()
    {
        var model = CreateSettingsModel();
        var settingsPage = (Page.Settings)model.Page;
        var withWhitespace = model with
        {
            Page = new Page.Settings(settingsPage.Data with { Password = "   " })
        };

        var (_, command) = ConduitProgram.Transition(withWhitespace, new SettingsSubmitted());

        var updateCmd = await Assert.That(command).IsTypeOf<UpdateUser>();
        await Assert.That(updateCmd!.Password).IsNull();
    }

    [Test]
    public async Task UserUpdated_UpdatesSessionAndResetsForm()
    {
        var model = CreateSettingsModel();
        var updatedSession = new Session("new-token", "newuser", "new@test.com", "New bio", "https://new.jpg");

        var (newModel, _) = ConduitProgram.Transition(model, new UserUpdated(updatedSession));

        await Assert.That(newModel.Session).IsEqualTo(updatedSession);
        var settings = await Assert.That(newModel.Page).IsTypeOf<Page.Settings>();
        await Assert.That(settings!.Data.Username).IsEqualTo("newuser");
        await Assert.That(settings.Data.Email).IsEqualTo("new@test.com");
        await Assert.That(settings.Data.Bio).IsEqualTo("New bio");
        await Assert.That(settings.Data.Image).IsEqualTo("https://new.jpg");
        await Assert.That(settings.Data.Password).IsEmpty();
        await Assert.That(settings.Data.IsSubmitting).IsFalse();
    }

    [Test]
    public async Task ApiError_ShowsErrorsAndStopsSubmitting()
    {
        var model = CreateSettingsModel();
        var settingsPage = (Page.Settings)model.Page;
        var submitting = model with
        {
            Page = new Page.Settings(settingsPage.Data with { IsSubmitting = true })
        };

        var errors = new List<string> { "Username taken", "Email invalid" };
        var (newModel, _) = ConduitProgram.Transition(submitting, new ApiError(errors));

        var settings = await Assert.That(newModel.Page).IsTypeOf<Page.Settings>();
        await Assert.That(settings!.Data.Errors).IsEquivalentTo(errors);
        await Assert.That(settings.Data.IsSubmitting).IsFalse();
    }

    [Test]
    public async Task Logout_ClearsSessionAndNavigatesToHome()
    {
        var model = CreateSettingsModel();

        var (newModel, _) = ConduitProgram.Transition(model, new Logout());

        await Assert.That(newModel.Session).IsNull();
        await Assert.That(newModel.Page).IsTypeOf<Page.Home>();
    }
}
