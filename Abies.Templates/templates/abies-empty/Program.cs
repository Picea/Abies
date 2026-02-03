using Abies;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

// Start the Abies runtime with your application
await Runtime.Run<App, Arguments, Model>(new Arguments());

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
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(), Commands.None);

    /// <summary>
    /// Update the model based on incoming messages.
    /// </summary>
    public static (Model model, Command command) Update(Message message, Model model)
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
    /// Handle URL changes (for routing).
    /// </summary>
    public static Message OnUrlChanged(Url url)
        => throw new NotImplementedException("Add your URL change message here");

    /// <summary>
    /// Handle link clicks (for navigation).
    /// </summary>
    public static Message OnLinkClicked(UrlRequest urlRequest)
        => throw new NotImplementedException("Add your link click message here");

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
