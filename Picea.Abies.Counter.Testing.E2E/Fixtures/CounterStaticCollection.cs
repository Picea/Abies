// =============================================================================
// CounterStaticCollection — xUnit Collection for Static E2E Tests
// =============================================================================

using Picea.Abies.Counter.Testing.E2E.Fixtures;

namespace Picea.Abies.Counter.Testing.E2E;

[CollectionDefinition("CounterStatic")]
public sealed class CounterStaticCollection : ICollectionFixture<CounterStaticFixture>;
