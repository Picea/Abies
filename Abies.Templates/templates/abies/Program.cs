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
        => new("AbiesApp",
            div([class_("app")],
            [
                // Header
                header([class_("topbar")],
                [
                    div([class_("brand")],
                    [
                        img([class_("brand-logo"), src("https://avatars.githubusercontent.com/u/188364441?v=4"), alt("Abies logo")]),
                        div([class_("brand-meta")],
                        [
                            span([class_("brand-wordmark")], [text("Abies")]),
                            span([class_("brand-subtitle")], [text("MVU Counter Example")])
                        ])
                    ]),
                    span([class_("badge")], [text("Demo")])
                ]),

                // Main content
                main([class_("content")],
                [
                    h1([], [text("Counter")]),
                    p([class_("subtitle")], [text("Model-View-Update in action")]),
                    
                    div([class_("counter")],
                    [
                        button(
                            [type("button"), onclick(new Decrement()), class_("btn")],
                            [text("−")]
                        ),
                        span([class_("count")], [text(model.Count.ToString())]),
                        button(
                            [type("button"), onclick(new Increment()), class_("btn")],
                            [text("+")]
                        )
                    ]),

                    div([class_("info-panel")],
                    [
                        p([], [text("Each button click dispatches a message. The Update function creates a new immutable model. The View renders the current state.")])
                    ])
                ]),

                // Footer
                footer([class_("footer")],
                [
                    text("Built with "),
                    a([href("https://github.com/Picea/Abies")], [text("Abies")]),
                    text(" — Functional web apps in .NET")
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
