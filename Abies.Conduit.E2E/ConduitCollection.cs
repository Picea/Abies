using Xunit;

namespace Abies.Conduit.E2E;

[CollectionDefinition("Conduit collection")]
public class ConduitCollection : ICollectionFixture<ConduitFixture>
{
}
