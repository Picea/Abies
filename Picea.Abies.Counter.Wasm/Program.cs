// =============================================================================
// Counter WASM Bootstrap — Program.cs
// =============================================================================
// Entry point for the Abies Counter WebAssembly application.
//
// Picea.Abies.Browser.Runtime.Run handles all browser-specific wiring:
//   - Loading abies.js
//   - Setting up event delegation and navigation
//   - Creating the binary batch writer and Apply delegate
//   - Starting the MVU runtime
//   - Keeping the WASM process alive
// =============================================================================

using Picea.Abies.Counter;
using Picea;

await Picea.Abies.Browser.Runtime.Run<CounterProgram, CounterModel, Unit>();
