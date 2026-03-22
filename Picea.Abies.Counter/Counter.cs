// =============================================================================
// Counter — An Abies MVU Counter Application
// =============================================================================
// A minimal counter that demonstrates the Abies framework's MVU loop:
//
//     Model: a single integer count
//     Messages: Increment, Decrement, Reset
//     View: buttons and a count display
//
// Implements Program<CounterModel, Unit> which extends the Picea kernel
// with View and Subscriptions.
//
// This is a pure program definition with no platform dependencies. Both the
// WASM host (Picea.Abies.Counter.Wasm) and server host (Picea.Abies.Counter.Server)
// reference this library.
// =============================================================================

using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Head;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Counter;

/// <summary>
/// The application model — a single immutable counter value.
/// </summary>
/// <param name="Count">The current count value.</param>
public record CounterModel(int Count);

/// <summary>
/// Messages that the counter can process.
/// </summary>
public interface CounterMessage : Message;

/// <summary>Increment the counter by one.</summary>
public record Increment : CounterMessage;

/// <summary>Decrement the counter by one.</summary>
public record Decrement : CounterMessage;

/// <summary>Reset the counter to zero.</summary>
public record Reset : CounterMessage;

/// <summary>
/// The Counter program — implements the Abies MVU contract.
/// </summary>
/// <remarks>
/// <para>
/// This is a pure functional program definition. All methods are static.
/// The Picea kernel drives the state machine; Abies wires in the
/// View → Diff → Apply pipeline.
/// </para>
/// </remarks>
public sealed class CounterProgram : Program<CounterModel, Unit>
{
    /// <summary>
    /// Initializes the counter with a count of zero and no initial commands.
    /// </summary>
    public static (CounterModel, Command) Initialize(Unit argument) =>
        (new CounterModel(0), Commands.None);

    /// <summary>
    /// Transitions the counter model in response to messages.
    /// Pure function: no side effects, just state transformation.
    /// </summary>
    public static (CounterModel, Command) Transition(CounterModel model, Message message) =>
        message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            Reset => (model with { Count = 0 }, Commands.None),
            _ => (model, Commands.None)
        };

    /// <summary>
    /// Renders the counter view as a virtual DOM document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The view is a pure function of the model. The stylesheet reference
    /// uses the Abies brand palette defined in counter.css, served by
    /// the host project's static file middleware.
    /// </para>
    /// </remarks>
    public static Document View(CounterModel model) =>
        new("Abies Counter",
            div([class_("counter")],
            [
                h1([], [text("Abies Counter")]),
                div([class_("controls")],
                [
                    button([class_("btn"), onclick(new Decrement())], [text("−")]),
                    span([class_("count")], [text(model.Count.ToString())]),
                    button([class_("btn"), onclick(new Increment())], [text("+")])
                ]),
                button([class_("reset"), onclick(new Reset())], [text("Reset")])
            ]),
            stylesheet("/counter.css"));

    /// <summary>
    /// No subscriptions needed for the counter.
    /// </summary>
    public static Subscription Subscriptions(CounterModel model) =>
        new Subscription.None();
}
