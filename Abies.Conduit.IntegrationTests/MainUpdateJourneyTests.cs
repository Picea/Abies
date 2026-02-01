using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Main;
using Abies.Conduit.Services;
using Abies.DOM;
using Xunit;
using MainPage = Abies.Conduit.Main.Page;

namespace Abies.Conduit.IntegrationTests;

/// <summary>
/// Journey tests that exercise the Main.Update function (top-level MVU loop),
/// not just page-level Update functions. These tests verify that messages
/// handled at the main level properly update the app's navigation state.
/// </summary>
public class MainUpdateJourneyTests
{
    private static void ConfigureFakeApi(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("http://fake") };
        ApiClient.ConfigureHttpClient(httpClient);
        ApiClient.ConfigureBaseUrl("http://fake/api");
        Storage.Configure(new InMemoryStorageProvider());
    }

    #region Register Page → Main Navigation

    /// <summary>
    /// Bug: After successful signup, the navigation header should show authenticated links
    /// (New Article, Settings, username) instead of "Sign in" / "Sign up".
    /// 
    /// This test verifies that when RegisterSuccess is dispatched and handled by Main.Update,
    /// the model's CurrentUser is set and Navigation.View renders authenticated links.
    /// </summary>
    [Fact]
    public void Register_Success_UpdatesNavigationToShowAuthenticatedLinks()
    {
        // Arrange: Create a Main.Model with Register page active
        var registerModel = new Abies.Conduit.Page.Register.Model(
            Username: "newuser",
            Email: "newuser@example.com",
            Password: "password123",
            IsSubmitting: true, // Just submitted
            Errors: null,
            CurrentUser: null);

        var mainModel = new Model(
            Page: new MainPage.Register(registerModel),
            CurrentRoute: new Abies.Conduit.Routing.Route.Register(),
            CurrentUser: null);

        // Verify initial state: navigation shows unauthenticated links
        var navBefore = Navigation.View(mainModel);
        var signInBefore = MvuDomTestHarness.TryFindFirstElement(navBefore,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "Sign in"));
        var signUpBefore = MvuDomTestHarness.TryFindFirstElement(navBefore,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "Sign up"));
        
        Assert.NotNull(signInBefore); // Should see Sign in link
        Assert.NotNull(signUpBefore); // Should see Sign up link

        // Act: Dispatch RegisterSuccess message to Main.Update
        var registeredUser = new User(
            Username: new UserName("newuser"),
            Email: new Email("newuser@example.com"),
            Token: new Token("jwt-token-123"),
            Image: "",
            Bio: "");
        
        var registerSuccessMsg = new Abies.Conduit.Page.Register.Message.RegisterSuccess(registeredUser);
        var (updatedModel, _) = Program.Update(registerSuccessMsg, mainModel);

        // Assert: Model should have CurrentUser set
        Assert.NotNull(updatedModel.CurrentUser);
        Assert.Equal("newuser", updatedModel.CurrentUser!.Username.Value);
        Assert.Equal("newuser@example.com", updatedModel.CurrentUser.Email.Value);

        // Assert: Navigation should now show authenticated links
        var navAfter = Navigation.View(updatedModel);
        
        var signInAfter = MvuDomTestHarness.TryFindFirstElement(navAfter,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "Sign in"));
        var signUpAfter = MvuDomTestHarness.TryFindFirstElement(navAfter,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "Sign up"));
        var newArticleLink = MvuDomTestHarness.TryFindFirstElement(navAfter,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "New Article"));
        var settingsLink = MvuDomTestHarness.TryFindFirstElement(navAfter,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "Settings"));
        var usernameLink = MvuDomTestHarness.TryFindFirstElement(navAfter,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "newuser"));

        Assert.Null(signInAfter); // Should NOT see Sign in link anymore
        Assert.Null(signUpAfter); // Should NOT see Sign up link anymore
        Assert.NotNull(newArticleLink); // Should see New Article link
        Assert.NotNull(settingsLink); // Should see Settings link
        Assert.NotNull(usernameLink); // Should see username link
    }

    /// <summary>
    /// Tests the full flow including the UrlChanged message that follows RegisterSuccess.
    /// This simulates what the runtime does: RegisterSuccess → PushState → UrlChanged.
    /// </summary>
    [Fact]
    public void Register_Success_FollowedByUrlChanged_PreservesCurrentUser()
    {
        // Arrange: Create a Main.Model with Register page active
        var registerModel = new Abies.Conduit.Page.Register.Model(
            Username: "newuser",
            Email: "newuser@example.com",
            Password: "password123",
            IsSubmitting: true,
            Errors: null,
            CurrentUser: null);

        var mainModel = new Model(
            Page: new MainPage.Register(registerModel),
            CurrentRoute: new Abies.Conduit.Routing.Route.Register(),
            CurrentUser: null);

        // Act 1: Dispatch RegisterSuccess message
        var registeredUser = new User(
            Username: new UserName("newuser"),
            Email: new Email("newuser@example.com"),
            Token: new Token("jwt-token-123"),
            Image: "",
            Bio: "");
        
        var registerSuccessMsg = new Abies.Conduit.Page.Register.Message.RegisterSuccess(registeredUser);
        var (afterRegister, command) = Program.Update(registerSuccessMsg, mainModel);

        // Verify command includes PushState
        Assert.IsType<Abies.Command.Batch>(command);
        var batch = (Abies.Command.Batch)command;
        Assert.Contains(batch.Commands, c => c is Abies.Navigation.Command.PushState);

        // Act 2: Simulate what the runtime does - dispatch UrlChanged after PushState
        var urlChangedMsg = Program.OnUrlChanged(Url.Create("/"));
        var (afterUrlChanged, _) = Program.Update(urlChangedMsg, afterRegister);

        // Assert: CurrentUser should still be set after UrlChanged
        Assert.NotNull(afterUrlChanged.CurrentUser);
        Assert.Equal("newuser", afterUrlChanged.CurrentUser!.Username.Value);

        // Assert: Navigation should still show authenticated links
        var navAfter = Navigation.View(afterUrlChanged);
        var newArticleLink = MvuDomTestHarness.TryFindFirstElement(navAfter,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "New Article"));
        Assert.NotNull(newArticleLink);
    }

    /// <summary>
    /// Same test for Login - ensures LoginSuccess also updates navigation.
    /// </summary>
    [Fact]
    public void Login_Success_UpdatesNavigationToShowAuthenticatedLinks()
    {
        // Arrange: Create a Main.Model with Login page active
        var loginModel = new Abies.Conduit.Page.Login.Model(
            Email: "user@example.com",
            Password: "password123",
            IsSubmitting: true,
            Errors: null,
            CurrentUser: null);

        var mainModel = new Model(
            Page: new MainPage.Login(loginModel),
            CurrentRoute: new Abies.Conduit.Routing.Route.Login(),
            CurrentUser: null);

        // Act: Dispatch LoginSuccess message to Main.Update
        var loggedInUser = new User(
            Username: new UserName("existinguser"),
            Email: new Email("user@example.com"),
            Token: new Token("jwt-token-456"),
            Image: "",
            Bio: "");
        
        var loginSuccessMsg = new Abies.Conduit.Page.Login.Message.LoginSuccess(loggedInUser);
        var (updatedModel, _) = Program.Update(loginSuccessMsg, mainModel);

        // Assert: Model should have CurrentUser set
        Assert.NotNull(updatedModel.CurrentUser);
        Assert.Equal("existinguser", updatedModel.CurrentUser!.Username.Value);

        // Assert: Navigation should show authenticated links
        var navAfter = Navigation.View(updatedModel);
        
        var newArticleLink = MvuDomTestHarness.TryFindFirstElement(navAfter,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "New Article"));
        var settingsLink = MvuDomTestHarness.TryFindFirstElement(navAfter,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "Settings"));

        Assert.NotNull(newArticleLink);
        Assert.NotNull(settingsLink);
    }

    /// <summary>
    /// Full async journey test that exercises the complete flow through HandleCommand,
    /// including the actual API call and dispatched messages.
    /// This is the most realistic test of what happens in the browser.
    /// </summary>
    [Fact]
    public async Task Register_FullJourney_NavigationUpdatesAfterApiCall()
    {
        // Arrange: Set up fake API
        var handler = new FakeHttpMessageHandler { StrictMode = true };
        
        handler.When(
            HttpMethod.Post,
            "/api/users",
            HttpStatusCode.OK,
            new
            {
                user = new
                {
                    email = "newuser@example.com",
                    token = "jwt-token-789",
                    username = "newuser",
                    bio = "",
                    image = ""
                }
            });

        ConfigureFakeApi(handler);

        // Create Main.Model with Register page active
        var registerModel = new Abies.Conduit.Page.Register.Model(
            Username: "newuser",
            Email: "newuser@example.com",
            Password: "password123",
            IsSubmitting: false,
            Errors: null,
            CurrentUser: null);

        var mainModel = new Model(
            Page: new MainPage.Register(registerModel),
            CurrentRoute: new Abies.Conduit.Routing.Route.Register(),
            CurrentUser: null);

        // Act: Run the full MVU loop with Main.Update (not page-level Update)
        var submitMsg = new Abies.Conduit.Page.Register.Message.RegisterSubmitted();
        
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: mainModel,
            update: Program.Update,
            initialMessage: submitMsg,
            options: new MvuLoopRuntime.Options(
                MaxIterations: 15,
                StrictUnhandledMessages: false // Some messages may not change model
            ));

        // Assert: CurrentUser should be set on the final model
        Assert.NotNull(result.Model.CurrentUser);
        Assert.Equal("newuser", result.Model.CurrentUser!.Username.Value);

        // Assert: Navigation should show authenticated links
        var nav = Navigation.View(result.Model);
        var newArticleLink = MvuDomTestHarness.TryFindFirstElement(nav,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "New Article"));
        var signInLink = MvuDomTestHarness.TryFindFirstElement(nav,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "Sign in"));

        Assert.NotNull(newArticleLink); // Should see New Article link
        Assert.Null(signInLink); // Should NOT see Sign in link

        // Also verify the full page view has the right navigation
        var fullPage = Program.View(result.Model);
        var navInPage = MvuDomTestHarness.TryFindFirstElement(fullPage.Body,
            el => el.Tag == "nav" && el.Attributes.Any(a => a.Name == "class" && a.Value.Contains("navbar")));
        Assert.NotNull(navInPage);
        
        // Find New Article link within the navbar
        var newArticleInNav = MvuDomTestHarness.TryFindFirstElement(navInPage!,
            el => el.Tag == "a" && el.Children.OfType<Text>().Any(t => t.Value == "New Article"));
        Assert.NotNull(newArticleInNav);

        // Debug: Print the full navigation HTML to see what's actually rendered
        var navHtml = MvuDomTestHarness.Html(navInPage!);
        
        // Verify there's NO "Sign in" or "Sign up" in the rendered HTML
        Assert.DoesNotContain("Sign in", navHtml);
        Assert.DoesNotContain("Sign up", navHtml);
    }

    /// <summary>
    /// Tests DOM diffing when navigation changes from unauthenticated to authenticated state.
    /// This verifies that patches are computed correctly.
    /// </summary>
    [Fact]
    public void Navigation_DomDiff_CorrectlyRemovesUnauthenticatedLinks()
    {
        // Arrange: Create unauthenticated model
        var unauthModel = new Model(
            Page: new MainPage.Home(new Abies.Conduit.Page.Home.Model([], 0, [], Abies.Conduit.Page.Home.FeedTab.Global, "", false, 0, null)),
            CurrentRoute: new Abies.Conduit.Routing.Route.Home(),
            CurrentUser: null);

        // Create authenticated model
        var authUser = new User(
            Username: new UserName("testuser"),
            Email: new Email("test@example.com"),
            Token: new Token("jwt-123"),
            Image: "",
            Bio: "");
        
        var authModel = new Model(
            Page: new MainPage.Home(new Abies.Conduit.Page.Home.Model([], 0, [], Abies.Conduit.Page.Home.FeedTab.Global, "", false, 0, authUser)),
            CurrentRoute: new Abies.Conduit.Routing.Route.Home(),
            CurrentUser: authUser);

        // Get DOM for both states
        var unauthDom = Program.View(unauthModel);
        var authDom = Program.View(authModel);

        // Render HTML for comparison
        var unauthHtml = MvuDomTestHarness.Html(unauthDom.Body);
        var authHtml = MvuDomTestHarness.Html(authDom.Body);

        // Verify unauthenticated state has Sign in/Sign up
        Assert.Contains("Sign in", unauthHtml);
        Assert.Contains("Sign up", unauthHtml);
        Assert.DoesNotContain("New Article", unauthHtml);
        Assert.DoesNotContain("Settings", unauthHtml);

        // Verify authenticated state has New Article/Settings/username
        Assert.DoesNotContain("Sign in", authHtml);
        Assert.DoesNotContain("Sign up", authHtml);
        Assert.Contains("New Article", authHtml);
        Assert.Contains("Settings", authHtml);
        Assert.Contains("testuser", authHtml);

        // Now test DOM diffing - compute patches
        var patches = Abies.DOM.Operations.Diff(unauthDom.Body, authDom.Body);

        // There should be patches that remove Sign in/Sign up and add New Article/Settings
        Assert.NotEmpty(patches);

        // Apply patches and verify final state
        // Note: We can't actually apply patches in tests without the browser, 
        // but we can verify the patches contain the expected operations
    }

    /// <summary>
    /// Detailed test to inspect what patches are generated when navigating 
    /// from unauthenticated to authenticated state.
    /// Per ADR-016: Element IDs are used for keyed diffing, not data-key attributes.
    /// Elements with the same ID should be diffed in place, not replaced.
    /// </summary>
    [Fact]
    public void Navigation_DomDiff_InspectPatches()
    {
        // Arrange: Create unauthenticated navigation
        var unauthModel = new Model(
            Page: new MainPage.Home(new Abies.Conduit.Page.Home.Model([], 0, [], Abies.Conduit.Page.Home.FeedTab.Global, "", false, 0, null)),
            CurrentRoute: new Abies.Conduit.Routing.Route.Home(),
            CurrentUser: null);

        var authUser = new User(
            Username: new UserName("testuser"),
            Email: new Email("test@example.com"),
            Token: new Token("jwt-123"),
            Image: "",
            Bio: "");
        
        var authModel = unauthModel with { CurrentUser = authUser };

        // Get the navigation views specifically
        var navBefore = Navigation.View(unauthModel);
        var navAfter = Navigation.View(authModel);

        // Get the ul element (navigation links container)
        var ulBefore = MvuDomTestHarness.FindFirstElement(navBefore, 
            el => el.Tag == "ul" && el.Attributes.Any(a => a.Name == "class" && a.Value.Contains("navbar-nav")));
        var ulAfter = MvuDomTestHarness.FindFirstElement(navAfter, 
            el => el.Tag == "ul" && el.Attributes.Any(a => a.Name == "class" && a.Value.Contains("navbar-nav")));

        Assert.NotNull(ulBefore);
        Assert.NotNull(ulAfter);

        // Count children (li elements)
        var liBefore = ulBefore!.Children.OfType<Element>().ToList();
        var liAfter = ulAfter!.Children.OfType<Element>().ToList();

        // Unauthenticated: Home, Sign in, Sign up = 3 items
        Assert.Equal(3, liBefore.Count);
        // Authenticated: Home, New Article, Settings, username = 4 items
        Assert.Equal(4, liAfter.Count);

        // Verify element IDs are stable and unique
        var beforeIds = liBefore.Select(li => li.Id).ToList();
        var afterIds = liAfter.Select(li => li.Id).ToList();
        
        // Home should have the same ID in both states
        Assert.Contains("nav-home", beforeIds);
        Assert.Contains("nav-home", afterIds);
        
        // Unauthenticated IDs
        Assert.Contains("nav-login", beforeIds);
        Assert.Contains("nav-register", beforeIds);
        
        // Authenticated IDs
        Assert.Contains("nav-editor", afterIds);
        Assert.Contains("nav-settings", afterIds);
        Assert.Contains("nav-profile-testuser", afterIds);

        // Compute patches for just the navigation ul
        var patches = Abies.DOM.Operations.Diff(ulBefore, ulAfter);

        // Log patches for debugging
        var patchDescriptions = new List<string>();
        foreach (var patch in patches)
        {
            patchDescriptions.Add(patch switch
            {
                AddChild ac => $"AddChild: parent={ac.Parent.Id}, child={ac.Child.Id} ({ac.Child.Tag})",
                RemoveChild rc => $"RemoveChild: parent={rc.Parent.Id}, child={rc.Child.Id} ({rc.Child.Tag})",
                ReplaceChild rpc => $"ReplaceChild: old={rpc.OldElement.Id}, new={rpc.NewElement.Id}",
                UpdateAttribute ua => $"UpdateAttribute: el={ua.Element.Id}, attr={ua.Attribute.Name}={ua.Value}",
                AddAttribute aa => $"AddAttribute: el={aa.Element.Id}, attr={aa.Attribute.Name}",
                RemoveAttribute ra => $"RemoveAttribute: el={ra.Element.Id}, attr={ra.Attribute.Name}",
                UpdateText ut => $"UpdateText: node={ut.Node.Id}, text='{ut.Text}' → newId={ut.NewId}",
                AddText at => $"AddText: parent={at.Parent.Id}, child={at.Child.Id} ('{at.Child.Value}')",
                RemoveText rt => $"RemoveText: parent={rt.Parent.Id}, child={rt.Child.Id}",
                _ => $"Unknown patch type: {patch.GetType().Name}"
            });
        }

        var patchSummary = string.Join("\n", patchDescriptions);
        
        // Per ADR-016: ID-based diffing
        // Elements with different IDs should be removed and added.
        // Elements with same ID (nav-home) should be diffed in place.
        var addChildPatches = patches.OfType<AddChild>().ToList();
        var removeChildPatches = patches.OfType<RemoveChild>().ToList();
        
        // Should remove only nav-login and nav-register (2 items, not 3)
        // nav-home has the same ID so it should NOT be removed
        Assert.Equal(2, removeChildPatches.Count);
        Assert.Contains(removeChildPatches, p => p.Child.Id == "nav-login");
        Assert.Contains(removeChildPatches, p => p.Child.Id == "nav-register");
        Assert.DoesNotContain(removeChildPatches, p => p.Child.Id == "nav-home");
        
        // Should add nav-editor, nav-settings, nav-profile-testuser (3 items, not 4)
        // nav-home is already present so it should NOT be added
        Assert.Equal(3, addChildPatches.Count);
        Assert.Contains(addChildPatches, p => p.Child.Id == "nav-editor");
        Assert.Contains(addChildPatches, p => p.Child.Id == "nav-settings");
        Assert.Contains(addChildPatches, p => p.Child.Id == "nav-profile-testuser");
        Assert.DoesNotContain(addChildPatches, p => p.Child.Id == "nav-home");
        
        // Verify the specific items being removed/added have unique IDs
        var removedIds = removeChildPatches.Select(p => p.Child.Id).Distinct().ToList();
        var addedIds = addChildPatches.Select(p => p.Child.Id).Distinct().ToList();
        
        // All removed items should have unique IDs
        Assert.Equal(2, removedIds.Count); // nav-login, nav-register
        
        // All added items should have unique IDs
        Assert.Equal(3, addedIds.Count); // nav-editor, nav-settings, nav-profile-testuser
    }

    #endregion
}
