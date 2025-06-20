using System;
using Abies;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;
using static Abies.Browser;
using static Abies.Counter.Fluent;
using System.Diagnostics.Metrics;
using Abies.Counter;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Collections.Generic;

Console.WriteLine("Bootstrapping...");

var counter = Browser.Application<Counter, Arguments, Model>();

await Runtime.Run(counter, new Arguments());

public static partial class Interop
{
    [JSImport("writeToConsole", "abies.js")]
    public static partial Task WriteToConsole(string message);
}
public record Arguments
{
}

public record Model(int Count);

public record Increment : Message;

public record Decrement : Message;

public class Counter : Application<Model, Arguments>
{
    public static Model Initialize(Url url, Arguments argument)
    {
        Interop.WriteToConsole($"initialized");
        return new Model(0);
    }

    public static Message OnLinkClicked(UrlRequest urlRequest)
    {
        Interop.WriteToConsole($"link clicked");
        return new Increment();
    }

    public static Message OnUrlChanged(Url url)
    {
        Interop.WriteToConsole($"url changed");
        return new Increment();
    }

    public static Subscription Subscriptions(Model model)
        => new();

    public static (Model model, IEnumerable<Command> commands) Update(Message message, Model model)
    {
        switch (message)
        {
            case Increment _:
                Interop.WriteToConsole($"increment received");
                model = model with { Count = model.Count + 1 };
                Interop.WriteToConsole($"model count : {model.Count.ToString()}");
                return (model, Array.Empty<Command>());
            case Decrement _:
                Interop.WriteToConsole($"decrement received");
                model = model with { Count = model.Count - 1 };
                Interop.WriteToConsole($"model count : {model.Count.ToString()}");
                return (model, Array.Empty<Command>());
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