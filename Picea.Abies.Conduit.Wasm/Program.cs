// =============================================================================
// Program.cs — Conduit WASM Bootstrap
// =============================================================================
// Entry point for the Conduit Blazor WebAssembly application.
//
// Picea.Abies.Browser.Runtime.Run handles all browser-specific wiring:
//   - Loading abies.js
//   - Setting up event delegation and navigation
//   - Creating the binary batch writer and Apply delegate
//   - Starting the MVU runtime
//   - Keeping the WASM process alive
//
// The interpreter converts ConduitCommands into HTTP API calls.
//
// API URL resolution:
//   The WASM app reads window.location.origin at startup so that the
//   interpreter can build absolute URLs for HttpClient (which requires
//   absolute URIs unless BaseAddress is set).  Because the hosting
//   server reverse-proxies /api/** to the backend, using the page
//   origin as the API base works for both development and production.
// =============================================================================

using Picea.Abies.Conduit.App;

// Import the Abies JS module early so we can read the browser origin.
// The subsequent import inside Runtime.Run is a cached no-op.
await Picea.Abies.Browser.Runtime.ImportModule();
var apiUrl = Picea.Abies.Browser.Runtime.GetOrigin();

await Picea.Abies.Browser.Runtime.Run<ConduitProgram, Model, string>(
    argument: apiUrl,
    interpreter: ConduitInterpreter.Interpret);
