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

using Picea;
using Picea.Abies.Counter;

#if DEBUG
var debugUiOptOut = string.Equals(
	Environment.GetEnvironmentVariable("ABIES_DEBUG_UI"),
	"0",
	StringComparison.OrdinalIgnoreCase);

Picea.Abies.Debugger.DebuggerConfiguration.ConfigureDebugger(
	new Picea.Abies.Debugger.DebuggerOptions { Enabled = !debugUiOptOut });
#endif

await Picea.Abies.Browser.Runtime.Run<CounterProgram, CounterModel, Unit>();
