// =============================================================================
// ConduitCollection — xUnit Collection for Shared E2E Infrastructure
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;

namespace Picea.Abies.Conduit.Testing.E2E;

[CollectionDefinition("Conduit")]
public sealed class ConduitCollection : ICollectionFixture<ConduitAppFixture>;
