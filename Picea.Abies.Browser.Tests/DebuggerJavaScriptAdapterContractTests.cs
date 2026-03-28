// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Picea.Abies.Browser.Tests;

public sealed class DebuggerJavaScriptAdapterContractTests
{
    [Test]
    public async Task DebuggerJsMountAdapter_CreatesIdempotentMountPoint()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("export function mountDebugger");
        await Assert.That(script).Contains("document.createElement");
        await Assert.That(script).Contains("MOUNT_POINT_ID");
        await Assert.That(script).Contains("document.body.appendChild(mp)");
        await Assert.That(script).Contains("data-intent");
    }

    [Test]
    public async Task DebuggerJsMountAdapter_UsesTransportContract()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("abies-debugger-timeline");
        await Assert.That(script).Contains("abiesDebuggerAdapterInitialized");
        await Assert.That(script).Contains("abies:debugger:message-dispatched");
        await Assert.That(script).Contains("invokeRuntimeBridge");
        await Assert.That(script).Contains("JSON.parse");
    }

    [Test]
    public async Task DebuggerJsMountAdapter_IncludesKeyboardNavigation()
    {
        var script = ReadDebuggerScript();

        // v2 debugger owns keyboard shortcuts within the panel
        var requiredKeys = new[] { "ArrowRight", "ArrowLeft", "Escape", "Home", "End" };

        foreach (var key in requiredKeys)
        {
            await Assert.That(script).Contains(key);
        }

        await Assert.That(script).Contains("keydown");
    }

    [Test]
    public async Task DebuggerJsMountAdapter_ExportsRequiredFunctions()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("export function mountDebugger");
        await Assert.That(script).Contains("export function setRuntimeBridge");
    }

    [Test]
    public async Task DebuggerJsRuntimeBridge_UsesJsonProtocol()
    {
        var script = ReadDebuggerScript();

        // v2 bridge uses JSON protocol with Promise.resolve wrapping
        await Assert.That(script).Contains("await Promise.resolve(");
        await Assert.That(script).Contains("runtimeBridge(messageType");
        await Assert.That(script).Contains("JSON.parse(raw)");
    }

    [Test]
    public async Task DebuggerJsMountAdapter_ImplementsTimelineSynchronization()
    {
        var script = ReadDebuggerScript();

        // v2 uses lazy timeline sync: fetches get-timeline when size changes
        await Assert.That(script).Contains("get-timeline");
        await Assert.That(script).Contains("localTimeline");
        await Assert.That(script).Contains("timelineEntries");
        await Assert.That(script).Contains("timelineSize");
    }

    [Test]
    public async Task DebuggerJsMountAdapter_RendersAccessibleUI()
    {
        var script = ReadDebuggerScript();

        // ARIA attributes for accessibility
        await Assert.That(script).Contains("aria-live");
        await Assert.That(script).Contains("aria-label");
        await Assert.That(script).Contains("aria-selected");
        await Assert.That(script).Contains("role");
        await Assert.That(script).Contains("listbox");
    }

    [Test]
    public async Task DebuggerJsMountAdapter_DoesNotContainReplayDomainReferences()
    {
        var script = ReadDebuggerScript();

        // JS adapter should not embed C# replay/domain internals.
        var forbiddenTokens = new[]
        {
            "DebuggerMachine",
            "CaptureMessage",
            "StepForward(",
            "StepBackward(",
            "ClearTimeline(",
            "CurrentDebugger",
            "GenerateModelSnapshot"
        };

        foreach (var token in forbiddenTokens)
        {
            await Assert.That(script.Contains(token, StringComparison.Ordinal)).IsFalse();
        }
    }

    [Test]
    public async Task DebuggerJsMountAdapter_UsesBoundaryStatesForDisabledControls()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("atStart");
        await Assert.That(script).Contains("atEnd");
        await Assert.That(script).Contains("setDisabled");
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
