// =============================================================================
// main.js — WASM Bootstrap for Abies Counter
// =============================================================================
// The .NET WASM runtime exports a builder object from dotnet.js.
// This script imports it and calls .run() to start the C# Main() method.
//
// Boot chain:
//   1. index.html loads this script as <script type="module" src="./main.js">
//   2. This script imports { dotnet } from dotnet.js
//   3. dotnet.run() → dotnet.create() → download assets → start runtime
//   4. C# Main() runs → Picea.Abies.Browser.Runtime.Run<>()
//   5. Runtime.Run() calls JSHost.ImportAsync("Abies", "../abies.js")
//   6. abies.js is loaded, DOM mutation callbacks are wired
//   7. MVU loop starts, initial render paints the UI
// =============================================================================

import { dotnet } from './_framework/dotnet.js';

await dotnet.run();
