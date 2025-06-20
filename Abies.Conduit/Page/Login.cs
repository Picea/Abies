using Abies.Conduit.Main;
using Abies.Conduit.Routing;
using Abies.Conduit.Services;
using Abies.DOM;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

namespace Abies.Conduit.Page.Login;

public interface Message : Abies.Message
{
    public record LoginSuccess(User User) : Message;
    public record EmailChanged(string Value) : Message;
    public record PasswordChanged(string Value) : Message;
    public record LoginSubmitted : Message;
    public record LoginError(string[] Errors) : Message;
}

public record Model(
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
            Message.EmailChanged emailChanged => (
                model with { Email = emailChanged.Value },
                []
            ),
            Message.PasswordChanged passwordChanged => (
                model with { Password = passwordChanged.Value },
                []
            ),            Message.LoginSubmitted => (
                model with { IsSubmitting = true, Errors = null },
                [new LoginCommand(model.Email, model.Password)]
            ),
            Message.LoginSuccess loginSuccess => (
                model with { IsSubmitting = false, Errors = null },
                []
            ),
            Message.LoginError errors => (
                model with 
                { 
                    IsSubmitting = false,
                    Errors = errors.Errors.ToDictionary(e => e, e => new string[] { e })
                },
                []
            ),
            _ => (model, [])
        };

    private static Node ErrorList(Dictionary<string, string[]>? errors) =>
        errors == null
            ? text("")
            : ul([@class("error-messages")], 
                [..errors.SelectMany(e => e.Value.Select(msg => 
                    li([], [text($"{e.Key} {msg}")])
                ))]
            );

    public static Node View(Model model) =>
        div([@class("auth-page")], [
            div([@class("container page")], [
                div([@class("row")], [
                    div([@class("col-md-6 offset-md-3 col-xs-12")], [
                        h1([@class("text-xs-center")], [text("Sign in")]),
                        p([@class("text-xs-center")], [
                            a([href("/register")], [text("Need an account?")])
                        ]),
                        
                        ErrorList(model.Errors),

                        form([], [
                            fieldset([], [
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
                                ]),
                                button([@class("btn btn-lg btn-primary pull-xs-right"),
                                    type("button"),
                                    disabled((model.IsSubmitting || 
                                             string.IsNullOrWhiteSpace(model.Email) || 
                                             string.IsNullOrWhiteSpace(model.Password)).ToString()),
                                    onclick(new Message.LoginSubmitted())],
                                    [text(model.IsSubmitting ? "Signing in..." : "Sign in")]
                                )
                            ])
                        ])
                    ])
                ])
            ])
        ]);
}
