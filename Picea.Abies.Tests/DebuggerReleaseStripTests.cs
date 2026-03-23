using System.Text.RegularExpressions;
using Picea.Abies.Tests.Debugger;

namespace Picea.Abies.Tests;

public sealed class DebuggerReleaseStripTests
{
    [Test]
    public async Task ReleaseAssemblyDoesNotContainDebuggerSymbols()
    {
        var publishDirectory = await PublishOutputProbe.PublishReleaseProject("Picea.Abies/Picea.Abies.csproj");
        var releaseAssemblyPath = Path.Combine(publishDirectory, "Picea.Abies.dll");

        await Assert.That(File.Exists(releaseAssemblyPath)).IsTrue();

        var containsDebuggerNamespaceMetadata = PublishOutputProbe.FileContainsUtf8Token(
            releaseAssemblyPath,
            "Picea.Abies.Debugger");

        await Assert.That(containsDebuggerNamespaceMetadata).IsFalse();
    }

    [Test]
    public async Task DebuggerCompilationConditionalOnDebugFlag()
    {
        var repoRoot = FindRepoRoot();
        var runtimeCsPath = Path.Combine(repoRoot, "Picea.Abies", "Runtime.cs");
        var handlerRegistryCsPath = Path.Combine(repoRoot, "Picea.Abies", "HandlerRegistry.cs");

        var debugGuardFound = 0;

        if (File.Exists(runtimeCsPath))
        {
            var runtimeContent = File.ReadAllText(runtimeCsPath);
            if (Regex.IsMatch(runtimeContent, @"#if\s+DEBUG.*?UseDebugger.*?#endif", RegexOptions.Singleline))
            {
                debugGuardFound++;
            }
            else if (runtimeContent.Contains("UseDebugger", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Runtime.cs contains UseDebugger() but it is not guarded by #if DEBUG.");
            }
        }

        if (File.Exists(handlerRegistryCsPath))
        {
            var handlerContent = File.ReadAllText(handlerRegistryCsPath);
            if (Regex.IsMatch(handlerContent, @"#if\s+DEBUG.*?(UseDebugger|CreateMessage|Debugger).*?#endif", RegexOptions.Singleline))
            {
                debugGuardFound++;
            }
            else if (handlerContent.Contains("CreateMessage", StringComparison.Ordinal)
                     && handlerContent.Contains("Debugger", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("HandlerRegistry.cs contains debugger hooks but they are not guarded by #if DEBUG.");
            }
        }

        await Assert.That(debugGuardFound).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ReleaseAssemblyNoDebuggerReferencesInJavaScript()
    {
        var repoRoot = FindRepoRoot();
        var abiesJsPath = Path.Combine(repoRoot, "Picea.Abies.Browser", "wwwroot", "abies.js");
        await Assert.That(File.Exists(abiesJsPath)).IsTrue();

        var jsContent = File.ReadAllText(abiesJsPath);

        var hasTimelineMount = Regex.IsMatch(jsContent, @"abies-debugger-timeline|timeline.*mount|debugger.*init", RegexOptions.IgnoreCase);
        var hasReplayLogic = Regex.IsMatch(jsContent, @"jump-to-entry|step-forward|step-back|replay.*dispatch", RegexOptions.IgnoreCase);

        await Assert.That(hasTimelineMount).IsFalse();
        await Assert.That(hasReplayLogic).IsFalse();
    }

    [Test]
    public async Task ReleaseAssemblyContainsCoreAbiesFramework()
    {
        var publishDirectory = await PublishOutputProbe.PublishReleaseProject("Picea.Abies/Picea.Abies.csproj");
        var releaseAssemblyPath = Path.Combine(publishDirectory, "Picea.Abies.dll");

        await Assert.That(File.Exists(releaseAssemblyPath)).IsTrue();

        var assembly = System.Reflection.Assembly.LoadFrom(releaseAssemblyPath);

        var allTypes = assembly.GetTypes();
        var coreTypes = allTypes.Where(t =>
            t.FullName is not null
            && t.FullName.StartsWith("Picea.Abies", StringComparison.Ordinal)
            && !t.FullName.StartsWith("Picea.Abies.Debugger", StringComparison.Ordinal)
        ).ToList();

        await Assert.That(coreTypes.Count).IsGreaterThan(0);

        var hasRuntime = coreTypes.Any(t => t.Name.StartsWith("Runtime", StringComparison.Ordinal));
        var hasDocument = coreTypes.Any(t => t.Name == "Document");
        var hasNode = coreTypes.Any(t => t.Name == "Node");

        await Assert.That(hasRuntime && (hasDocument || hasNode)).IsTrue();
    }

    [Test]
    public async Task DebuggerJsNotIncludedInReleasePublishOutput()
    {
        var repoRoot = FindRepoRoot();
        var publishedDirs = new[]
        {
            Path.Combine(repoRoot, "Picea.Abies.Counter.Wasm", "bin", "Release", "net10.0", "browser-wasm", "AppBundle"),
            Path.Combine(repoRoot, "Picea.Abies.Counter.Wasm", "bin", "Release", "net10.0", "publish", "wwwroot")
        };

        var foundDebuggerJs = publishedDirs
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.GetFiles(directory, "debugger.js", SearchOption.AllDirectories))
            .ToList();

        await Assert.That(foundDebuggerJs).IsEmpty();
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Picea.Abies.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test base directory.");
    }
}
