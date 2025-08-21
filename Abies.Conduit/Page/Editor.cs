using Abies.Conduit.Main;
using Abies.Conduit.Routing;
using Abies.DOM;
using System.Collections.Generic;
using Abies.Conduit;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

namespace Abies.Conduit.Page.Editor;

public interface Message : Abies.Message
{
    public record TitleChanged(string Value) : Message;
    public record DescriptionChanged(string Value) : Message;
    public record BodyChanged(string Value) : Message;
    public record TagInputChanged(string Value) : Message;
    public record AddTag : Message;
    public record NoOp : Message;
    public record RemoveTag(string Tag) : Message;
    public record ArticleSubmitted : Message;
    public record ArticleSubmitSuccess(string Slug) : Message;
    public record ArticleSubmitError(Dictionary<string, string[]> Errors) : Message;
    public record ArticleLoaded(Home.Article Article) : Message;
}

public record Model(
    string Title = "",
    string Description = "",
    string Body = "",
    string TagInput = "",
    List<string>? TagList = null,
    bool IsSubmitting = false,
    bool IsLoading = false,
    string? Slug = null,
    Dictionary<string, string[]>? Errors = null,
    User? CurrentUser = null
)
{
    public Model() : this("", "", "", "", []) { }
}

public class Page : Element<Model, Message>
{
    public static Model Initialize(Message argument)
    {
        return new Model();
    }

    public static Subscription Subscriptions(Model model)
    {
        return new Subscription();
    }

    public static (Model model, Command command) Update(Abies.Message message, Model model)
        => message switch
        {
            Message.TitleChanged titleChanged => (
                model with { Title = titleChanged.Value },
                Commands.None
            ),
            Message.DescriptionChanged descriptionChanged => (
                model with { Description = descriptionChanged.Value },
                Commands.None
            ),
            Message.BodyChanged bodyChanged => (
                model with { Body = bodyChanged.Value },
                Commands.None
            ),
            Message.TagInputChanged tagInputChanged => (
                model with { TagInput = tagInputChanged.Value },
                Commands.None
            ),
            Message.AddTag => (
        !string.IsNullOrWhiteSpace(model.TagInput)
                    ? model with 
                    { 
            TagList = (model.TagList is null
                                ? new List<string> { model.TagInput }
                                : (model.TagList.Contains(model.TagInput)
                                    ? model.TagList
                                    : new List<string>(model.TagList) { model.TagInput })),
                        TagInput = model.TagList is null || !model.TagList.Contains(model.TagInput) ? "" : model.TagInput
                    }
                    : model,
                Commands.None
            ),
            Message.RemoveTag removeTag => (
        model with { TagList = model.TagList?.FindAll(t => t != removeTag.Tag) },
                Commands.None
            ),
            Message.NoOp => (model, Commands.None),
            Message.ArticleSubmitted => (
                model with { IsSubmitting = true, Errors = null },
                model.Slug == null
                    ?  new CreateArticleCommand(model.Title, model.Description, model.Body, model.TagList ?? new List<string>()) 
                    :  new UpdateArticleCommand(model.Slug, model.Title, model.Description, model.Body) 
            ),
            Message.ArticleSubmitSuccess slug => (
                model with { IsSubmitting = false, Errors = null, Slug = slug.Slug },
                Commands.None
            ),
            Message.ArticleSubmitError errors => (
                model with { IsSubmitting = false, Errors = errors.Errors },
                Commands.None
            ),
            Message.ArticleLoaded article => (
                model with 
                { 
                    Title = article.Article.Title,
                    Description = article.Article.Description,
                    Body = article.Article.Body,
                    TagList = article.Article.TagList,
                    Slug = article.Article.Slug,
                    IsLoading = false
                },
                Commands.None
            ),
            _ => (model, Commands.None)
        };

    private static Node ErrorList(Dictionary<string, string[]>? errors) =>
        errors == null
            ? text("")
            : ul([class_("error-messages")],
                [.. errors.SelectMany(e => e.Value.Select(msg => 
                    li([], [text($"{e.Key} {msg}")])
                ))]
            );

    private static Node TagInputSection(Model model) =>
        div([class_("tag-list")], [
            ..model.TagList?.Select(static tag =>
                div([class_("tag-pill tag-default")], [
                    i([class_("ion-close-round"),
                      onclick(new Message.RemoveTag(tag))],
                      []),
                    text($" {tag}")
                ])) ?? []
        ]);

    public static Node View(Model model) =>
    div([class_("editor-page"), Abies.Html.Attributes.data("testid", "editor-page")], [
            div([class_("container page")], [
                div([class_("row")], [
                    div([class_("col-md-10 offset-md-1 col-xs-12")], [
                        ErrorList(model.Errors),
                        form([], [
                            fieldset([], [
                                fieldset([class_("form-group")], [
                                    input([
                                        class_("form-control form-control-lg"),
                                        type("text"),
                                        placeholder("Article Title"),
                                        value(model.Title),
                                        oninput(d => new Message.TitleChanged(d?.Value ?? "")),
                                        onchange(d => new Message.TitleChanged(d?.Value ?? "")),
                                        ..(model.IsSubmitting ? new[] { disabled() } : System.Array.Empty<DOM.Attribute>())
                                    ])
                                ]),
                                fieldset([class_("form-group")], [
                                    input([
                                        class_("form-control"),
                                        type("text"),
                                        placeholder("What's this article about?"),
                                        value(model.Description),
                                        oninput(d => new Message.DescriptionChanged(d?.Value ?? "")),
                                        onchange(d => new Message.DescriptionChanged(d?.Value ?? "")),
                                        ..(model.IsSubmitting ? new[] { disabled() } : System.Array.Empty<DOM.Attribute>())
                                    ])
                                ]),
                                fieldset([class_("form-group")], [
                                    textarea([
                                        class_("form-control"),
                                        rows("8"),
                                        placeholder("Write your article (in markdown)"),
                                        value(model.Body),
                                        oninput(d => new Message.BodyChanged(d?.Value ?? "")),
                                        onchange(d => new Message.BodyChanged(d?.Value ?? "")),
                                        ..(model.IsSubmitting ? new[] { disabled() } : System.Array.Empty<DOM.Attribute>())
                                    ], [])
                                ]),
                                fieldset([class_("form-group")], [
                                    input([
                                        class_("form-control"),
                                        type("text"),
                                        placeholder("Enter tags"),
                                        value(model.TagInput),
                                        oninput(d => new Message.TagInputChanged(d?.Value ?? "")),
                                        onchange(d => new Message.TagInputChanged(d?.Value ?? "")),
                                          onkeydown(e => e != null && e.Key == "Enter" ? new Message.AddTag() : new Message.NoOp()),
                                          ..(model.IsSubmitting ? new[] { disabled() } : System.Array.Empty<DOM.Attribute>())
                                    ]),
                                    div([class_("tag-list")], [
                                        TagInputSection(model)
                                    ])
                                ]),
                                button([
                                    class_("btn btn-lg pull-xs-right btn-primary"),
                                    type("button"),
                                    ..((model.IsSubmitting ||
                                             string.IsNullOrWhiteSpace(model.Title) ||
                                             string.IsNullOrWhiteSpace(model.Description) ||
                                             string.IsNullOrWhiteSpace(model.Body))
                                          ? new[] { disabled() }
                                          : System.Array.Empty<DOM.Attribute>()),
                                    onclick(new Message.ArticleSubmitted())],
                                    [text(model.IsSubmitting 
                                        ? "Publishing Article..." 
                                        : model.Slug != null 
                                            ? "Update Article" 
                                            : "Publish Article")]
                                )
                            ])
                        ])
                    ])
                ])
            ])
        ]);
}
