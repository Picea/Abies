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
    Dictionary<string, string[]>? Errors = null
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

    public static (Model model, IEnumerable<Command> commands) Update(Abies.Message message, Model model)
        => message switch
        {
            Message.ImageUrlChanged imageUrlChanged => (
                model with { ImageUrl = imageUrlChanged.Value },
                []
            ),
            Message.UsernameChanged usernameChanged => (
                model with { Username = usernameChanged.Value },
                []
            ),
            Message.BioChanged bioChanged => (
                model with { Bio = bioChanged.Value },
                []
            ),
            Message.EmailChanged emailChanged => (
                model with { Email = emailChanged.Value },
                []
            ),
            Message.PasswordChanged passwordChanged => (
                model with { Password = passwordChanged.Value },
                []
            ),            Message.SettingsSubmitted => (
                model with { IsSubmitting = true, Errors = null },
                [new Abies.Conduit.UpdateUserCommand(model.Username, model.Email, model.Bio, model.ImageUrl, 
                    string.IsNullOrEmpty(model.Password) ? null : model.Password)]
            ),
            Message.SettingsSuccess success => (
                model with { IsSubmitting = false, Errors = null },
                []
            ),
            Message.SettingsError errors => (
                model with { IsSubmitting = false, Errors = errors.Errors },
                []
            ),
            Message.LogoutRequested => (model, []),
            _ => (model, [])
        };    private static Node ErrorList(Dictionary<string, string[]>? errors) =>
        errors == null
            ? text("")
            : ul([@class("error-messages")], 
                [..errors.SelectMany(e => e.Value.Select(msg => 
                    li([], [text($"{e.Key} {msg}")])
                ))]
            );

    public static Node View(Model model) =>
        div([@class("settings-page")], [
            div([@class("container page")], [
                div([@class("row")], [
                    div([@class("col-md-6 offset-md-3 col-xs-12")], [
                        h1([@class("text-xs-center")], [text("Your Settings")]),
                        
                        ErrorList(model.Errors),
                          form([], [
                            fieldset([], [
                                fieldset([@class("form-group")], [
                                    input([@class("form-control"),
                                        type("text"),
                                        placeholder("URL of profile picture"),
                                        value(model.ImageUrl),
                                        oninput(new Message.ImageUrlChanged(model.ImageUrl)),
                                        disabled(model.IsSubmitting.ToString())]
                                    )
                                ]),
                                fieldset([@class("form-group")], [
                                    input([@class("form-control form-control-lg"),
                                        type("text"),
                                        placeholder("Your Name"),
                                        value(model.Username),
                                        oninput(new Message.UsernameChanged(model.Username)),
                                        disabled(model.IsSubmitting.ToString())]
                                    )
                                ]),                                fieldset([@class("form-group")], [
                                    textarea([@class("form-control form-control-lg"),
                                        rows("8"),
                                        placeholder("Short bio about you"),
                                        value(model.Bio),
                                        oninput(new Message.BioChanged(model.Bio)),
                                        disabled(model.IsSubmitting.ToString())],
                                        []
                                    )
                                ]),
                                fieldset([@class("form-group")], [
                                    input([@class("form-control form-control-lg"),
                                        type("text"),
                                        placeholder("Email"),
                                        value(model.Email),
                                        oninput(new Message.EmailChanged(model.Email)),
                                        disabled(model.IsSubmitting.ToString())]
                                    )
                                ]),
                                fieldset([@class("form-group")], [
                                    input([@class("form-control form-control-lg"),
                                        type("password"),
                                        placeholder("Password"),
                                        value(model.Password),
                                        oninput(new Message.PasswordChanged(model.Password)),
                                        disabled(model.IsSubmitting.ToString())]
                                    )
                                ]),                                button([@class("btn btn-lg btn-primary pull-xs-right"),
                                    type("button"),
                                    disabled((model.IsSubmitting || 
                                             string.IsNullOrWhiteSpace(model.Username) || 
                                             string.IsNullOrWhiteSpace(model.Email)).ToString()),
                                    onclick(new Message.SettingsSubmitted())],
                                    [text(model.IsSubmitting ? "Updating Settings..." : "Update Settings")]
                                )
                            ])
                        ]),
                        hr([@class("my-4")]),
                        button([@class("btn btn-outline-danger"),
                            onclick(new Message.LogoutRequested())],
                            [text("Or click here to logout.")]
                        )
                    ])
                ])
            ])
        ]);
}