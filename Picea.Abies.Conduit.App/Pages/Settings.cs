using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Conduit.App.Pages;

public static class Settings
{
    public static Node View(SettingsModel model) =>
        div([class_("settings-page")],
        [
            div([class_("container page")],
            [
                div([class_("row")],
                [
                    div([class_("col-md-6 offset-md-3 col-xs-12")],
                    [
                        h1([class_("text-xs-center")], [text("Your Settings")]),
                        Login.ErrorList(model.Errors),
                        Form(model),
                        hr([]),
                        button([class_("btn btn-outline-danger"), onclick(new Logout())],
                            [text("Or click here to logout.")])
                    ])
                ])
            ])
        ]);

    private static Node Form(SettingsModel model) =>
        form([onsubmit(new SettingsSubmitted())],
        [
            fieldset([],
            [
                fieldset([class_("form-group")],
                    [input([class_("form-control"), type("text"), placeholder("URL of profile picture"),
                            value(model.Image), oninput(e => new SettingsImageChanged(e?.Value ?? ""))])]),
                fieldset([class_("form-group")],
                    [input([class_("form-control form-control-lg"), type("text"), placeholder("Your Name"),
                            value(model.Username), oninput(e => new SettingsUsernameChanged(e?.Value ?? ""))])]),
                fieldset([class_("form-group")],
                    [textarea([class_("form-control form-control-lg"), rows("8"), placeholder("Short bio about you"),
                               value(model.Bio), oninput(e => new SettingsBioChanged(e?.Value ?? ""))], [])]),
                fieldset([class_("form-group")],
                    [input([class_("form-control form-control-lg"), type("text"), placeholder("Email"),
                            value(model.Email), oninput(e => new SettingsEmailChanged(e?.Value ?? ""))])]),
                fieldset([class_("form-group")],
                    [input([class_("form-control form-control-lg"), type("password"), placeholder("New Password"),
                            value(model.Password), oninput(e => new SettingsPasswordChanged(e?.Value ?? ""))])]),
                button([class_("btn btn-lg btn-primary pull-xs-right"), type("submit"),
                        ..model.IsSubmitting ? [disabled()] : Array.Empty<DOM.Attribute>()],
                    [text(model.IsSubmitting ? "Updating..." : "Update Settings")])
            ])
        ]);
}
