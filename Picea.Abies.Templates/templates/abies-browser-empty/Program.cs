using Picea.Abies;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Events;

// Start the Abies runtime with your application
await Picea.Abies.Browser.Runtime.Run<App, Model, Arguments>();

/// <summary>
/// Application startup arguments.
/// </summary>
public record Arguments;

/// <summary>
/// The application model (state).
/// Add your application state properties here.
/// </summary>
public record Model;

/// <summary>
/// The main application implementing the MVU pattern.
/// </summary>
public class App : Program<Model, Arguments>
{
    /// <summary>
    /// Initialize the application with an initial model and optional command.
    /// </summary>
    public static (Model, Command) Initialize(Arguments argument)
        => (new Model(), Commands.None);

    /// <summary>
    /// Transition the model based on incoming messages.
    /// </summary>
    public static (Model, Command) Transition(Model model, Message message)
        => (model, Commands.None);

    /// <summary>
    /// Render the view based on the current model.
    /// </summary>
    public static Document View(Model model)
        => new("AbiesApp",
            div([class_("container")],
            [
                h1([], [text("Welcome to Abies!")]),
                p([], [text("Start building your MVU application.")]),
                p([],
                [
                    text("Learn more at "),
                    a([href("https://github.com/Picea/Abies")], [text("github.com/Picea/Abies")])
                ])
            ])
        );

    /// <summary>
    /// Define subscriptions for external events (timers, websockets, etc.).
    /// </summary>
    public static Subscription Subscriptions(Model model)
        => new Subscription.None();
}
