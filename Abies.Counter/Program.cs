using Abies;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

// Start the Abies runtime with the Counter program
await Runtime.Run<Counter, Arguments, Model>(new Arguments());

/// <summary>
/// Application startup arguments.
/// </summary>
public record Arguments;

/// <summary>
/// The application model (state).
/// </summary>
public record Model(int Count);

/// <summary>
/// Message to increment the counter.
/// </summary>
public record Increment : Message;

/// <summary>
/// Message to decrement the counter.
/// </summary>
public record Decrement : Message;

/// <summary>
/// The Counter program implementing the MVU pattern.
/// </summary>
public class Counter : Program<Model, Arguments>
{
    /// <summary>
    /// Initialize the application with an initial model and optional command.
    /// </summary>
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(0), Commands.None);

    /// <summary>
    /// Update the model based on incoming messages.
    /// </summary>
    public static (Model model, Command command) Update(Message message, Model model)
        => message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            _ => (model, Commands.None)
        };

    /// <summary>
    /// Render the view based on the current model.
    /// </summary>
    public static Document View(Model model)
        => new("Counter",
            div([class_("container")],
            [
                h1([], [text("Counter")]),
                div([class_("counter")],
                [
                    button(
                        [
                            type("button"),
                            onclick(new Decrement()),
                            class_("btn")
                        ],
                        [text("-")]
                    ),
                    span([class_("count")], [text(model.Count.ToString())]),
                    button(
                        [
                            type("button"),
                            onclick(new Increment()),
                            class_("btn")
                        ],
                        [text("+")]
                    )
                ]),
                p([class_("description")], 
                [
                    text("Click the buttons to increment or decrement the counter.")
                ])
            ])
        );

    /// <summary>
    /// Handle URL changes (for routing).
    /// </summary>
    public static Message OnUrlChanged(Url url) => new Increment();

    /// <summary>
    /// Handle link clicks (for navigation).
    /// </summary>
    public static Message OnLinkClicked(UrlRequest urlRequest) => new Increment();

    /// <summary>
    /// Define subscriptions for external events (timers, websockets, etc.).
    /// </summary>
    public static Subscription Subscriptions(Model model)
        => SubscriptionModule.None;

    /// <summary>
    /// Handle commands (side effects like HTTP requests).
    /// </summary>
    public static Task HandleCommand(Command command, Func<Message, System.ValueTuple> dispatch)
        => Task.CompletedTask;
}
