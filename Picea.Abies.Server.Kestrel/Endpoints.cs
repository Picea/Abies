// =============================================================================
// Endpoints — MapAbies Extension Method for ASP.NET Core
// =============================================================================
// Provides the single-call entry point for hosting an Abies application
// in a Kestrel-based ASP.NET Core server:
//
//     app.MapAbies<MyApp, MyModel, Unit>("/", new RenderMode.InteractiveServer());
//
// This one call wires up:
//   1. A GET endpoint at {path} that serves the server-rendered HTML page
//   2. A WebSocket endpoint at {wsPath} for interactive modes
//
// For Static mode, only the GET endpoint is created (no WebSocket).
// For InteractiveServer/InteractiveAuto, both endpoints are created.
// For InteractiveWasm, only the GET endpoint is created (WASM handles interactivity).
//
// Architecture: This is the "adapter" side of Ports & Adapters. It maps
// ASP.NET Core's HTTP/WebSocket primitives to the pure Picea.Abies.Server
// computational library via the delegate types in Transport.cs.
//
// See also:
//   - Picea.Abies.Server/Page.cs — pure HTML rendering
//   - Picea.Abies.Server/Session.cs — pure MVU session
//   - WebSocketTransport.cs — WebSocket ↔ delegate adapter
// =============================================================================

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Picea.Abies.Server.Kestrel;

/// <summary>
/// Extension methods for mapping Abies applications to ASP.NET Core endpoints.
/// </summary>
public static class Endpoints
{
    private static readonly ActivitySource _activitySource = new("Picea.Abies.Server.Kestrel.Endpoints");

    /// <summary>
    /// Maps an Abies MVU application to ASP.NET Core endpoints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Based on the <paramref name="mode"/>, this method creates:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>Static</b>: A single GET endpoint serving pre-rendered HTML</item>
    ///   <item><b>InteractiveServer</b>: GET endpoint + WebSocket endpoint for live updates</item>
    ///   <item><b>InteractiveWasm</b>: GET endpoint only (WASM bootstrap script included)</item>
    ///   <item><b>InteractiveAuto</b>: GET endpoint + WebSocket endpoint (transitions to WASM)</item>
    /// </list>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.MapAbies&lt;CounterProgram, CounterModel, Unit&gt;(
    ///     "/", new RenderMode.InteractiveServer());
    /// app.Run();
    /// </code>
    /// </example>
    /// </remarks>
    /// <typeparam name="TProgram">The Abies program type.</typeparam>
    /// <typeparam name="TModel">The application model.</typeparam>
    /// <typeparam name="TArgument">Initialization parameters.</typeparam>
    /// <param name="endpoints">The endpoint route builder (typically <c>WebApplication</c>).</param>
    /// <param name="path">The URL path to serve the application at (e.g., <c>"/"</c>).</param>
    /// <param name="mode">The render mode determining interactivity strategy.</param>
    /// <param name="interpreter">
    /// Command interpreter for side effects. Required for interactive modes;
    /// ignored for static mode. Defaults to a no-op interpreter.
    /// </param>
    /// <param name="argument">Initialization parameters for the program.</param>
    /// <param name="debuggerModelJsonTypeInfo">
    /// Optional source-generated JSON metadata for debugger model snapshots.
    /// </param>
    /// <returns>The endpoint route builder for further chaining.</returns>
    public static IEndpointRouteBuilder MapAbies<TProgram, TModel, TArgument>(
        this IEndpointRouteBuilder endpoints,
        string path,
        RenderMode mode,
        Interpreter<Command, Message>? interpreter = null,
        TArgument argument = default!,
        JsonTypeInfo<TModel>? debuggerModelJsonTypeInfo = null)
        where TProgram : Program<TModel, TArgument>
    {
        var effectiveInterpreter = interpreter ?? NoOpInterpreter;

        // Always map the HTML page endpoint
        MapPageEndpoint<TProgram, TModel, TArgument>(endpoints, path, mode, argument);

        // Map WebSocket endpoint for interactive server modes
        switch (mode)
        {
            case RenderMode.InteractiveServer server:
                MapWebSocketEndpoint<TProgram, TModel, TArgument>(
                    endpoints, server.WebSocketPath, effectiveInterpreter, argument, debuggerModelJsonTypeInfo);
                break;

            case RenderMode.InteractiveAuto auto:
                MapWebSocketEndpoint<TProgram, TModel, TArgument>(
                    endpoints, auto.WebSocketPath, effectiveInterpreter, argument, debuggerModelJsonTypeInfo);
                break;
        }

        return endpoints;
    }

    /// <summary>
    /// Maps the HTML page GET endpoint that serves the initial server-rendered page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the <paramref name="path"/> is a catch-all route (contains <c>**</c>),
    /// <see cref="EndpointRouteBuilderExtensions.MapFallback(IEndpointRouteBuilder, Delegate)"/>
    /// is used instead of <see cref="EndpointRouteBuilderExtensions.MapGet(IEndpointRouteBuilder, string, Delegate)"/>.
    /// This ensures that static file middleware (e.g., <see cref="UseAbiesWasmFiles"/>)
    /// gets priority over the HTML page handler. Without this, a <c>/{**catch-all}</c>
    /// route would swallow requests for <c>/_framework/dotnet.js</c> and other static assets.
    /// </para>
    /// <para>
    /// This follows the standard SPA fallback pattern from ASP.NET Core, where
    /// <c>MapFallback</c> has the lowest route priority — matching only when no
    /// other endpoint or middleware has handled the request.
    /// </para>
    /// </remarks>
    private static void MapPageEndpoint<TProgram, TModel, TArgument>(
        IEndpointRouteBuilder endpoints,
        string path,
        RenderMode mode,
        TArgument argument)
        where TProgram : Program<TModel, TArgument>
    {
        // Handler shared by both MapGet and MapFallback
        RequestDelegate handler = (HttpContext context) =>
        {
            using var activity = _activitySource.StartActivity("Picea.Abies.Kestrel.ServePage");
            activity?.SetTag("abies.program", typeof(TProgram).Name);
            activity?.SetTag("abies.path", path);
            activity?.SetTag("abies.renderMode", mode.GetType().Name);

            // Parse the request URL for route-aware rendering
            var requestUrl = Url.FromUri(new Uri(
                $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}"));

            // Render the full HTML page using the pure computation
            var html = Page.Render<TProgram, TModel, TArgument>(
                mode, argument, initialUrl: requestUrl);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Results.Content(html, "text/html; charset=utf-8").ExecuteAsync(context);
        };

        // Catch-all routes use MapFallback for lowest priority — lets static
        // files middleware serve _framework/*, abies.js, etc. first.
        // Specific routes use MapGet for normal priority.
        if (path.Contains("**"))
            endpoints.MapFallback(handler);
        else
            endpoints.MapGet(path, handler);
    }

    /// <summary>
    /// Maps the WebSocket endpoint that handles interactive server sessions.
    /// Each WebSocket connection gets its own Session with isolated state.
    /// </summary>
    private static void MapWebSocketEndpoint<TProgram, TModel, TArgument>(
        IEndpointRouteBuilder endpoints,
        string wsPath,
        Interpreter<Command, Message> interpreter,
        TArgument argument,
        JsonTypeInfo<TModel>? debuggerModelJsonTypeInfo)
        where TProgram : Program<TModel, TArgument>
    {
        endpoints.Map(wsPath, async (HttpContext context) =>
        {
            using var activity = _activitySource.StartActivity("Picea.Abies.Kestrel.WebSocketSession");
            activity?.SetTag("abies.program", typeof(TProgram).Name);
            activity?.SetTag("abies.wsPath", wsPath);

            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // Accept the WebSocket upgrade
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            // Create the transport adapter
            using var transport = new WebSocketTransport(webSocket);

            // Parse the initial URL from the WebSocket connection.
            // The JS client appends the current page path as a ?url= query
            // parameter on the WebSocket URL (e.g., /_abies/ws?url=%2Fregister).
            // This is the most reliable method since:
            //   - Origin header has no path (only scheme+host)
            //   - Referer header is not always sent on WebSocket upgrades
            //   - Query parameters are always available on the upgrade request
            Url? initialUrl = null;
            var urlParam = context.Request.Query["url"].FirstOrDefault();
            if (urlParam is not null)
            {
                initialUrl = Navigation.ParseUrl(urlParam);
            }
            else if (context.Request.Headers.TryGetValue("Referer", out var referer) &&
                     Uri.TryCreate(referer.ToString(), UriKind.Absolute, out var refererUri))
            {
                initialUrl = Url.FromUri(refererUri);
            }

            activity?.SetTag("abies.initialUrl", initialUrl?.ToString());

            // Start a new session — each connection gets its own Runtime
            using var session = await Session.Start<TProgram, TModel, TArgument>(
                sendPatches: transport.CreateSendPatches(),
                receiveEvent: transport.CreateReceiveEvent(),
                interpreter: interpreter,
                sendText: transport.CreateSendText(),
                argument: argument,
                initialUrl: initialUrl,
                debuggerModelJsonTypeInfo);

            // Run the event loop until the client disconnects
            await session.RunEventLoop(context.RequestAborted);

            // Graceful close
            await transport.CloseAsync();

            activity?.SetStatus(ActivityStatusCode.Ok);
        });
    }

    /// <summary>
    /// Default no-op interpreter: returns empty message arrays for all commands.
    /// </summary>
    private static ValueTask<Result<Message[], PipelineError>> NoOpInterpreter(Command _) =>
        new(Result<Message[], PipelineError>.Ok([]));

    /// <summary>
    /// Serves the Abies static files (e.g., <c>abies-server.js</c>) from the
    /// <c>Picea.Abies.Server.Kestrel</c> assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses a composite resolution strategy that always includes embedded
    /// resources as a fallback:
    /// <list type="number">
    ///   <item>
    ///     <b>Physical files (development)</b>: When a <c>wwwroot/</c> directory
    ///     exists next to the assembly DLL, physical files are checked first.
    ///     This path is populated by project references via
    ///     <c>CopyToOutputDirectory</c>, enabling hot-reload during development.
    ///   </item>
    ///   <item>
    ///     <b>Embedded resources (always available)</b>: The
    ///     <see cref="ManifestEmbeddedFileProvider"/> serves files embedded inside
    ///     the DLL itself. This ensures the JS is always available regardless of
    ///     how the consumer references the package.
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// A <see cref="CompositeFileProvider"/> is used when physical files are
    /// present so that both sources are tried. This is critical for NuGet
    /// consumers whose own <c>wwwroot/</c> (e.g., <c>site.css</c>) is copied
    /// to the output directory — <c>Directory.Exists</c> returns <c>true</c>,
    /// but the Abies JS files only exist in the embedded resources.
    /// </para>
    /// <para>
    /// Files are served without a path prefix, matching the script references
    /// in <see cref="Page"/>:
    /// </para>
    /// <code>
    ///   &lt;script src="/_abies/abies-server.js" ...&gt;
    /// </code>
    /// </remarks>
    /// <param name="app">The application builder to add the static files middleware to.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseAbiesStaticFiles(this IApplicationBuilder app)
    {
        var assembly = typeof(Endpoints).Assembly;
        var contentRoot = Path.GetDirectoryName(assembly.Location)!;
        var wwwrootPath = Path.Combine(contentRoot, "wwwroot");

        // Embedded resources are always available — the DLL contains abies-server.js
        var embeddedProvider = new ManifestEmbeddedFileProvider(assembly, "wwwroot");

        // Composite strategy: physical files first (dev hot-reload), embedded fallback (NuGet).
        // A simple Directory.Exists check is insufficient because NuGet consumers may have
        // their own wwwroot/ in the output directory (e.g., site.css from the template)
        // without the Abies JS files. CompositeFileProvider ensures both are checked.
        IFileProvider fileProvider;
        if (Directory.Exists(wwwrootPath))
        {
            // CodeQL: physicalProvider disposal is handled via the ApplicationStopping
            // callback below. Using 'using' would dispose it immediately after middleware
            // setup, which is incorrect — file providers must live for the app's lifetime.
            var physicalProvider = new PhysicalFileProvider(wwwrootPath); // lgtm[cs/local-not-disposed]

            // PhysicalFileProvider owns a file watcher — register for disposal on app shutdown.
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(
                static state => ((PhysicalFileProvider)state!).Dispose(),
                physicalProvider);

            fileProvider = new CompositeFileProvider(
                physicalProvider,   // Development: project reference copies to output
                embeddedProvider);  // Fallback: embedded resources (always present)
        }
        else
        {
            fileProvider = embeddedProvider; // NuGet-only: no physical wwwroot at all
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            RequestPath = ""
        });

        return app;
    }

    /// <summary>
    /// Serves the WASM application files (e.g., <c>_framework/dotnet.js</c>) from the
    /// published AppBundle directory of a WASM project.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enables the <see cref="RenderMode.InteractiveWasm"/> hosting model where:
    /// <list type="number">
    ///   <item>The server renders the initial HTML page (fast first paint)</item>
    ///   <item>The browser downloads the WASM bundle from <c>/_framework/</c></item>
    ///   <item>The .NET WASM runtime boots and the MVU loop starts client-side</item>
    /// </list>
    /// </para>
    /// <para>
    /// The <paramref name="appBundlePath"/> should point to the directory containing
    /// <c>_framework/</c> and <c>abies.js</c> — typically the output of
    /// <c>dotnet publish</c> for a browser-wasm project.
    /// </para>
    /// <example>
    /// <code>
    /// app.UseAbiesWasmFiles(
    ///     Path.Combine("..", "Picea.Abies.Counter.Wasm", "bin", "Release",
    ///         "net10.0", "browser-wasm", "publish", "wwwroot"));
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="app">The application builder.</param>
    /// <param name="appBundlePath">
    /// Absolute or relative path to the WASM AppBundle directory.
    /// Must contain <c>_framework/dotnet.js</c> and other WASM runtime files.
    /// </param>
    /// <returns>The application builder for chaining.</returns>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when <paramref name="appBundlePath"/> does not exist.
    /// </exception>
    public static IApplicationBuilder UseAbiesWasmFiles(
        this IApplicationBuilder app,
        string appBundlePath)
    {
        var fullPath = Path.GetFullPath(appBundlePath);

        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException(
                $"WASM AppBundle directory not found: {fullPath}. " +
                "Ensure the WASM project has been published before starting the server.");

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(fullPath),
            RequestPath = "",
            // The WASM AppBundle contains files with non-standard extensions
            // (.dat, .blat, .symbols) that the default content type provider
            // doesn't recognize. ServeUnknownFileTypes ensures all files are
            // served, using application/octet-stream as the fallback MIME type.
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream"
        });

        return app;
    }
}
