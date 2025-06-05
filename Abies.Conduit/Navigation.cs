using Abies.Conduit.Main;
using Abies.Conduit.Routing;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;

namespace Abies.Conduit;

public static class Navigation
{
    private static Node NavLink(string url, string label, bool active) =>
        li([class_("nav-item")], [
            a([class_(active ? "nav-link active" : "nav-link"), href(url)], [text(label)])
        ]);

    public static Node View(Model model)
    {
        var userLinks = model.CurrentUser == null
            ? new Node[]
            {
                NavLink("/login", "Sign in", model.CurrentRoute is Route.Login),
                NavLink("/register", "Sign up", model.CurrentRoute is Route.Register)
            }
            : new Node[]
            {
                NavLink("/editor", "New Article", model.CurrentRoute is Route.NewArticle),
                NavLink("/settings", "Settings", model.CurrentRoute is Route.Settings),
                NavLink($"/profile/{model.CurrentUser!.Username.Value}",
                        model.CurrentUser.Username.Value,
                        model.CurrentRoute is Route.Profile p && p.UserName.Value == model.CurrentUser.Username.Value ||
                        model.CurrentRoute is Route.ProfileFavorites pf && pf.UserName.Value == model.CurrentUser.Username.Value)
            };

        return nav([class_("navbar navbar-light")], [
            div([class_("container")], [
                a([class_("navbar-brand"), href("/")], [text("conduit")]),
                ul([class_("nav navbar-nav pull-xs-right")], [
                    NavLink("/", "Home", model.CurrentRoute is Route.Home),
                    ..userLinks
                ])
            ])
        ]);
    }
}
