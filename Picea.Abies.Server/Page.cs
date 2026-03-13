// =============================================================================
// Page — Pure HTML Page Rendering
// =============================================================================
// Converts an Abies Document into a complete HTML page string.
//
// This is the core pure function of Static SSR:
//
//     Render : (Program, RenderMode, Url?) → string
//
// The function composes three existing pure computations:
//   1. TProgram.Initialize(argument) → (model, command)
//   2. TProgram.View(model) → Document
//   3. Render.Html(document.Body) → body HTML string
//   4. HeadContent.ToHtml() → head element strings
//
// The result is a complete <!DOCTYPE html> page ready to serve.
//
// For Interactive modes, the appropriate bootstrap script tags are injected:
//   - InteractiveServer → WebSocket client script
//   - InteractiveWasm → .NET WASM bootstrap script
//   - InteractiveAuto → both (WebSocket first, WASM handoff)
//
// No ASP.NET Core dependencies. No server dependencies. Pure string output.
//
// Architecture note: This follows the same pattern as Elm's
// elm/browser package where `Browser.document` produces a full page
// from a Program. The difference is we do it server-side.
// =============================================================================

using System.Diagnostics;
using System.Text;
using Picea.Abies.DOM;

namespace Picea.Abies.Server;

/// <summary>
/// Pure functions for rendering an Abies application as a complete HTML page.
/// </summary>
/// <remarks>
/// <para>
/// This is the entry point for server-side rendering. Given a program type,
/// it initializes the model, renders the view, and produces a full
/// <c>&lt;!DOCTYPE html&gt;</c> page string.
/// </para>
/// <para>
/// The rendering is a one-shot pure computation — no runtime is started,
/// no subscriptions are activated, no commands are interpreted. This makes
/// it suitable for Static SSR where no interactivity is needed.
/// </para>
/// <para>
/// For Interactive modes, the page includes bootstrap scripts that establish
/// client-side interactivity after the initial server-rendered HTML is displayed.
/// </para>
/// </remarks>
public static class Page
{
    private static readonly ActivitySource _activitySource = new("Picea.Abies.Server.Page");

    /// <summary>
    /// Renders an Abies program as a complete HTML page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rendering pipeline:
    /// <list type="number">
    ///   <item><c>TProgram.Initialize(argument)</c> → initial model</item>
    ///   <item><c>TProgram.View(model)</c> → virtual DOM Document</item>
    ///   <item><see cref="RenderDocument"/> → full HTML string</item>
    /// </list>
    /// </para>
    /// <para>
    /// Initial commands from <c>Initialize</c> are discarded in Static mode
    /// (no interpreter to handle them). For Interactive modes, the server-side
    /// session handles them.
    /// </para>
    /// </remarks>
    /// <typeparam name="TProgram">The Abies program type.</typeparam>
    /// <typeparam name="TModel">The application model (state).</typeparam>
    /// <typeparam name="TArgument">Initialization parameters.</typeparam>
    /// <param name="mode">The render mode determining what bootstrap scripts to inject.</param>
    /// <param name="argument">Initialization parameters for the program.</param>
    /// <param name="initialUrl">
    /// Optional initial URL for route-dependent rendering.
    /// When provided, a <see cref="UrlChanged"/> message is dispatched
    /// synchronously before rendering to allow the program to route.
    /// Only effective for programs that handle <see cref="UrlChanged"/> in their
    /// <c>Transition</c> function.
    /// </param>
    /// <returns>A complete <c>&lt;!DOCTYPE html&gt;</c> page string.</returns>
    public static string Render<TProgram, TModel, TArgument>(
        RenderMode mode,
        TArgument argument = default!,
        Url? initialUrl = null)
        where TProgram : Program<TModel, TArgument>
    {
        using var activity = _activitySource.StartActivity("Picea.Abies.Server.Page.Render");
        activity?.SetTag("abies.program", typeof(TProgram).Name);
        activity?.SetTag("abies.renderMode", mode.GetType().Name);

        // Step 1: Initialize the program to get the model
        var (model, _) = TProgram.Initialize(argument);

        // Step 2: If an initial URL is provided, run it through the transition
        // so the program can route to the correct page before rendering.
        if (initialUrl is not null)
        {
            var (routedModel, _) = TProgram.Transition(model, new UrlChanged(initialUrl));
            model = routedModel;
        }

        // Step 3: Render the view
        var document = TProgram.View(model);

        // Step 4: Produce the full HTML page
        var html = RenderDocument(document, mode);

        activity?.SetStatus(ActivityStatusCode.Ok);
        return html;
    }

    /// <summary>
    /// Renders a <see cref="Document"/> as a complete HTML page string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the pure function that composes the page from its parts:
    /// <list type="bullet">
    ///   <item>DOCTYPE and html/head/body structure</item>
    ///   <item>Title from <see cref="Document.Title"/></item>
    ///   <item>Head elements from <see cref="Document.Head"/> via <c>ToHtml()</c></item>
    ///   <item>Body content from <see cref="Document.Body"/> via <see cref="Render.Html"/></item>
    ///   <item>Bootstrap scripts based on <see cref="RenderMode"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="document">The virtual DOM document to render.</param>
    /// <param name="mode">The render mode for script injection.</param>
    /// <returns>A complete HTML page string.</returns>
    public static string RenderDocument(Document document, RenderMode mode)
    {
        var sb = new StringBuilder(4096);

        sb.Append("<!DOCTYPE html>\n");
        sb.Append("<html>\n");

        // ── Head ─────────────────────────────────────────────────
        sb.Append("<head>\n");
        sb.Append("  <meta charset=\"utf-8\">\n");
        sb.Append("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        sb.Append("  <title>");
        sb.Append(System.Web.HttpUtility.HtmlEncode(document.Title));
        sb.Append("</title>\n");

        // Managed head elements (stylesheets, meta tags, etc.)
        foreach (var head in document.Head)
        {
            sb.Append("  ");
            sb.Append(head.ToHtml());
            sb.Append('\n');
        }

        sb.Append("</head>\n");

        // ── Body ─────────────────────────────────────────────────
        sb.Append("<body>\n");
        sb.Append(Abies.Render.Html(document.Body));
        sb.Append('\n');

        // ── Bootstrap Scripts ────────────────────────────────────
        AppendBootstrapScripts(sb, mode);

        sb.Append("</body>\n");
        sb.Append("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Appends render-mode-specific bootstrap scripts to the page.
    /// </summary>
    private static void AppendBootstrapScripts(StringBuilder sb, RenderMode mode)
    {
        switch (mode)
        {
            case RenderMode.Static:
                // No scripts — pure static HTML
                break;

            case RenderMode.InteractiveServer server:
                // WebSocket client that receives binary patches and applies them to the DOM.
                // The actual script is loaded from the hosting adapter's static files.
                sb.Append("  <script src=\"/_abies/abies-server.js\" data-ws-path=\"");
                sb.Append(System.Web.HttpUtility.HtmlAttributeEncode(server.WebSocketPath));
                sb.Append("\"></script>\n");
                break;

            case RenderMode.InteractiveWasm:
                // Inline module that imports dotnet.js and starts the .NET runtime.
                // Sets __ABIES_DOTNET_STARTED so that when Runtime.Run() later imports
                // abies.js (which also contains bootstrap code), the guard in abies.js
                // skips the redundant dotnet.run() call.
                // The import path MUST be absolute (leading /) so it resolves
                // correctly regardless of the current page URL. A relative path
                // like "./_framework/dotnet.js" would resolve to
                // "/article/_framework/dotnet.js" when the page is at "/article/slug".
                sb.Append("  <script type=\"module\">\n");
                sb.Append("    import { dotnet } from '/_framework/dotnet.js';\n");
                sb.Append("    globalThis.__ABIES_DOTNET_STARTED = true;\n");
                sb.Append("    await dotnet.run();\n");
                sb.Append("  </script>\n");
                break;

            case RenderMode.InteractiveAuto auto:
                // Both: WebSocket for immediate interactivity, WASM for handoff.
                sb.Append("  <script src=\"/_abies/abies-server.js\" data-ws-path=\"");
                sb.Append(System.Web.HttpUtility.HtmlAttributeEncode(auto.WebSocketPath));
                sb.Append("\" data-auto=\"true\"></script>\n");
                sb.Append("  <script type=\"module\">\n");
                sb.Append("    import { dotnet } from '/_framework/dotnet.js';\n");
                sb.Append("    globalThis.__ABIES_DOTNET_STARTED = true;\n");
                sb.Append("    await dotnet.run();\n");
                sb.Append("  </script>\n");
                break;
        }
    }
}
