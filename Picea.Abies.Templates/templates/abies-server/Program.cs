using Picea.Abies;
using Picea.Abies.DOM;
using Picea.Abies.Server;
using Picea.Abies.Server.Kestrel;
using Picea.Abies.Subscriptions;
using Picea;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Events;
using static Picea.Abies.Head;

// =============================================================================
// AbiesServerApp — Server-Rendered MVU Counter
// =============================================================================
//
// This application runs the MVU loop on the server. The server renders HTML
// and sends binary DOM patches over a WebSocket. Each browser tab gets its
// own isolated MVU runtime.
//
// Usage:
//     dotnet run
//     → http://localhost:5000
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
var debugUiOptOut = string.Equals(
    Environment.GetEnvironmentVariable("ABIES_DEBUG_UI"),
    "0",
    StringComparison.OrdinalIgnoreCase);

Picea.Abies.Debugger.DebuggerConfiguration.ConfigureDebugger(
    new Picea.Abies.Debugger.DebuggerOptions { Enabled = !debugUiOptOut });
#endif

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing
        .AddSource("Picea.Abies")
        .AddSource("Picea.Abies.Server.Kestrel.OtlpProxy")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

app.UseWebSockets();
app.UseStaticFiles();
app.UseAbiesStaticFiles();
app.MapOtlpProxy();
app.MapAbies<Counter, CounterModel, Unit>(
    "/{**catch-all}",
    new RenderMode.InteractiveServer());

app.Run();

// ---------------------------------------------------------------------------
// Model
// ---------------------------------------------------------------------------

/// <summary>
/// The application model (state) — a single immutable counter value.
/// </summary>
public record CounterModel(int Count);

// ---------------------------------------------------------------------------
// Messages
// ---------------------------------------------------------------------------

/// <summary>Increment the counter by one.</summary>
public record Increment : Message;

/// <summary>Decrement the counter by one.</summary>
public record Decrement : Message;

/// <summary>Reset the counter to zero.</summary>
public record Reset : Message;

// ---------------------------------------------------------------------------
// Program
// ---------------------------------------------------------------------------

/// <summary>
/// The Counter program implementing the MVU pattern for server-side rendering.
/// </summary>
/// <remarks>
/// <para>
/// This is a pure functional program definition. All methods are static.
/// The Picea kernel drives the state machine; Abies wires in the
/// View → Diff → Apply pipeline. The server sends DOM patches to the
/// browser over a WebSocket connection.
/// </para>
/// </remarks>
public sealed class Counter : Program<CounterModel, Unit>
{
    /// <summary>
    /// Initialize the application with an initial model and optional command.
    /// </summary>
    public static (CounterModel, Command) Initialize(Unit argument) =>
        (new CounterModel(0), Commands.None);

    /// <summary>
    /// Update the model based on incoming messages.
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

    public static Result<Message[], Message> Decide(CounterModel _, Message command) =>
        Result<Message[], Message>.Ok([command]);

    public static bool IsTerminal(CounterModel _) => false;

    /// <summary>
    /// Render the view based on the current model.
    /// </summary>
    public static Document View(CounterModel model) =>
        new("AbiesServerApp",
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
                            span([class_("brand-subtitle")], [text("MVU Counter — Server Mode")])
                        ])
                    ]),
                    span([class_("badge badge-server")], [text("Server")])
                ]),

                // Main content
                main([class_("content")],
                [
                    h1([], [text("Counter")]),
                    p([class_("subtitle")], [text("Server-rendered Model-View-Update")]),

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
                        p([], [text("This app runs on the server. Each button click sends a message over a WebSocket. The server processes it, diffs the virtual DOM, and sends binary patches back to the browser.")])
                    ])
                ]),

                // Footer
                footer([class_("footer")],
                [
                    text("Built with "),
                    a([href("https://github.com/Picea/Abies")], [text("Abies")]),
                    text(" \u2014 Functional web apps in .NET")
                ])
            ]),
            stylesheet("/site.css")
        );

    /// <summary>
    /// Define subscriptions for external events (timers, websockets, etc.).
    /// </summary>
    public static Subscription Subscriptions(CounterModel model) =>
        new Subscription.None();
}
