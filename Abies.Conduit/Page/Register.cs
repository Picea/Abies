using Abies.Conduit.Main;
using Abies.Conduit.Routing;
using Abies.Conduit;
using Abies.DOM;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

namespace Abies.Conduit.Page.Register;

public interface Message : Abies.Message
{
    public record RegisterSuccess(User User) : Message;
    public record UsernameChanged(string Value) : Message;
    public record EmailChanged(string Value) : Message;
    public record PasswordChanged(string Value) : Message;
    public record RegisterSubmitted : Message;
    public record RegisterError(Dictionary<string, string[]> Errors) : Message;
}

public record Model(
    string Username = "",
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
            Message.UsernameChanged usernameChanged => (
                model with { Username = usernameChanged.Value },
                Commands.None
            ),
            Message.EmailChanged emailChanged => (
                model with { Email = emailChanged.Value },
                Commands.None
            ),
                        Message.PasswordChanged passwordChanged => (
                model with { Password = passwordChanged.Value },
                Commands.None
            ),            Message.RegisterSubmitted => (
                model with { IsSubmitting = true, Errors = null },
                new Abies.Conduit.RegisterCommand(model.Username, model.Email, model.Password)
            ),
            Message.RegisterSuccess registerSuccess => (
                model with { IsSubmitting = false, Errors = null },
                Commands.None
            ),
            Message.RegisterError errors => (
                model with { IsSubmitting = false, Errors = errors.Errors },
                Commands.None
            ),
            _ => (model, Commands.None)
        };    private static Node ErrorList(Dictionary<string, string[]>? errors) =>
        errors == null
            ? text("")
            : ul([class_("error-messages")], 
                [..errors.SelectMany(e => e.Value.Select(msg => 
                    li([], [text($"{e.Key} {msg}")])
                ))]
            );    public static Node View(Model model) =>
        div([class_("auth-page")], [
            div([class_("container page")], [
                div([class_("row")], [
                    div([class_("col-md-6 offset-md-3 col-xs-12")], [
                        h1([class_("text-xs-center")], [text("Sign up")]),
                        p([class_("text-xs-center")], [
                            a([href("/login")], [text("Have an account?")])
                        ]),
                        
                        ErrorList(model.Errors),

                        form([], [
                            fieldset([], [                                fieldset([class_("form-group")], [
                                    input([class_("form-control form-control-lg"),
                                        type("text"),
                                        placeholder("Username"),
                                        value(model.Username),
                                        oninput(new Message.UsernameChanged(model.Username)),
                                        disabled(model.IsSubmitting.ToString())]
                                    )
                                ]),
                                fieldset([class_("form-group")], [
                                    input([class_("form-control form-control-lg"),
                                        type("text"),
                                        placeholder("Email"),
                                        value(model.Email),
                                        oninput(new Message.EmailChanged(model.Email)),
                                        disabled(model.IsSubmitting.ToString())]
                                    )
                                ]),
                                fieldset([class_("form-group")], [
                                    input([class_("form-control form-control-lg"),
                                        type("password"),
                                        placeholder("Password"),
                                        value(model.Password),
                                        oninput(new Message.PasswordChanged(model.Password)),
                                        disabled(model.IsSubmitting.ToString())]
                                    )
                                ]),                                button([class_("btn btn-lg btn-primary pull-xs-right"),
                                    type("button"),
                                    disabled((model.IsSubmitting || 
                                             string.IsNullOrWhiteSpace(model.Username) || 
                                             string.IsNullOrWhiteSpace(model.Email) || 
                                             string.IsNullOrWhiteSpace(model.Password)).ToString()),
                                    onclick(new Message.RegisterSubmitted())],
                                    [text(model.IsSubmitting ? "Signing up..." : "Sign up")]
                                )
                            ])
                        ])
                    ])
                ])
            ])
        ]);
}