namespace Picea.Abies.Tests.Debugger;

public sealed class ReleaseStripDebuggerArtifactsTests
{
    [Test]
    public async Task ReleasePublish_StripsDebuggerTypeMetadata_WhenDebuggerOptInIsDisabled()
    {
        const string debuggerContractTypeName = "Picea.Abies.Debugger.RuntimeReplayProbe";

        var debugAssemblyType = typeof(Runtime<,,>).Assembly.GetType(debuggerContractTypeName, throwOnError: false);
        if (debugAssemblyType is null)
        {
            throw new InvalidOperationException(
                $"Debugger contract type is missing in Debug build: {debuggerContractTypeName}. " +
                "Issue 160 expects a debugger surface in Debug that is stripped from Release artifacts."
            );
        }

        var publishDirectory = await PublishOutputProbe.PublishReleaseProject("Picea.Abies/Picea.Abies.csproj");
        var releaseAssemblyPath = Path.Combine(publishDirectory, "Picea.Abies.dll");

        await Assert.That(File.Exists(releaseAssemblyPath)).IsTrue();

        var containsDebuggerMetadata = PublishOutputProbe.FileContainsUtf8Token(releaseAssemblyPath, debuggerContractTypeName);
        await Assert.That(containsDebuggerMetadata).IsFalse();
    }
}
