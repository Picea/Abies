using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Conduit.App.Pages;

public static class Editor
{
    public static Node View(EditorModel model) =>
        div([class_("editor-page")],
        [
            div([class_("container page")],
            [
                div([class_("row")],
                [
                    div([class_("col-md-10 offset-md-1 col-xs-12")],
                    [
                        Login.ErrorList(model.Errors),
                        Form(model)
                    ])
                ])
            ])
        ]);

    private static Node Form(EditorModel model) =>
        form([onsubmit(new EditorSubmitted())],
        [
            fieldset([],
            [
                fieldset([class_("form-group")],
                    [input([class_("form-control form-control-lg"), type("text"), placeholder("Article Title"),
                            value(model.Title), oninput(e => new EditorTitleChanged(e?.Value ?? ""))])]),
                fieldset([class_("form-group")],
                    [input([class_("form-control"), type("text"), placeholder("What's this article about?"),
                            value(model.Description), oninput(e => new EditorDescriptionChanged(e?.Value ?? ""))])]),
                fieldset([class_("form-group")],
                    [textarea([class_("form-control"), rows("8"), placeholder("Write your article (in markdown)"),
                               value(model.Body), oninput(e => new EditorBodyChanged(e?.Value ?? ""))], [])]),
                fieldset([class_("form-group")],
                [
                    input([class_("form-control"), type("text"), placeholder("Enter tags"),
                           value(model.TagInput), oninput(e => new EditorTagInputChanged(e?.Value ?? "")),
                           onkeydown(e => new EditorTagKeyDown(e?.Key ?? ""))]),
                    TagList(model.TagList)
                ]),
                button([class_("btn btn-lg pull-xs-right btn-primary"), type("submit"),
                        ..model.IsSubmitting ? [disabled()] : Array.Empty<DOM.Attribute>()],
                    [text(model.IsSubmitting ? "Publishing..." : "Publish Article")])
            ])
        ]);

    private static Node TagList(IReadOnlyList<string> tags) =>
        tags.Count == 0 ? new Empty() :
        div([class_("tag-list")],
            tags.Select(tag =>
                span([class_("tag-default tag-pill")],
                    [i([class_("ion-close-round"), onclick(new EditorRemoveTag(tag))], []),
                     text($" {tag}")])).ToArray());
}
