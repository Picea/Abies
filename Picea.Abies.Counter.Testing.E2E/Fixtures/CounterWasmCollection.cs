// =============================================================================
// CounterWasmCollection — xUnit Collection for Shared E2E Infrastructure
// =============================================================================
// Defines an xUnit test collection that shares a single CounterWasmFixture
// across all Counter E2E test classes. This ensures:
//   - One Kestrel server instance (InteractiveWasm mode)
//   - One WASM publish (via MSBuild target)
//   - One Playwright browser instance
//
// Without this, each test class would spin up its own server and browser.
// =============================================================================

using Picea.Abies.Counter.Testing.E2E.Fixtures;

namespace Picea.Abies.Counter.Testing.E2E;

[CollectionDefinition("CounterWasm")]
public sealed class CounterWasmCollection : ICollectionFixture<CounterWasmFixture>;
