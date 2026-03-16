using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Conduit.App.Pages;

public static class Profile
{
    public static Node View(ProfileModel model, Session? session)
    {
        if (model.IsLoading || model.Profile is null)
            return div([class_("profile-page")],
                [div([class_("container")],
                    [div([class_("row")],
                        [div([class_("col-xs-12 col-md-10 offset-md-1")], [text("Loading profile...")])])])]);

        var profile = model.Profile;
        return div([class_("profile-page")],
        [
            UserInfo(profile, session),
            div([class_("container")],
            [
                div([class_("row")],
                [
                    div([class_("col-xs-12 col-md-10 offset-md-1")],
                    [
                        ArticleTabs(model.Username, model.ShowFavorites),
                        Views.ArticlePreview.List(model.Articles, false),
                        Views.ArticlePreview.Pagination(model.ArticlesCount, model.CurrentPage, Constants.ArticlesPerPage, page => PageHref(model.Username, model.ShowFavorites, page))
                    ])
                ])
            ])
        ]);
    }

    private static Node UserInfo(ProfileData profile, Session? session) =>
        div([class_("user-info")],
        [
            div([class_("container")],
            [
                div([class_("row")],
                [
                    div([class_("col-xs-12 col-md-10 offset-md-1")],
                    [
                        img([src(profile.Image ?? "https://api.realworld.io/images/smiley-cyrus.jpeg"), class_("user-img")]),
                        h4([], [text(profile.Username)]),
                        p([], [text(profile.Bio)]),
                        ..ActionButton(profile, session)
                    ])
                ])
            ])
        ]);

    private static Node[] ActionButton(ProfileData profile, Session? session)
    {
        if (session is not null && session.Username == profile.Username)
            return [a([class_("btn btn-sm btn-outline-secondary action-btn"), href("/settings")],
                [i([class_("ion-gear-a")], []), text("\u00A0 Edit Profile Settings")])];
        if (session is null) return [];
        var btnClass = profile.Following ? "btn btn-sm btn-secondary action-btn" : "btn btn-sm btn-outline-secondary action-btn";
        var label = profile.Following ? $"\u00A0 Unfollow {profile.Username}" : $"\u00A0 Follow {profile.Username}";
        return [button([class_(btnClass), onclick(new ToggleFollow(profile.Username, profile.Following))],
            [i([class_("ion-plus-round")], []), text(label)])];
    }

    private static Node ArticleTabs(string username, bool showFavorites) =>
        div([class_("articles-toggle")],
        [
            ul([class_("nav nav-pills outline-active")],
            [
                li([class_("nav-item")],
                    [a([class_(showFavorites ? "nav-link" : "nav-link active"), href(PageHref(username, false, 1))],
                        [text("My Articles")])]),
                li([class_("nav-item")],
                    [a([class_(showFavorites ? "nav-link active" : "nav-link"), href(PageHref(username, true, 1))],
                        [text("Favorited Articles")])])
            ])
        ]);

    private static string PageHref(string username, bool showFavorites, int page)
    {
        var path = showFavorites ? $"/profile/{Uri.EscapeDataString(username)}/favorites" : $"/profile/{Uri.EscapeDataString(username)}";
        return page > 1 ? $"{path}?page={page}" : path;
    }
}
