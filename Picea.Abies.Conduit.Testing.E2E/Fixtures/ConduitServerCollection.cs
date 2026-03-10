// =============================================================================
// ConduitServerCollection — xUnit Collection for InteractiveServer E2E Tests
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;

namespace Picea.Abies.Conduit.Testing.E2E;

[CollectionDefinition("ConduitServer")]
public sealed class ConduitServerCollection : ICollectionFixture<ConduitServerFixture>;
