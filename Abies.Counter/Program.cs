using Abies;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;
using System.Runtime.InteropServices.JavaScript;
using Abies.DOM;

await Runtime.Run<Counter, Arguments, Model>(new Arguments());

public static partial class Interop
{
}
public record Arguments
{
}

public record Model(int Count);

public record Increment : Message;

public record Decrement : Message;

public class Counter : Program<Model, Arguments>
{
    public static (Model, Command) Initialize(Url url, Arguments argument)
    {
        return (new Model(0), Commands.None);
    }

    public static Message OnLinkClicked(UrlRequest urlRequest)
    {
        return new Increment();
    }

    public static Message OnUrlChanged(Url url)
    {
        return new Increment();
    }

    public static Subscription Subscriptions(Model model)
        => new();

    public static Task HandleCommand(Command command, Func<Message, System.ValueTuple> dispatch) => Task.CompletedTask;

    public static (Model model, Command command) Update(Message message, Model model)
    {
        switch (message)
        {
            case Increment _:
                model = model with { Count = model.Count + 1 };
                return (model, Commands.None);
            case Decrement _:
                model = model with { Count = model.Count - 1 };
                return (model, Commands.None);
            default:
                throw new NotImplementedException();
        }
    }

    public static Document View(Model model)
        => new("Counter",
            div([],
            [
                a(
                    [
                        href("https://www.google.com")
                    ],
                    [
                        text("Google")
                    ]
                ),
                button(
                    [
                        type("button"),
                        onclick(new Increment())
                    ],
                    [
                        text("+")
                    ]
                ),
                button(
                    [
                        type("button"),
                        onclick(new Decrement())
                    ],
                    [
                        text("-")
                    ]
                ),
                text(model.Count.ToString())
            ])
        );
}