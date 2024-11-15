using System;
using Abies;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;
using static Abies.Browser;
using static Abies.Counter.Fluent;
using System.Diagnostics.Metrics;
using Abies.Counter;

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

var counter = Document(initialize, view, update, subscriptions);

await Runtime.Run(new Arguments(), counter);

public record Arguments
{
}

public record Model(int Count);

public record Increment : Command;

public record Decrement : Command;



