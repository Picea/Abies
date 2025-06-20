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
                NavLink("/login", "Sign in", model.CurrentRoute is Routing.Route.Login),
                NavLink("/register", "Sign up", model.CurrentRoute is Routing.Route.Register)
            }
            : new Node[]
            {
                NavLink("/editor", "New Article", model.CurrentRoute is Abies.Conduit.Routing.Route.NewArticle),
                NavLink("/settings", "Settings", model.CurrentRoute is Routing.Route.Settings),
                NavLink($"/profile/{model.CurrentUser!.Username.Value}",
                        model.CurrentUser.Username.Value,
                        model.CurrentRoute is Routing.Route.Profile p && p.UserName.Value == model.CurrentUser.Username.Value ||
                        model.CurrentRoute is Routing.Route.ProfileFavorites pf && pf.UserName.Value == model.CurrentUser.Username.Value)
            };

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
