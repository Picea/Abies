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

// ─── Messages ───────────────────────────────────────────────────────────────
// Messages represent all possible events in your application.
// Each message type triggers a specific state transition.

/// <summary>
/// Increment the counter by 1.
/// </summary>
public record Increment : Message;

/// <summary>
/// Decrement the counter by 1.
/// </summary>
public record Decrement : Message;

/// <summary>
/// Reset the counter to 0.
/// </summary>
public record Reset : Message;

// ─── Program ────────────────────────────────────────────────────────────────

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
            Reset => (new Model(0), Commands.None),
            _ => (model, Commands.None)
        };

    /// <summary>
    /// Render the current model as HTML.
    /// </summary>
    public static Document View(Model model)
        => new("Abies Counter",
            div([class_("app")],
            [
                div([class_("header")],
                [
                    h1([], [text("\ud83c\udf32 Abies Counter")]),
                    p([class_("subtitle")], [text("Model-View-Update for .NET WebAssembly")])
                ]),

                div([class_("counter-section")],
                [
                    div([class_("counter-display")],
                    [
                        span([class_("counter-value")], [text(model.Count.ToString())])
                    ]),

                    div([class_("button-group")],
                    [
                        button([class_("btn btn-decrement"), onclick(new Decrement())], [text("\u2212 Decrease")]),
                        button([class_("btn btn-reset"), onclick(new Reset())], [text("\u21ba Reset")]),
                        button([class_("btn btn-increment"), onclick(new Increment())], [text("+ Increase")])
                    ]),

                    div([class_("info-panel")],
                    [
                        p([], [text("Each button click dispatches a message. The Transition function creates a new immutable model. The View renders the current state.")])
                    ])
                ]),

                div([class_("footer")],
                [
                    p([], [text("Built with Abies \u2014 The Elm Architecture for .NET")])
                ])
            ])
        );

    /// <summary>
    /// Define subscriptions for external events (timers, websockets, etc.).
    /// </summary>
    public static Subscription Subscriptions(Model model)
        => new Subscription.None();
}
