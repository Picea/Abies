using Abies.Conduit.Main;
using Abies.Conduit.Routing;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;

namespace Abies.Conduit;

public static class Navigation
{
    /// <summary>
    /// Creates a navigation link with a stable, content-based identity.
    /// Per ADR-016: Uses the element's Id for keyed DOM diffing, so DOM updates
    /// correctly identify and update individual nav items.
    /// </summary>
    private static Node NavLink(string url, string label, bool active)
    {
        // Generate a stable ID based on the URL to make each li element unique.
        // This ensures DOM diffing can correctly match old and new nav items,
        // and the browser's getElementById returns the correct element.
        var stableId = $"nav-{url.Replace("/", "-").TrimStart('-').TrimEnd('-')}";
        if (string.IsNullOrEmpty(stableId) || stableId == "nav-")
            stableId = "nav-home";
        
        // ADR-016: The id: parameter serves as both the DOM id and the diffing key.
        // No separate key() attribute needed.
        return li([class_("nav-item")], [
            a([class_(active ? "nav-link active" : "nav-link"), href(url)], [text(label)])
        ], id: stableId);
    }

    public static Node View(Model model)
    {
        Node[] userLinks;
        
        // While initializing (checking localStorage for user), don't show auth links
        // This prevents a brief flash of "Sign in" before user state loads
        if (model.IsInitializing)
        {
            userLinks = [];
        }
        else if (model.CurrentUser is null)
        {
            userLinks = new Node[]
            {
                NavLink("/login", "Sign in", model.CurrentRoute is Routing.Route.Login),
                NavLink("/register", "Sign up", model.CurrentRoute is Routing.Route.Register)
            };
        }
        else
        {
            var links = new System.Collections.Generic.List<Node>
            {
                NavLink("/editor", "New Article", model.CurrentRoute is Abies.Conduit.Routing.Route.NewArticle),
                NavLink("/settings", "Settings", model.CurrentRoute is Routing.Route.Settings)
            };
            if (!string.IsNullOrWhiteSpace(model.CurrentUser.Username.Value))
            {
                links.Add(NavLink($"/profile/{model.CurrentUser.Username.Value}",
                    model.CurrentUser.Username.Value,
                    model.CurrentRoute is Routing.Route.Profile p && p.UserName.Value == model.CurrentUser.Username.Value ||
                    model.CurrentRoute is Routing.Route.ProfileFavorites pf && pf.UserName.Value == model.CurrentUser.Username.Value));
            }
            userLinks = links.ToArray();
        }

        return nav([class_("navbar navbar-light")], [
            div([class_("container")], [
                a([class_("navbar-brand"), href("/")], [text("conduit")]),
                ul([class_("nav navbar-nav pull-xs-right")], [
                    NavLink("/", "Home", model.CurrentRoute is Routing.Route.Home),
                    ..userLinks
                ])
            ])
        ]);
    }
}
