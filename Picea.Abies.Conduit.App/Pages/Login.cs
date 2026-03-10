using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Conduit.App.Pages;

public static class Login
{
    public static Node View(LoginModel model) =>
        div([class_("auth-page")],
        [
            div([class_("container page")],
            [
                div([class_("row")],
                [
                    div([class_("col-md-6 offset-md-3 col-xs-12")],
                    [
                        h1([class_("text-xs-center")], [text("Sign in")]),
                        p([class_("text-xs-center")], [a([href("/register")], [text("Need an account?")])]),
                        ErrorList(model.Errors),
                        Form(model)
                    ])
                ])
            ])
        ]);

    private static Node Form(LoginModel model) =>
        form([onsubmit(new LoginSubmitted())],
        [
            fieldset([class_("form-group")],
            [
                input([class_("form-control form-control-lg"), type("email"), placeholder("Email"),
                       value(model.Email), oninput(e => new LoginEmailChanged(e?.Value ?? ""))])
            ]),
            fieldset([class_("form-group")],
            [
                input([class_("form-control form-control-lg"), type("password"), placeholder("Password"),
                       value(model.Password), oninput(e => new LoginPasswordChanged(e?.Value ?? ""))])
            ]),
            button([class_("btn btn-lg btn-primary pull-xs-right"), type("submit"),
                    ..model.IsSubmitting ? [disabled()] : Array.Empty<DOM.Attribute>()],
                [text(model.IsSubmitting ? "Signing in..." : "Sign in")])
        ]);

    internal static Node ErrorList(IReadOnlyList<string> errors) =>
        errors.Count == 0
            ? new Empty()
            : ul([class_("error-messages")], errors.Select(error => li([], [text(error)])).ToArray());
}
