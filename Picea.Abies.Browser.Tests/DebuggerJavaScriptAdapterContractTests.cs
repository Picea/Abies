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
        await Assert.That(script).Contains("els.panel.contains(active)");
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
        await Assert.That(script).Contains("dataJson ?? ''");
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
    public async Task DebuggerJsSessionExport_IncludesRuntimeMetadataInPayload()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("function buildExportSessionPayload()");
        await Assert.That(script).Contains("schemaVersion: SESSION_SCHEMA_VERSION");
        await Assert.That(script).Contains("runtime,");
        await Assert.That(script).Contains("appName");
        await Assert.That(script).Contains("appVersion");
        await Assert.That(script).Contains("timelineEntries: localTimeline");
    }

    [Test]
    public async Task DebuggerJsSessionImport_HappyPathAppliesImportedSession()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("function importSession(file)");
        await Assert.That(script).Contains("buildRuntimeImportPayload(session)");
        await Assert.That(script).Contains("invokeRuntimeBridge('import-session', -1, bridgePayload)");
        await Assert.That(script).Contains("applyImportedSession(session)");
        await Assert.That(script).Contains("Session imported:");
    }

    [Test]
    public async Task DebuggerJsSessionImport_RejectsAppVersionMismatch()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("session.runtime.appName !== currentRuntime.appName");
        await Assert.That(script).Contains("session.runtime.appVersion !== currentRuntime.appVersion");
        await Assert.That(script).Contains("Import rejected: session");
    }

    [Test]
    public async Task DebuggerJsSessionImport_FallsBackToReadOnlyViewMode_WhenNoRuntimeBridgeExists()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("detachedImportedSession");
        await Assert.That(script).Contains("canControlLiveRuntime");
        await Assert.That(script).Contains("!canControlLiveRuntime");
        await Assert.That(script).Contains("read-only view mode");
        await Assert.That(script).Contains("showDetachedSessionNotice");
        await Assert.That(script).Contains("if (runtimeBridge)");
    }

    [Test]
    public async Task DebuggerJsRuntimeMetadata_IsConsumedFromBridgeResponses()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("function syncRuntimeMetadata(response)");
        await Assert.That(script).Contains("response?.appName");
        await Assert.That(script).Contains("response?.appVersion");
    }

    [Test]
    public async Task DebuggerJsSessionImport_HandlesMalformedJsonGracefully()
    {
        var script = ReadDebuggerScript();

        await Assert.That(script).Contains("const payload = JSON.parse(raw);");
        await Assert.That(script).Contains("catch");
        await Assert.That(script).Contains("Import failed: file is not valid debugger session JSON.");
        await Assert.That(script).Contains("showNotice");
    }

    [Test]
    public async Task BrowserRuntime_DebuggerBootstrap_WiresRuntimeBridgeWhenEnabled()
    {
        var runtimeScript = ReadBrowserRuntimeSource();
        var interopSource = ReadBrowserInteropSource();

        await Assert.That(runtimeScript).Contains("Interop.SetRuntimeBridge(Interop.DispatchDebuggerMessage);");
        await Assert.That(runtimeScript).Contains("TimelineChanged += Interop.NotifyTimelineChanged");
        await Assert.That(runtimeScript).Contains("runtime.UseDebugger();");
        await Assert.That(interopSource).Contains("JSImport(\"notifyTimelineChanged\", \"AbiesDebugger\")");
        await Assert.That(interopSource).Contains("Func<string, int, string, string>");
    }

    [Test]
    public async Task BrowserInterop_DebuggerDispatch_ReturnsJsonProtocolResponses()
    {
        var interopSource = ReadBrowserInteropSource();
        var serverInteropSource = ReadServerInteropSource();

        await Assert.That(interopSource).Contains("DispatchDebuggerMessage");
        await Assert.That(interopSource).Contains("JsonSerializer.Serialize(response");
        await Assert.That(interopSource).Contains("DebuggerAdapterJsonContext.Default.DebuggerAdapterResponse");
        await Assert.That(interopSource.Contains("const char separator = '|'", StringComparison.Ordinal)).IsFalse();
        await Assert.That(serverInteropSource).Contains("resolve(JSON.stringify(response))");
        await Assert.That(serverInteropSource).Contains("status: \"unavailable\"");
        await Assert.That(serverInteropSource).Contains("mod.setRuntimeBridge((type, entryId, dataJson) => sendDebuggerCommand(type, entryId, dataJson))");
        await Assert.That(serverInteropSource).Contains("refreshDebuggerState()");
        await Assert.That(serverInteropSource).Contains("mod.notifyTimelineChanged()");
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

    private static string ReadBrowserInteropSource()
    {
        var repoRoot = FindRepoRoot();
        var interopPath = Path.Combine(repoRoot, "Picea.Abies.Browser", "Interop.cs");

        if (!File.Exists(interopPath))
        {
            throw new FileNotFoundException("Could not locate browser Interop.cs for debugger bridge contract tests.", interopPath);
        }

        return File.ReadAllText(interopPath);
    }

    private static string ReadServerInteropSource()
    {
        var repoRoot = FindRepoRoot();
        var serverInteropPath = Path.Combine(repoRoot, "Picea.Abies.Server.Kestrel", "wwwroot", "_abies", "abies-server.js");

        if (!File.Exists(serverInteropPath))
        {
            throw new FileNotFoundException("Could not locate abies-server.js for debugger bridge contract tests.", serverInteropPath);
        }

        return File.ReadAllText(serverInteropPath);
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
