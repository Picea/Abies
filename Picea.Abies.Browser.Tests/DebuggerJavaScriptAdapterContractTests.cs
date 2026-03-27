// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;

namespace Picea.Abies.Browser.Tests;

public sealed class DebuggerJavaScriptAdapterContractTests
{
    [Test]
    public async Task DebuggerJsMountAdapter_CreatesOnlyMountPointContainer()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("export function mountDebugger");
        await Assert.That(script).Contains("document.createElement('div')");
        await Assert.That(script).Contains("mountPoint.id = MountPointId");
        await Assert.That(script).Contains("document.body.appendChild(mountPoint)");
        await Assert.That(script).Contains("ensureDebuggerShellVisible");
        await Assert.That(script).Contains("ensureDebuggerPanel");
        await Assert.That(script).Contains("data-abies-debugger-shell");
        await Assert.That(script).Contains("data-abies-debugger-panel");
        await Assert.That(script).Contains("data-abies-debugger-intent");
    }

    [Test]
    public async Task DebuggerJsMountAdapter_UsesIntentTransportContract()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("abies-debugger-timeline");
        await Assert.That(script).Contains("abiesDebuggerAdapterInitialized");
        await Assert.That(script).Contains("abies:debugger:intent");
        await Assert.That(script).Contains("abies:debugger:message-dispatched");
        await Assert.That(script).Contains("data-abies-debugger-intent");
        await Assert.That(script).Contains("data-abies-debugger-payload");
    }

    [Test]
    public async Task DebuggerJsMountAdapter_DoesNotContainDocumentKeyboardShortcutSwitches()
    {
        var script = ReadDebuggerScript();

        var disallowedPatterns = new[]
        {
            @"case\s+'ArrowRight'",
            @"case\s+'ArrowLeft'",
            @"case\s+'Escape'",
            @"case\s+'j'",
            "addEventListener\\(\\s*['\\\"]keydown['\\\"]"
        };

        var found = disallowedPatterns
            .Where(pattern => Regex.IsMatch(script, pattern, RegexOptions.IgnoreCase))
            .ToArray();

        await Assert.That(found).IsEmpty();
    }

    [Test]
    public async Task DebuggerJsMountAdapter_ExportsAdapterHelpersForTests()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("mountDebugger");
        await Assert.That(script).Contains("initializeDebuggerAdapter");
        await Assert.That(script).Contains("bootstrapIntentTransport");
        await Assert.That(script).Contains("forwardIntentToRuntimeBridge");
        await Assert.That(script).Contains("parsePayload");
    }

    [Test]
    public async Task DebuggerJsRuntimeBridge_AwaitsAsyncBridgeResponses()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("await Promise.resolve(runtimeBridge(message.type, entryId))");
        await Assert.That(script).Contains("void invokeRuntimeBridge(message, mountPoint)");
    }

    [Test]
    public async Task BrowserRuntime_DebuggerBootstrap_WiresRuntimeBridgeWhenEnabled()
    {
        var runtimeScript = ReadBrowserRuntimeSource();

        await Assert.That(runtimeScript).Contains("Interop.SetRuntimeBridge(Interop.DispatchDebuggerMessage);");
        await Assert.That(runtimeScript).Contains("runtime.UseDebugger();");
    }

    private static string ReadDebuggerScript()
    {
        var repoRoot = FindRepoRoot();
        var debuggerScriptPath = Path.Combine(repoRoot, "Picea.Abies.Browser", "wwwroot", "debugger.js");

        if (!File.Exists(debuggerScriptPath))
        {
            throw new FileNotFoundException("Could not locate debugger.js for adapter contract tests.", debuggerScriptPath);
        }

        return File.ReadAllText(debuggerScriptPath);
    }

    private static string ReadBrowserRuntimeSource()
    {
        var repoRoot = FindRepoRoot();
        var runtimePath = Path.Combine(repoRoot, "Picea.Abies.Browser", "Runtime.cs");

        if (!File.Exists(runtimePath))
        {
            throw new FileNotFoundException("Could not locate browser Runtime.cs for debugger bootstrap tests.", runtimePath);
        }

        return File.ReadAllText(runtimePath);
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
