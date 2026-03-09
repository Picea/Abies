using Picea.Abies.Subscriptions;

namespace Picea.Abies;

public static class Navigation
{
    public static Command PushUrl(Url url) => new NavigationCommand.Push(url);

    public static Command ReplaceUrl(Url url) => new NavigationCommand.Replace(url);

    public static readonly Command Back = new NavigationCommand.GoBack();

    public static readonly Command Forward = new NavigationCommand.GoForward();

    public static Command ExternalUrl(string href) => new NavigationCommand.External(href);

    public static Subscription UrlChanges(Func<Url, Message> toMessage) =>
        SubscriptionModule.Create("navigation:urlChanges", (dispatch, cancellationToken) =>
        {
            NavigationCallbacks.OnUrlChange = url => dispatch(toMessage(url));

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() =>
            {
                NavigationCallbacks.OnUrlChange = null;
                tcs.TrySetResult();
            });
            return tcs.Task;
        });

    public static Url ParseUrl(string locationHref)
    {
        var uri = new Uri(locationHref, UriKind.RelativeOrAbsolute);
        if (!uri.IsAbsoluteUri)
            uri = new Uri(new Uri("https://localhost"), locationHref);
        return Url.FromUri(uri);
    }
}

public interface NavigationCommand : Command
{
    sealed record Push(Url Url) : NavigationCommand;
    sealed record Replace(Url Url) : NavigationCommand;
    sealed record GoBack : NavigationCommand;
    sealed record GoForward : NavigationCommand;
    sealed record External(string Href) : NavigationCommand;
}

internal static class NavigationCallbacks
{
    internal static Action<Url>? OnUrlChange { get; set; }

    internal static void HandleUrlChanged(string urlString)
    {
        if (OnUrlChange is null)
            return;

        var uri = new Uri(urlString, UriKind.RelativeOrAbsolute);

        if (!uri.IsAbsoluteUri)
        {
            uri = new Uri(new Uri("https://localhost"), urlString);
        }

        var url = Url.FromUri(uri);
        OnUrlChange(url);
    }
}
