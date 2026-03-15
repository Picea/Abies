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

    [Test]
    public async Task Url_FromUri_ParsesSimplePath()
    {
        var uri = new Uri("https://example.com/articles/my-slug");
        var url = Url.FromUri(uri);

        await Assert.That(url.Path).IsEquivalentTo(new[] { "articles", "my-slug" });
        await Assert.That(url.Query).IsEmpty();
        await Assert.That(url.Fragment.IsNone).IsTrue();
    }

    [Test]
    public async Task Url_FromUri_ParsesRootPath()
    {
        var uri = new Uri("https://example.com/");
        var url = Url.FromUri(uri);

        await Assert.That(url.Path).IsEmpty();
        await Assert.That(url.Query).IsEmpty();
    }

    [Test]
    public async Task Url_FromUri_ParsesQueryParameters()
    {
        var uri = new Uri("https://example.com/search?q=hello&page=2");
        var url = Url.FromUri(uri);

        await Assert.That(url.Path).IsEquivalentTo(new[] { "search" });
        await Assert.That(url.Query["q"]).IsEqualTo("hello");
        await Assert.That(url.Query["page"]).IsEqualTo("2");
    }

    [Test]
    public async Task Url_FromUri_ParsesFragment()
    {
        var uri = new Uri("https://example.com/articles#comments");
        var url = Url.FromUri(uri);

        await Assert.That(url.Path).IsEquivalentTo(new[] { "articles" });
        await Assert.That(url.Fragment.IsSome).IsTrue();
        await Assert.That(url.Fragment.Value).IsEqualTo("comments");
    }

    [Test]
    public async Task Url_FromUri_ParsesQueryAndFragment()
    {
        var uri = new Uri("https://example.com/search?q=test#results");
        var url = Url.FromUri(uri);

        await Assert.That(url.Path).IsEquivalentTo(new[] { "search" });
        await Assert.That(url.Query["q"]).IsEqualTo("test");
        await Assert.That(url.Fragment.IsSome).IsTrue();
        await Assert.That(url.Fragment.Value).IsEqualTo("results");
    }

    [Test]
    public async Task Url_FromUri_DecodesEncodedComponents()
    {
        var uri = new Uri("https://example.com/tag/hello%20world?name=foo%26bar");
        var url = Url.FromUri(uri);

        await Assert.That(url.Path).IsEquivalentTo(new[] { "tag", "hello world" });
        await Assert.That(url.Query["name"]).IsEqualTo("foo&bar");
    }

    [Test]
    public async Task Url_FromUri_HandlesEmptyQuery()
    {
        var uri = new Uri("https://example.com/articles?");
        var url = Url.FromUri(uri);

        await Assert.That(url.Path).IsEquivalentTo(new[] { "articles" });
        await Assert.That(url.Query).IsEmpty();
    }

    [Test]
    public async Task Url_FromUri_HandlesMultipleSlashes()
    {
        var uri = new Uri("https://example.com/a/b/c/d");
        var url = Url.FromUri(uri);

        await Assert.That(url.Path).IsEquivalentTo(new[] { "a", "b", "c", "d" });
    }

    // =========================================================================
    // Url — ToRelativeUri
    // =========================================================================

    [Test]
    public async Task Url_ToRelativeUri_RootPath()
    {
        var url = Url.Root;

        await Assert.That(url.ToRelativeUri()).IsEqualTo("/");
    }

    [Test]
    public async Task Url_ToRelativeUri_SimplePath()
    {
        var url = new Url(
            ["articles", "my-slug"],
            new Dictionary<string, string>(),
            Option<string>.None);

        await Assert.That(url.ToRelativeUri()).IsEqualTo("/articles/my-slug");
    }

    [Test]
    public async Task Url_ToRelativeUri_WithQuery()
    {
        var url = new Url(
            ["search"],
            new Dictionary<string, string> { ["q"] = "hello", ["page"] = "2" },
            Option<string>.None);

        var relative = url.ToRelativeUri();
        await Assert.That(relative).StartsWith("/search?");
        await Assert.That(relative).Contains("q=hello");
        await Assert.That(relative).Contains("page=2");
    }

    [Test]
    public async Task Url_ToRelativeUri_WithFragment()
    {
        var url = new Url(
            ["articles"],
            new Dictionary<string, string>(),
            Option.Some("comments"));

        await Assert.That(url.ToRelativeUri()).IsEqualTo("/articles#comments");
    }

    [Test]
    public async Task Url_ToRelativeUri_WithQueryAndFragment()
    {
        var url = new Url(
            ["search"],
            new Dictionary<string, string> { ["q"] = "test" },
            Option.Some("results"));

        await Assert.That(url.ToRelativeUri()).IsEqualTo("/search?q=test#results");
    }

    [Test]
    public async Task Url_ToRelativeUri_EncodesSpecialCharacters()
    {
        var url = new Url(
            ["search"],
            new Dictionary<string, string> { ["q"] = "hello world" },
            Option<string>.None);

        var relative = url.ToRelativeUri();
        await Assert.That(relative).Contains("q=hello%20world");
    }

    // =========================================================================
    // Url — Roundtrip
    // =========================================================================

    [Test]
    [Arguments("https://example.com/articles/my-slug")]
    [Arguments("https://example.com/")]
    [Arguments("https://example.com/search?q=hello")]
    [Arguments("https://example.com/page#section")]
    public async Task Url_Roundtrip_FromUri_ToRelativeUri(string original)
    {
        var uri = new Uri(original);
        var url = Url.FromUri(uri);
        var relative = url.ToRelativeUri();

        // Parse the relative URI back using a base URI
        var roundtrip = Url.FromUri(new Uri(new Uri("https://example.com"), relative));

        await Assert.That(roundtrip.Path).IsEquivalentTo(url.Path);
        await Assert.That(roundtrip.Query).IsEquivalentTo(url.Query);
    }

    // =========================================================================
    // Url.Root
    // =========================================================================

    [Test]
    public async Task Url_Root_HasEmptyComponents()
    {
        await Assert.That(Url.Root.Path).IsEmpty();
        await Assert.That(Url.Root.Query).IsEmpty();
        await Assert.That(Url.Root.Fragment.IsNone).IsTrue();
    }

    // =========================================================================
    // Navigation Commands — Factory Methods
    // =========================================================================

    [Test]
    public async Task PushUrl_ReturnsNavigationCommandPush()
    {
        var url = new Url(["articles"], new Dictionary<string, string>(), Option<string>.None);
        var command = Navigation.PushUrl(url);

        await Assert.That(command).IsTypeOf<NavigationCommand.Push>();
        var push = (NavigationCommand.Push)command;
        await Assert.That(push.Url).IsEqualTo(url);
    }

    [Test]
    public async Task ReplaceUrl_ReturnsNavigationCommandReplace()
    {
        var url = new Url(["login"], new Dictionary<string, string>(), Option<string>.None);
        var command = Navigation.ReplaceUrl(url);

        await Assert.That(command).IsTypeOf<NavigationCommand.Replace>();
        var replace = (NavigationCommand.Replace)command;
        await Assert.That(replace.Url).IsEqualTo(url);
    }

    [Test]
    public async Task Back_ReturnsNavigationCommandGoBack()
    {
        var command = Navigation.Back;

        await Assert.That(command).IsTypeOf<NavigationCommand.GoBack>();
    }

    [Test]
    public async Task Forward_ReturnsNavigationCommandGoForward()
    {
        var command = Navigation.Forward;

        await Assert.That(command).IsTypeOf<NavigationCommand.GoForward>();
    }

    [Test]
    public async Task ExternalUrl_ReturnsNavigationCommandExternal()
    {
        var command = Navigation.ExternalUrl("https://github.com");

        await Assert.That(command).IsTypeOf<NavigationCommand.External>();
        var external = (NavigationCommand.External)command;
        await Assert.That(external.Href).IsEqualTo("https://github.com");
    }

    // =========================================================================
    // Navigation Commands — Are Commands
    // =========================================================================

    [Test]
    public async Task NavigationCommands_ImplementCommandInterface()
    {
        await Assert.That(Navigation.PushUrl(Url.Root)).IsAssignableTo<Command>();
        await Assert.That(Navigation.ReplaceUrl(Url.Root)).IsAssignableTo<Command>();
        await Assert.That(Navigation.Back).IsAssignableTo<Command>();
        await Assert.That(Navigation.Forward).IsAssignableTo<Command>();
        await Assert.That(Navigation.ExternalUrl("https://example.com")).IsAssignableTo<Command>();
    }

    // =========================================================================
    // Navigation Commands — Batchable
    // =========================================================================

    [Test]
    public async Task NavigationCommand_CanBeBatchedWithRegularCommands()
    {
        var batch = Commands.Batch(
            Navigation.PushUrl(Url.Root),
            Commands.None);

        await Assert.That(batch).IsTypeOf<Command.Batch>();
        var batchCmd = (Command.Batch)batch;
        await Assert.That(batchCmd.Commands.Count).IsEqualTo(2);
        await Assert.That(batchCmd.Commands[0]).IsTypeOf<NavigationCommand.Push>();
    }

    // =========================================================================
    // NavigationCallbacks — URL Change Handling
    // =========================================================================

    [Test]
    [NotInParallel(nameof(NavigationCallbacks))]
    public async Task HandleUrlChanged_DispatchesParsedUrl()
    {
        Url? received = null;
        NavigationCallbacks.OnUrlChange = url => received = url;

        try
        {
            NavigationCallbacks.HandleUrlChanged("/articles/my-slug?page=1#comments");

            await Assert.That(received).IsNotNull();
            await Assert.That(received!.Path).IsEquivalentTo(new[] { "articles", "my-slug" });
            await Assert.That(received.Query["page"]).IsEqualTo("1");
            await Assert.That(received.Fragment.IsSome).IsTrue();
            await Assert.That(received.Fragment.Value).IsEqualTo("comments");
        }
        finally
        {
            NavigationCallbacks.OnUrlChange = null;
        }
    }

    [Test]
    [NotInParallel(nameof(NavigationCallbacks))]
    public async Task HandleUrlChanged_RootPath_DispatchesEmptyPath()
    {
        Url? received = null;
        NavigationCallbacks.OnUrlChange = url => received = url;

        try
        {
            NavigationCallbacks.HandleUrlChanged("/");

            await Assert.That(received).IsNotNull();
            await Assert.That(received!.Path).IsEmpty();
        }
        finally
        {
            NavigationCallbacks.OnUrlChange = null;
        }
    }

    [Test]
    [NotInParallel(nameof(NavigationCallbacks))]
    public async Task HandleUrlChanged_NoCallback_DoesNotThrow()
    {
        NavigationCallbacks.OnUrlChange = null;

        // Record.Exception equivalent — just call it, it shouldn't throw
        NavigationCallbacks.HandleUrlChanged("/some/path");
    }

    [Test]
    [NotInParallel(nameof(NavigationCallbacks))]
    public async Task HandleUrlChanged_AbsoluteUrl_ParsesCorrectly()
    {
        Url? received = null;
        NavigationCallbacks.OnUrlChange = url => received = url;

        try
        {
            NavigationCallbacks.HandleUrlChanged("https://localhost:8080/articles?tag=elm");

            await Assert.That(received).IsNotNull();
            await Assert.That(received!.Path).IsEquivalentTo(new[] { "articles" });
            await Assert.That(received.Query["tag"]).IsEqualTo("elm");
        }
        finally
        {
            NavigationCallbacks.OnUrlChange = null;
        }
    }

    // =========================================================================
    // UrlChanges Subscription — Creation
    // =========================================================================

    [Test]
    public async Task UrlChanges_ReturnsSubscriptionSource()
    {
        var subscription = Navigation.UrlChanges(url => new UrlChanged(url));

        await Assert.That(subscription).IsTypeOf<Subscription.Source>();
        var source = (Subscription.Source)subscription;
        await Assert.That(source.Key.Value).IsEqualTo("navigation:urlChanges");
    }

    [Test]
    public async Task UrlChanges_CanBeBatchedWithOtherSubscriptions()
    {
        var sub = SubscriptionModule.Batch(
            Navigation.UrlChanges(url => new UrlChanged(url)),
            SubscriptionModule.Every(TimeSpan.FromSeconds(1), () => new UrlChanged(Url.Root)));

        await Assert.That(sub).IsTypeOf<Subscription.Batch>();
        var batch = (Subscription.Batch)sub;
        await Assert.That(batch.Subscriptions.Count).IsEqualTo(2);
    }

    // =========================================================================
    // Navigation.ParseUrl — Helper
    // =========================================================================

    [Test]
    public async Task ParseUrl_RelativePath_ParsesCorrectly()
    {
        var url = Navigation.ParseUrl("/articles/my-slug");

        await Assert.That(url.Path).IsEquivalentTo(new[] { "articles", "my-slug" });
    }

    [Test]
    public async Task ParseUrl_AbsoluteUrl_ParsesCorrectly()
    {
        var url = Navigation.ParseUrl("https://example.com/search?q=test");

        await Assert.That(url.Path).IsEquivalentTo(new[] { "search" });
        await Assert.That(url.Query["q"]).IsEqualTo("test");
    }

    [Test]
    public async Task ParseUrl_RootPath_ReturnsEmptyPath()
    {
        var url = Navigation.ParseUrl("/");

        await Assert.That(url.Path).IsEmpty();
    }

    [Test]
    public async Task ParseUrl_WithFragment_ParsesFragment()
    {
        var url = Navigation.ParseUrl("/page#section");

        await Assert.That(url.Path).IsEquivalentTo(new[] { "page" });
        await Assert.That(url.Fragment.IsSome).IsTrue();
        await Assert.That(url.Fragment.Value).IsEqualTo("section");
    }

    // =========================================================================
    // UrlChanged Message Type
    // =========================================================================

    [Test]
    public async Task UrlChanged_IsMessage()
    {
        var url = new Url(["articles"], new Dictionary<string, string>(), Option<string>.None);
        var msg = new UrlChanged(url);

        await Assert.That(msg).IsAssignableTo<Message>();
        await Assert.That(msg.Url).IsEqualTo(url);
    }

    // =========================================================================
    // UrlRequest Types
    // =========================================================================

    [Test]
    public async Task UrlRequest_Internal_IsMessage()
    {
        var url = new Url(["articles"], new Dictionary<string, string>(), Option<string>.None);
        var request = new UrlRequest.Internal(url);

        await Assert.That(request).IsAssignableTo<Message>();
        await Assert.That(request.Url).IsEqualTo(url);
    }

    [Test]
    public async Task UrlRequest_External_IsMessage()
    {
        var request = new UrlRequest.External("https://github.com");

        await Assert.That(request).IsAssignableTo<Message>();
        await Assert.That(request.Href).IsEqualTo("https://github.com");
    }

    // =========================================================================
    // NavigationCommand — Pattern Matching
    // =========================================================================

    [Test]
    public async Task NavigationCommand_CanBePatternMatched()
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

        await Assert.That(navCount).IsEqualTo(5);
        await Assert.That(otherCount).IsEqualTo(1);
    }

    [Test]
    public async Task NavigationCommand_Push_PatternMatchesWithUrl()
    {
        var url = new Url(["articles", "test"], new Dictionary<string, string>(), Option<string>.None);
        Command command = Navigation.PushUrl(url);

        var matched = command switch
        {
            NavigationCommand.Push push => push.Url.ToRelativeUri(),
            _ => "unknown"
        };

        await Assert.That(matched).IsEqualTo("/articles/test");
    }

    [Test]
    public async Task NavigationCommand_External_PatternMatchesWithHref()
    {
        Command command = Navigation.ExternalUrl("https://github.com");

        var matched = command switch
        {
            NavigationCommand.External ext => ext.Href,
            _ => "unknown"
        };

        await Assert.That(matched).IsEqualTo("https://github.com");
    }
}
