// =============================================================================
// Navigation Tests
// =============================================================================
// Tests for the navigation module: URL parsing, navigation commands,
// navigation callbacks, and the UrlChanges subscription.
//
// These are pure unit tests — no browser or JS interop involved.
// Navigation commands are tested by verifying their types and data.
// The UrlChanges subscription is tested via the NavigationCallbacks bridge.
//
// See also:
//   - Navigation.cs — navigation commands and subscriptions
//   - Program.cs — Url, UrlRequest, UrlChanged types
// =============================================================================

using Picea.Abies.Subscriptions;
using Picea;

namespace Picea.Abies.Tests;

public class NavigationTests
{
    // =========================================================================
    // Url Parsing — FromUri
    // =========================================================================

    [Fact]
    public void Url_FromUri_ParsesSimplePath()
    {
        var uri = new Uri("https://example.com/articles/my-slug");
        var url = Url.FromUri(uri);

        Assert.Equal(["articles", "my-slug"], url.Path);
        Assert.Empty(url.Query);
        Assert.True(url.Fragment.IsNone);
    }

    [Fact]
    public void Url_FromUri_ParsesRootPath()
    {
        var uri = new Uri("https://example.com/");
        var url = Url.FromUri(uri);

        Assert.Empty(url.Path);
        Assert.Empty(url.Query);
    }

    [Fact]
    public void Url_FromUri_ParsesQueryParameters()
    {
        var uri = new Uri("https://example.com/search?q=hello&page=2");
        var url = Url.FromUri(uri);

        Assert.Equal(["search"], url.Path);
        Assert.Equal("hello", url.Query["q"]);
        Assert.Equal("2", url.Query["page"]);
    }

    [Fact]
    public void Url_FromUri_ParsesFragment()
    {
        var uri = new Uri("https://example.com/articles#comments");
        var url = Url.FromUri(uri);

        Assert.Equal(["articles"], url.Path);
        Assert.True(url.Fragment.IsSome);
        Assert.Equal("comments", url.Fragment.Value);
    }

    [Fact]
    public void Url_FromUri_ParsesQueryAndFragment()
    {
        var uri = new Uri("https://example.com/search?q=test#results");
        var url = Url.FromUri(uri);

        Assert.Equal(["search"], url.Path);
        Assert.Equal("test", url.Query["q"]);
        Assert.True(url.Fragment.IsSome);
        Assert.Equal("results", url.Fragment.Value);
    }

    [Fact]
    public void Url_FromUri_DecodesEncodedComponents()
    {
        var uri = new Uri("https://example.com/tag/hello%20world?name=foo%26bar");
        var url = Url.FromUri(uri);

        Assert.Equal(["tag", "hello world"], url.Path);
        Assert.Equal("foo&bar", url.Query["name"]);
    }

    [Fact]
    public void Url_FromUri_HandlesEmptyQuery()
    {
        var uri = new Uri("https://example.com/articles?");
        var url = Url.FromUri(uri);

        Assert.Equal(["articles"], url.Path);
        Assert.Empty(url.Query);
    }

    [Fact]
    public void Url_FromUri_HandlesMultipleSlashes()
    {
        var uri = new Uri("https://example.com/a/b/c/d");
        var url = Url.FromUri(uri);

        Assert.Equal(["a", "b", "c", "d"], url.Path);
    }

    // =========================================================================
    // Url — ToRelativeUri
    // =========================================================================

    [Fact]
    public void Url_ToRelativeUri_RootPath()
    {
        var url = Url.Root;

        Assert.Equal("/", url.ToRelativeUri());
    }

    [Fact]
    public void Url_ToRelativeUri_SimplePath()
    {
        var url = new Url(
            ["articles", "my-slug"],
            new Dictionary<string, string>(),
            Option<string>.None);

        Assert.Equal("/articles/my-slug", url.ToRelativeUri());
    }

    [Fact]
    public void Url_ToRelativeUri_WithQuery()
    {
        var url = new Url(
            ["search"],
            new Dictionary<string, string> { ["q"] = "hello", ["page"] = "2" },
            Option<string>.None);

        var relative = url.ToRelativeUri();
        Assert.StartsWith("/search?", relative);
        Assert.Contains("q=hello", relative);
        Assert.Contains("page=2", relative);
    }

    [Fact]
    public void Url_ToRelativeUri_WithFragment()
    {
        var url = new Url(
            ["articles"],
            new Dictionary<string, string>(),
            Option.Some("comments"));

        Assert.Equal("/articles#comments", url.ToRelativeUri());
    }

    [Fact]
    public void Url_ToRelativeUri_WithQueryAndFragment()
    {
        var url = new Url(
            ["search"],
            new Dictionary<string, string> { ["q"] = "test" },
            Option.Some("results"));

        Assert.Equal("/search?q=test#results", url.ToRelativeUri());
    }

    [Fact]
    public void Url_ToRelativeUri_EncodesSpecialCharacters()
    {
        var url = new Url(
            ["search"],
            new Dictionary<string, string> { ["q"] = "hello world" },
            Option<string>.None);

        var relative = url.ToRelativeUri();
        Assert.Contains("q=hello%20world", relative);
    }

    // =========================================================================
    // Url — Roundtrip
    // =========================================================================

    [Theory]
    [InlineData("https://example.com/articles/my-slug")]
    [InlineData("https://example.com/")]
    [InlineData("https://example.com/search?q=hello")]
    [InlineData("https://example.com/page#section")]
    public void Url_Roundtrip_FromUri_ToRelativeUri(string original)
    {
        var uri = new Uri(original);
        var url = Url.FromUri(uri);
        var relative = url.ToRelativeUri();

        // Parse the relative URI back using a base URI
        var roundtrip = Url.FromUri(new Uri(new Uri("https://example.com"), relative));

        Assert.Equal(url.Path, roundtrip.Path);
        Assert.Equal(url.Query, roundtrip.Query);
    }

    // =========================================================================
    // Url.Root
    // =========================================================================

    [Fact]
    public void Url_Root_HasEmptyComponents()
    {
        Assert.Empty(Url.Root.Path);
        Assert.Empty(Url.Root.Query);
        Assert.True(Url.Root.Fragment.IsNone);
    }

    // =========================================================================
    // Navigation Commands — Factory Methods
    // =========================================================================

    [Fact]
    public void PushUrl_ReturnsNavigationCommandPush()
    {
        var url = new Url(["articles"], new Dictionary<string, string>(), Option<string>.None);
        var command = Navigation.PushUrl(url);

        var push = Assert.IsType<NavigationCommand.Push>(command);
        Assert.Equal(url, push.Url);
    }

    [Fact]
    public void ReplaceUrl_ReturnsNavigationCommandReplace()
    {
        var url = new Url(["login"], new Dictionary<string, string>(), Option<string>.None);
        var command = Navigation.ReplaceUrl(url);

        var replace = Assert.IsType<NavigationCommand.Replace>(command);
        Assert.Equal(url, replace.Url);
    }

    [Fact]
    public void Back_ReturnsNavigationCommandGoBack()
    {
        var command = Navigation.Back;

        Assert.IsType<NavigationCommand.GoBack>(command);
    }

    [Fact]
    public void Forward_ReturnsNavigationCommandGoForward()
    {
        var command = Navigation.Forward;

        Assert.IsType<NavigationCommand.GoForward>(command);
    }

    [Fact]
    public void ExternalUrl_ReturnsNavigationCommandExternal()
    {
        var command = Navigation.ExternalUrl("https://github.com");

        var external = Assert.IsType<NavigationCommand.External>(command);
        Assert.Equal("https://github.com", external.Href);
    }

    // =========================================================================
    // Navigation Commands — Are Commands
    // =========================================================================

    [Fact]
    public void NavigationCommands_ImplementCommandInterface()
    {
        Assert.IsAssignableFrom<Command>(Navigation.PushUrl(Url.Root));
        Assert.IsAssignableFrom<Command>(Navigation.ReplaceUrl(Url.Root));
        Assert.IsAssignableFrom<Command>(Navigation.Back);
        Assert.IsAssignableFrom<Command>(Navigation.Forward);
        Assert.IsAssignableFrom<Command>(Navigation.ExternalUrl("https://example.com"));
    }

    // =========================================================================
    // Navigation Commands — Batchable
    // =========================================================================

    [Fact]
    public void NavigationCommand_CanBeBatchedWithRegularCommands()
    {
        var batch = Commands.Batch(
            Navigation.PushUrl(Url.Root),
            Commands.None);

        var batchCmd = Assert.IsType<Command.Batch>(batch);
        Assert.Equal(2, batchCmd.Commands.Count);
        Assert.IsType<NavigationCommand.Push>(batchCmd.Commands[0]);
    }

    // =========================================================================
    // NavigationCallbacks — URL Change Handling
    // =========================================================================

    [Fact]
    public void HandleUrlChanged_DispatchesParsedUrl()
    {
        Url? received = null;
        NavigationCallbacks.OnUrlChange = url => received = url;

        try
        {
            NavigationCallbacks.HandleUrlChanged("/articles/my-slug?page=1#comments");

            Assert.NotNull(received);
            Assert.Equal(["articles", "my-slug"], received.Path);
            Assert.Equal("1", received.Query["page"]);
            Assert.True(received.Fragment.IsSome);
            Assert.Equal("comments", received.Fragment.Value);
        }
        finally
        {
            NavigationCallbacks.OnUrlChange = null;
        }
    }

    [Fact]
    public void HandleUrlChanged_RootPath_DispatchesEmptyPath()
    {
        Url? received = null;
        NavigationCallbacks.OnUrlChange = url => received = url;

        try
        {
            NavigationCallbacks.HandleUrlChanged("/");

            Assert.NotNull(received);
            Assert.Empty(received.Path);
        }
        finally
        {
            NavigationCallbacks.OnUrlChange = null;
        }
    }

    [Fact]
    public void HandleUrlChanged_NoCallback_DoesNotThrow()
    {
        NavigationCallbacks.OnUrlChange = null;

        var exception = Record.Exception(() =>
            NavigationCallbacks.HandleUrlChanged("/some/path"));

        Assert.Null(exception);
    }

    [Fact]
    public void HandleUrlChanged_AbsoluteUrl_ParsesCorrectly()
    {
        Url? received = null;
        NavigationCallbacks.OnUrlChange = url => received = url;

        try
        {
            NavigationCallbacks.HandleUrlChanged("https://localhost:8080/articles?tag=elm");

            Assert.NotNull(received);
            Assert.Equal(["articles"], received.Path);
            Assert.Equal("elm", received.Query["tag"]);
        }
        finally
        {
            NavigationCallbacks.OnUrlChange = null;
        }
    }

    // =========================================================================
    // UrlChanges Subscription — Creation
    // =========================================================================

    [Fact]
    public void UrlChanges_ReturnsSubscriptionSource()
    {
        var subscription = Navigation.UrlChanges(url => new UrlChanged(url));

        var source = Assert.IsType<Subscription.Source>(subscription);
        Assert.Equal("navigation:urlChanges", source.Key.Value);
    }

    [Fact]
    public void UrlChanges_CanBeBatchedWithOtherSubscriptions()
    {
        var sub = SubscriptionModule.Batch(
            Navigation.UrlChanges(url => new UrlChanged(url)),
            SubscriptionModule.Every(TimeSpan.FromSeconds(1), () => new UrlChanged(Url.Root)));

        var batch = Assert.IsType<Subscription.Batch>(sub);
        Assert.Equal(2, batch.Subscriptions.Count);
    }

    // =========================================================================
    // Navigation.ParseUrl — Helper
    // =========================================================================

    [Fact]
    public void ParseUrl_RelativePath_ParsesCorrectly()
    {
        var url = Navigation.ParseUrl("/articles/my-slug");

        Assert.Equal(["articles", "my-slug"], url.Path);
    }

    [Fact]
    public void ParseUrl_AbsoluteUrl_ParsesCorrectly()
    {
        var url = Navigation.ParseUrl("https://example.com/search?q=test");

        Assert.Equal(["search"], url.Path);
        Assert.Equal("test", url.Query["q"]);
    }

    [Fact]
    public void ParseUrl_RootPath_ReturnsEmptyPath()
    {
        var url = Navigation.ParseUrl("/");

        Assert.Empty(url.Path);
    }

    [Fact]
    public void ParseUrl_WithFragment_ParsesFragment()
    {
        var url = Navigation.ParseUrl("/page#section");

        Assert.Equal(["page"], url.Path);
        Assert.True(url.Fragment.IsSome);
        Assert.Equal("section", url.Fragment.Value);
    }

    // =========================================================================
    // UrlChanged Message Type
    // =========================================================================

    [Fact]
    public void UrlChanged_IsMessage()
    {
        var url = new Url(["articles"], new Dictionary<string, string>(), Option<string>.None);
        var msg = new UrlChanged(url);

        Assert.IsAssignableFrom<Message>(msg);
        Assert.Equal(url, msg.Url);
    }

    // =========================================================================
    // UrlRequest Types
    // =========================================================================

    [Fact]
    public void UrlRequest_Internal_IsMessage()
    {
        var url = new Url(["articles"], new Dictionary<string, string>(), Option<string>.None);
        var request = new UrlRequest.Internal(url);

        Assert.IsAssignableFrom<Message>(request);
        Assert.Equal(url, request.Url);
    }

    [Fact]
    public void UrlRequest_External_IsMessage()
    {
        var request = new UrlRequest.External("https://github.com");

        Assert.IsAssignableFrom<Message>(request);
        Assert.Equal("https://github.com", request.Href);
    }

    // =========================================================================
    // NavigationCommand — Pattern Matching
    // =========================================================================

    [Fact]
    public void NavigationCommand_CanBePatternMatched()
    {
        var commands = new Command[]
        {
            Navigation.PushUrl(Url.Root),
            Navigation.ReplaceUrl(Url.Root),
            Navigation.Back,
            Navigation.Forward,
            Navigation.ExternalUrl("https://example.com"),
            Commands.None
        };

        var navCount = 0;
        var otherCount = 0;

        foreach (var command in commands)
        {
            switch (command)
            {
                case NavigationCommand:
                    navCount++;
                    break;
                default:
                    otherCount++;
                    break;
            }
        }

        Assert.Equal(5, navCount);
        Assert.Equal(1, otherCount);
    }

    [Fact]
    public void NavigationCommand_Push_PatternMatchesWithUrl()
    {
        var url = new Url(["articles", "test"], new Dictionary<string, string>(), Option<string>.None);
        Command command = Navigation.PushUrl(url);

        var matched = command switch
        {
            NavigationCommand.Push push => push.Url.ToRelativeUri(),
            _ => "unknown"
        };

        Assert.Equal("/articles/test", matched);
    }

    [Fact]
    public void NavigationCommand_External_PatternMatchesWithHref()
    {
        Command command = Navigation.ExternalUrl("https://github.com");

        var matched = command switch
        {
            NavigationCommand.External ext => ext.Href,
            _ => "unknown"
        };

        Assert.Equal("https://github.com", matched);
    }
}
