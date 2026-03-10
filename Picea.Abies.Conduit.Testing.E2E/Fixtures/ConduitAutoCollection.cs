// =============================================================================
// ConduitAutoCollection — xUnit Collection for InteractiveAuto E2E Tests
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;

namespace Picea.Abies.Conduit.Testing.E2E;

[CollectionDefinition("ConduitAuto")]
public sealed class ConduitAutoCollection : ICollectionFixture<ConduitAutoFixture>;
