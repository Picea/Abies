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

Console.WriteLine("Bootstrapping...");

Initialize<Arguments, Model> initialize = arguments =>
{
    return new Model(0);
};

Update<Model> update = (command, model) =>
{
    Console.WriteLine($"Command: {command} in counter");
    return command switch
    {
        Increment _ => model with { Count = model.Count + 1 },
        Decrement _ => model with { Count = model.Count - 1 },
        _ => throw new NotImplementedException()
    };
};

View<Model> view = model =>
    new("Counter", 
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

Subscriptions<Model> subscriptions = model =>
{
    return new Subscription();
};



var counter = Application(initialize, view, update, subscriptions, (url => {
    var urlString = url.ToString(); if (urlString is not null) Interop.WriteToConsole(urlString);
    return new Increment();}), (url => {
        Interop.WriteToConsole(url.ToString());
        return new Increment();}));

await Runtime.Run(new Arguments(), counter);

public  static partial class Interop{
    [JSImport("writeToConsole", "abies.js")]
        public static partial Task WriteToConsole(string message);
}
public record Arguments
{
}

public record Model(int Count);

public record Increment : Command;

public record Decrement : Command;



