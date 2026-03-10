// =============================================================================
// ConduitStaticCollection — xUnit Collection for Static E2E Tests
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;

namespace Picea.Abies.Conduit.Testing.E2E;

[CollectionDefinition("ConduitStatic")]
public sealed class ConduitStaticCollection : ICollectionFixture<ConduitStaticFixture>;
