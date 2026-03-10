using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Conduit.App.Pages;

public static class Register
{
    public static Node View(RegisterModel model) =>
        div([class_("auth-page")],
        [
            div([class_("container page")],
            [
                div([class_("row")],
                [
                    div([class_("col-md-6 offset-md-3 col-xs-12")],
                    [
                        h1([class_("text-xs-center")], [text("Sign up")]),
                        p([class_("text-xs-center")], [a([href("/login")], [text("Have an account?")])]),
                        Login.ErrorList(model.Errors),
                        Form(model)
                    ])
                ])
            ])
        ]);

    private static Node Form(RegisterModel model) =>
        form([onsubmit(new RegisterSubmitted())],
        [
            fieldset([class_("form-group")],
            [
                input([class_("form-control form-control-lg"), type("text"), placeholder("Your Name"),
                       value(model.Username), oninput(e => new RegisterUsernameChanged(e?.Value ?? ""))])
            ]),
            fieldset([class_("form-group")],
            [
                input([class_("form-control form-control-lg"), type("email"), placeholder("Email"),
                       value(model.Email), oninput(e => new RegisterEmailChanged(e?.Value ?? ""))])
            ]),
            fieldset([class_("form-group")],
            [
                input([class_("form-control form-control-lg"), type("password"), placeholder("Password"),
                       value(model.Password), oninput(e => new RegisterPasswordChanged(e?.Value ?? ""))])
            ]),
            button([class_("btn btn-lg btn-primary pull-xs-right"), type("submit"),
                    ..model.IsSubmitting ? [disabled()] : Array.Empty<DOM.Attribute>()],
                [text(model.IsSubmitting ? "Signing up..." : "Sign up")])
        ]);
}
