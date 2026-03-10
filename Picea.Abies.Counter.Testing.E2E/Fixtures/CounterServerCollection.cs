// =============================================================================
// CounterServerCollection — xUnit Collection for InteractiveServer E2E Tests
// =============================================================================

using Picea.Abies.Counter.Testing.E2E.Fixtures;

namespace Picea.Abies.Counter.Testing.E2E;

[CollectionDefinition("CounterServer")]
public sealed class CounterServerCollection : ICollectionFixture<CounterServerFixture>;
