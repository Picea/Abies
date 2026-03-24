// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;

namespace Picea.Abies.Browser.Tests;

public sealed class DebuggerJavaScriptAdapterContractTests
{
    [Test]
    public async Task DebuggerJsMountAdapter_DoesNotBuildUiDom()
    {
        var script = ReadDebuggerScript();

        var domConstructionPatterns = new[]
        {
            "document.createElement(",
            "appendChild(",
            "innerHTML =",
            "play-button",
            "pause-button",
            "step-forward-button",
            "step-back-button",
            "jump-input",
            "control-bar",
            "message-log",
            "timeline-inspector"
        };

        var found = domConstructionPatterns
            .Where(pattern => script.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        await Assert.That(found).IsEmpty();
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
    public async Task DebuggerJsMountAdapter_DoesNotContainKeyboardOrReplayDomainLogic()
    {
        var script = ReadDebuggerScript();

        var disallowedPatterns = new[]
        {
            @"case\s+'ArrowRight'",
            @"case\s+'ArrowLeft'",
            @"case\s+'Escape'",
            @"case\s+'j'",
            @"jump-to-entry",
            @"step-forward",
            @"step-back",
            @"clear-timeline",
            @"\bplay\b",
            @"\bpause\b"
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

        await Assert.That(script).Contains("initializeDebuggerAdapter");
        await Assert.That(script).Contains("bootstrapIntentTransport");
        await Assert.That(script).Contains("forwardIntentToRuntimeBridge");
        await Assert.That(script).Contains("parsePayload");
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
