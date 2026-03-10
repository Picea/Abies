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
// =============================================================================

using Picea.Abies.Conduit.App;
using Picea;

await Picea.Abies.Browser.Runtime.Run<ConduitProgram, Model, Unit>(
    interpreter: ConduitInterpreter.Interpret);
