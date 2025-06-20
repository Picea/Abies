using Abies.Conduit.Main;
using Abies.Conduit.Routing;
using Abies.DOM;
using System.Collections.Generic;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

namespace Abies.Conduit.Page.Settings;

public interface Message : Abies.Message
{
    public record ImageUrlChanged(string Value) : Message;
    public record UsernameChanged(string Value) : Message;
    public record BioChanged(string Value) : Message;
    public record EmailChanged(string Value) : Message;
    public record PasswordChanged(string Value) : Message;
    public record SettingsSubmitted : Message;
    public record SettingsSuccess(User User) : Message;
    public record SettingsError(Dictionary<string, string[]> Errors) : Message;
    public record LogoutRequested : Message;
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
        return new Subscription();
    }

    public static (Model model, Command command) Update(Abies.Message message, Model model)
        => message switch
        {
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
            ),            Message.SettingsSubmitted => (
                model with { IsSubmitting = true, Errors = null },
                new Abies.Conduit.UpdateUserCommand(model.Username, model.Email, model.Bio, model.ImageUrl, 
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
        errors == null
            ? text("")
            : ul([class_("error-messages")], 
                [..errors.SelectMany(e => e.Value.Select(msg => 
                    li([], [text($"{e.Key} {msg}")])
                ))]
            );

    public static Node View(Model model) =>
        div([class_("settings-page")], [
            div([class_("container page")], [
                div([class_("row")], [
                    div([class_("col-md-6 offset-md-3 col-xs-12")], [
                        h1([class_("text-xs-center")], [text("Your Settings")]),
                        
                        ErrorList(model.Errors),
                          form([], [
                            fieldset([], [
                                fieldset([class_("form-group")], [
                                    input([
                                        class_("form-control"),
                                        type("text"),
                                        placeholder("URL of profile picture"),
                                        value(model.ImageUrl),
                                        oninput(v => new Message.ImageUrlChanged(v)),
                                        ..(model.IsSubmitting ? [disabled()] : [])
                                    ])
                                ]),
                                fieldset([class_("form-group")], [
                                    input([
                                        class_("form-control form-control-lg"),
                                        type("text"),
                                        placeholder("Your Name"),
                                        value(model.Username),
                                        oninput(v => new Message.UsernameChanged(v)),
                                        ..(model.IsSubmitting ? [disabled()] : [])
                                    ])
                                ]),                                fieldset([class_("form-group")], [
                                    textarea([
                                        class_("form-control form-control-lg"),
                                        rows("8"),
                                        placeholder("Short bio about you"),
                                        value(model.Bio),
                                        oninput(v => new Message.BioChanged(v)),
                                        ..(model.IsSubmitting ? [disabled()] : [])
                                    ],
                                        []
                                    )
                                ]),
                                fieldset([class_("form-group")], [
                                    input([
                                        class_("form-control form-control-lg"),
                                        type("text"),
                                        placeholder("Email"),
                                        value(model.Email),
                                        oninput(v => new Message.EmailChanged(v)),
                                        ..(model.IsSubmitting ? [disabled()] : [])
                                    ])
                                ]),
                                fieldset([class_("form-group")], [
                                    input([
                                        class_("form-control form-control-lg"),
                                        type("password"),
                                        placeholder("Password"),
                                        value(model.Password),
                                        oninput(v => new Message.PasswordChanged(v)),
                                        ..(model.IsSubmitting ? [disabled()] : [])
                                    ])
                                ]),                                button([class_("btn btn-lg btn-primary pull-xs-right"),
                                    type("button"),
                                    ..((model.IsSubmitting ||
                                             string.IsNullOrWhiteSpace(model.Username) ||
                                             string.IsNullOrWhiteSpace(model.Email))
                                        ? [disabled()]
                                        : []),
                                    onclick(new Message.SettingsSubmitted())],
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