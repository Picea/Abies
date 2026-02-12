using Abies.Conduit.Main;
using Abies.DOM;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

namespace Abies.Conduit.Page.Settings;

public interface Message : Abies.Message
{
    record ImageUrlChanged(string Value) : Message;
    record UsernameChanged(string Value) : Message;
    record BioChanged(string Value) : Message;
    record EmailChanged(string Value) : Message;
    record PasswordChanged(string Value) : Message;
    record SettingsSubmitted : Message;
    record SettingsSuccess(User User) : Message;
    record SettingsError(Dictionary<string, string[]> Errors) : Message;
    record LogoutRequested : Message;
}

public record Model(
    string ImageUrl = "",
    string Username = "",
    string Bio = "",
    string Email = "",
    string Password = "",
    bool IsSubmitting = false,
    Dictionary<string, string[]>? Errors = null,
    User? CurrentUser = null
);

public class Page : Element<Model, Message>
{
    public static Model Initialize(Message argument)
    {
        return new Model();
    }

    public static Subscription Subscriptions(Model model)
    {
        return SubscriptionModule.None;
    }

    public static (Model model, Command command) Update(Abies.Message message, Model model)
        => message switch
        {
            // When Settings page is first shown or user context changes, prefill from CurrentUser
            // This is handled by SetUser in Main, but ensure initial values are not empty in view
            Message.ImageUrlChanged imageUrlChanged => (
                model with { ImageUrl = imageUrlChanged.Value },
                Commands.None
            ),
            Message.UsernameChanged usernameChanged => (
                model with { Username = usernameChanged.Value },
                Commands.None
            ),
            Message.BioChanged bioChanged => (
                model with { Bio = bioChanged.Value },
                Commands.None
            ),
            Message.EmailChanged emailChanged => (
                model with { Email = emailChanged.Value },
                Commands.None
            ),
            Message.PasswordChanged passwordChanged => (
                model with { Password = passwordChanged.Value },
                Commands.None
            ),
            Message.SettingsSubmitted => (
                model with { IsSubmitting = true, Errors = null },
                new UpdateUserCommand(model.Username, model.Email, model.Bio, model.ImageUrl,
                    string.IsNullOrEmpty(model.Password) ? null : model.Password)
            ),
            Message.SettingsSuccess success => (
                model with { IsSubmitting = false, Errors = null },
                Commands.None
            ),
            Message.SettingsError errors => (
                model with { IsSubmitting = false, Errors = errors.Errors },
                Commands.None
            ),
            Message.LogoutRequested => (model, Commands.None),
            _ => (model, Commands.None)
        };

    private static Node ErrorList(Dictionary<string, string[]>? errors) =>
    errors is null
        ? text("")
        : ul([class_("error-messages")],
            [..errors.SelectMany(e => e.Value.Select(msg =>
                    li([], [text($"{e.Key} {msg}")])
                ))]
        );

    public static Node View(Model model)
    {
        var initial = model;
        if (initial.CurrentUser is User u)
        {
            // Prefill from current user if fields are empty
            if (string.IsNullOrEmpty(initial.Username))
            {
                initial = initial with { Username = u.Username.Value };
            }

            if (string.IsNullOrEmpty(initial.Email))
            {
                initial = initial with { Email = u.Email.Value };
            }

            if (string.IsNullOrEmpty(initial.ImageUrl))
            {
                initial = initial with { ImageUrl = u.Image };
            }

            if (string.IsNullOrEmpty(initial.Bio))
            {
                initial = initial with { Bio = u.Bio };
            }
        }
        model = initial;

        return
            div([class_("settings-page"), data("testid", "settings-page")], [
                div([class_("container page")], [
                    div([class_("row")], [
                        div([class_("col-md-6 offset-md-3 col-xs-12")], [
                            h1([class_("text-xs-center")], [text("Your Settings")]),

                            ErrorList(model.Errors),
                            form([onsubmit(new Message.SettingsSubmitted())], [
                                fieldset([], [
                                    fieldset([class_("form-group")], [
                                        input([
                                            class_("form-control"),
                                            type("text"),
                                            placeholder("URL of profile picture"),
                                            value(model.ImageUrl),
                                            oninput(d => new Message.ImageUrlChanged(d?.Value ?? "")),
                                            onchange(d => new Message.ImageUrlChanged(d?.Value ?? "")),
                                            ..(model.IsSubmitting ? (DOM.Attribute[])[disabled()] : [])
                                        ])
                                    ]),
                                    fieldset([class_("form-group")], [
                                        input([
                                            class_("form-control form-control-lg"),
                                            type("text"),
                                            placeholder("Your Name"),
                                            value(model.Username),
                                            oninput(d => new Message.UsernameChanged(d?.Value ?? "")),
                                            onchange(d => new Message.UsernameChanged(d?.Value ?? "")),
                                            ..(model.IsSubmitting ? (DOM.Attribute[])[disabled()] : [])
                                        ])
                                    ]),
                                    fieldset([class_("form-group")], [
                                        textarea([
                                            class_("form-control form-control-lg"),
                                            rows("8"),
                                            placeholder("Short bio about you"),
                                            oninput(d => new Message.BioChanged(d?.Value ?? "")),
                                            onchange(d => new Message.BioChanged(d?.Value ?? "")),
                                            ..(model.IsSubmitting ? (DOM.Attribute[])[disabled()] : [])
                                        ],
                                            [text(model.Bio)]
                                        )
                                    ]),
                                    fieldset([class_("form-group")], [
                                        input([
                                            class_("form-control form-control-lg"),
                                            type("text"),
                                            placeholder("Email"),
                                            value(model.Email),
                                            oninput(d => new Message.EmailChanged(d?.Value ?? "")),
                                            onchange(d => new Message.EmailChanged(d?.Value ?? "")),
                                            ..(model.IsSubmitting ? (DOM.Attribute[])[disabled()] : [])
                                        ])
                                    ]),
                                    fieldset([class_("form-group")], [
                                        input([
                                            class_("form-control form-control-lg"),
                                            type("password"),
                                            placeholder("Password"),
                                            value(model.Password),
                                            oninput(d => new Message.PasswordChanged(d?.Value ?? "")),
                                            onchange(d => new Message.PasswordChanged(d?.Value ?? "")),
                                            ..(model.IsSubmitting ? (DOM.Attribute[])[disabled()] : [])
                                        ])
                                    ]),
                                    button([
                                            class_("btn btn-lg btn-primary pull-xs-right"),
                                            type("submit"),
                                            ..((model.IsSubmitting ||
                                                     string.IsNullOrWhiteSpace(model.Username) ||
                                                     string.IsNullOrWhiteSpace(model.Email))
                                                ? (DOM.Attribute[])[disabled()]
                                                : []),
                                            onclick(new Message.SettingsSubmitted())
                                        ],
                                        [text(model.IsSubmitting ? "Updating Settings..." : "Update Settings")]
                                    )
                                ])
                            ]),
                            hr([class_("my-4")]),
                            button([class_("btn btn-outline-danger"),
                                onclick(new Message.LogoutRequested())],
                                [text("Or click here to logout.")]
                            )
                        ])
                    ])
                ])
            ]);
    }
}
