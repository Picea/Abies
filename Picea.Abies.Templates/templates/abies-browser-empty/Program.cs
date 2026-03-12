using Picea;
using Picea.Abies;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Events;

// Start the Abies runtime with your application
await Picea.Abies.Browser.Runtime.Run<App, Model, Unit>();

/// <summary>
/// Application model — immutable state container.
/// Add your application state properties here.
/// </summary>
public record Model;

// ─── Messages ───────────────────────────────────────────────────────────────
// Add your message types here. Each message represents a user action or event.

// ─── Program ────────────────────────────────────────────────────────────────

/// <summary>
/// Your Abies application implementing the MVU pattern.
/// </summary>
public class App : Program<Model, Unit>
{
    /// <summary>
    /// Initialize the application with an initial model and optional command.
    /// </summary>
    public static (Model, Command) Initialize(Unit argument)
        => (new Model(), Commands.None);

    /// <summary>
    /// Transition the model based on incoming messages.
    /// </summary>
    public static (Model, Command) Transition(Model model, Message message)
        => (model, Commands.None);

    /// <summary>
    /// Render the current model as HTML.
    /// </summary>
    public static Document View(Model model)
        => new("Abies App",
            div([class_("app")],
            [
                h1([], [text("🌲 Welcome to Abies")]),
                p([], [text("Start building your MVU application!")])
            ])
        );

    /// <summary>
    /// Define subscriptions for external events (timers, websockets, etc.).
    /// </summary>
    public static Subscription Subscriptions(Model model)
        => new Subscription.None();
}
