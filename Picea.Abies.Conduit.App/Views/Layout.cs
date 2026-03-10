using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;

namespace Picea.Abies.Conduit.App.Views;

public static class Layout
{
    public static Node Page(Page currentPage, Session? session, Node content) =>
        div([], [Navbar(currentPage, session), content, Footer()]);

    private static Node Navbar(Page currentPage, Session? session) =>
        nav([class_("navbar navbar-light")],
        [
            div([class_("container")],
            [
                a([class_("navbar-brand"), href("/")], [text("conduit")]),
                ul([class_("nav navbar-nav pull-xs-right")], NavLinks(currentPage, session))
            ])
        ]);

    private static Node[] NavLinks(Page currentPage, Session? session)
    {
        if (session is not null)
            return
            [
                li([class_("nav-item")], [a([class_(NavClass(currentPage is Page.Home)), href("/")], [text("Home")])]),
                li([class_("nav-item")], [a([class_(NavClass(currentPage is Page.Editor)), href("/editor")],
                    [i([class_("ion-compose")], []), text("\u2003New Article")])]),
                li([class_("nav-item")], [a([class_(NavClass(currentPage is Page.Settings)), href("/settings")],
                    [i([class_("ion-gear-a")], []), text("\u2003Settings")])]),
                li([class_("nav-item")], [a([class_(NavClass(currentPage is Page.Profile p && p.Data.Username == session.Username)),
                    href($"/profile/{session.Username}")], [text(session.Username)])])
            ];
        return
        [
            li([class_("nav-item")], [a([class_(NavClass(currentPage is Page.Home)), href("/")], [text("Home")])]),
            li([class_("nav-item")], [a([class_(NavClass(currentPage is Page.Login)), href("/login")], [text("Sign in")])]),
            li([class_("nav-item")], [a([class_(NavClass(currentPage is Page.Register)), href("/register")], [text("Sign up")])])
        ];
    }

    private static string NavClass(bool isActive) => isActive ? "nav-link active" : "nav-link";

    private static Node Footer() =>
        footer([],
        [
            div([class_("container")],
            [
                a([href("/"), class_("logo-font")], [text("conduit")]),
                span([class_("attribution")],
                [
                    text("An interactive learning project from "),
                    a([href("https://thinkster.io")], [text("Thinkster")]),
                    text(". Code & design licensed under MIT.")
                ])
            ])
        ]);
}
