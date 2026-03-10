// =============================================================================
// CounterAutoCollection — xUnit Collection for InteractiveAuto E2E Tests
// =============================================================================

using Picea.Abies.Counter.Testing.E2E.Fixtures;

namespace Picea.Abies.Counter.Testing.E2E;

[CollectionDefinition("CounterAuto")]
public sealed class CounterAutoCollection : ICollectionFixture<CounterAutoFixture>;
