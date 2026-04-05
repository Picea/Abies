using Picea;
using Picea.Abies;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Events;

// Start the Abies runtime with the Counter program
await Picea.Abies.Browser.Runtime.Run<Counter, Model, Unit>();

/// <summary>
/// Application model — immutable state container.
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
/// Message to reset the counter to zero.
/// </summary>
public record Reset : Message;

/// <summary>
/// Counter application implementing the MVU pattern.
/// </summary>
public class Counter : Program<Model, Unit>
{
    /// <summary>
    /// Initialize the application with an initial model and optional command.
    /// </summary>
    public static (Model, Command) Initialize(Unit argument)
        => (new Model(0), Commands.None);

    /// <summary>
    /// Transition the model based on incoming messages.
    /// </summary>
    public static (Model, Command) Transition(Model model, Message message)
        => message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            Reset => (model with { Count = 0 }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Result<Message[], Message> Decide(Model _, Message command)
        => Result<Message[], Message>.Ok([command]);

    public static bool IsTerminal(Model _) => false;

    /// <summary>
    /// Render the current model as HTML.
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
                    h1([], [text("Abies Counter")]),
                    p([class_("subtitle")], [text("Model-View-Update in action")]),

                    div([class_("counter")],
                    [
                        div([class_("counter-controls")],
                        [
                            button(
                                [type("button"), onclick(new Decrement()), class_("btn"), ariaLabel("Decrease")],
                                [text("-")]
                            ),
                            span([class_("count")], [text(model.Count.ToString())]),
                            button(
                                [type("button"), onclick(new Increment()), class_("btn"), ariaLabel("Increase")],
                                [text("+")]
                            )
                        ]),
                        button(
                            [type("button"), onclick(new Reset()), class_("btn-reset")],
                            [text("Reset")]
                        )
                    ]),

                    div([class_("info-panel")],
                    [
                        p([], [text("Each button click dispatches a message. The Transition function creates a new immutable model. The View renders the current state.")])
                    ])
                ]),

                // Footer
                footer([class_("footer")],
                [
                    text("Built with "),
                    a([href("https://github.com/Picea/Abies")], [text("Abies")]),
                    text(" \u2014 Functional web apps in .NET")
                ])
            ])
        );

    /// <summary>
    /// Define subscriptions for external events (timers, websockets, etc.).
    /// </summary>
    public static Subscription Subscriptions(Model model)
        => new Subscription.None();
}
