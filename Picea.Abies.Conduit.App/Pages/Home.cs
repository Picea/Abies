using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;

namespace Picea.Abies.Conduit.App.Pages;

public static class Home
{
    public static Node View(HomeModel model, Session? session) =>
        div([class_("home-page")],
        [
            Banner(),
            div([class_("container page")],
            [
                div([class_("row")],
                [
                    div([class_("col-md-9")],
                    [
                        FeedTabs(model.ActiveTab, session, model.SelectedTag),
                        Views.ArticlePreview.List(model.Articles, model.IsLoading),
                        Views.ArticlePreview.Pagination(model.ArticlesCount, model.CurrentPage, Constants.ArticlesPerPage, page => PageHref(model.ActiveTab, model.SelectedTag, page))
                    ]),
                    div([class_("col-md-3")], [Sidebar(model.PopularTags)])
                ])
            ])
        ]);

    private static Node Banner() =>
        div([class_("banner")],
        [
            div([class_("container")],
            [
                h1([class_("logo-font")], [text("conduit")]),
                p([], [text("A place to share your knowledge.")])
            ])
        ]);

    private static Node FeedTabs(FeedTab activeTab, Session? session, string? selectedTag)
    {
        var tabs = new List<Node>();
        if (session is not null)
            tabs.Add(Tab("Your Feed", FeedTab.Your, activeTab, PageHref(FeedTab.Your, null, 1)));
        tabs.Add(Tab("Global Feed", FeedTab.Global, activeTab, PageHref(FeedTab.Global, null, 1)));
        if (activeTab is FeedTab.Tag && selectedTag is not null)
            tabs.Add(Tab($"# {selectedTag}", FeedTab.Tag, activeTab, PageHref(FeedTab.Tag, selectedTag, 1)));
        return div([class_("feed-toggle")], [ul([class_("nav nav-pills outline-active")], tabs.ToArray())]);
    }

    private static Node Tab(string label, FeedTab tab, FeedTab activeTab, string hrefValue)
    {
        var activeClass = tab == activeTab ? "nav-link active" : "nav-link";
        return li([class_("nav-item")], [a([class_(activeClass), href(hrefValue)], [text(label)])]);
    }

    private static Node Sidebar(IReadOnlyList<string> tags) =>
        div([class_("sidebar")],
        [
            p([], [text("Popular Tags")]),
            tags.Count == 0
                ? text("Loading tags...")
                : div([class_("tag-list")],
                    tags.Select(tag =>
                        a([href(PageHref(FeedTab.Tag, tag, 1)), class_("tag-pill tag-default")],
                            [text(tag)])).ToArray())
        ]);

    private static string PageHref(FeedTab tab, string? tag, int page)
    {
        var query = page > 1 ? $"?page={page}" : string.Empty;
        return tab switch
        {
            FeedTab.Your => page > 1 ? $"/?feed=following&page={page}" : "/?feed=following",
            FeedTab.Tag when tag is not null => $"/tag/{Uri.EscapeDataString(tag)}{query}",
            _ => page > 1 ? $"/?page={page}" : "/"
        };
    }
}
