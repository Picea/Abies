// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Text.RegularExpressions;

namespace Picea.Abies.Tests;

/// <summary>
/// Validates that the debugger code is stripped in Release builds via #if DEBUG guards.
/// Purpose: Verify that compiled Release binaries contain no debugger symbols and no
/// UseDebugger hooks, and that JS debugger code is not included in the Release bundle.
/// 
/// NOTE: These tests validate the compile-time stripping mechanism. Some may pass today
/// (release binary has no symbols yet) or fail (guards not yet in place). The critical ones
/// will fail once implementation starts without proper #if DEBUG guards.
/// </summary>
public class DebuggerReleaseStripTests
{
    /// <summary>
    /// Test 4a: Verify that a compiled Release build of Picea.Abies.dll does NOT contain
    /// any types in the Picea.Abies.Debugger namespace (stripped by #if DEBUG guards).
    /// 
    /// Validates the seam: C# namespace stripping in Release configuration.
    /// TODAY: Fails if debugger namespace exists without #if DEBUG guard. Passes if
    /// namespace doesn't exist yet or is properly guarded.
    /// </summary>
    [Test]
    public void ReleaseAssemblyDoesNotContainDebuggerSymbols()
    {
        // Arrange: Load the Picea.Abies assembly from the Release output path
        // This test assumes Release build has been published
        var releaseDllPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "Picea.Abies", "bin", "Release", "net10.0", "Picea.Abies.dll"
        );

        if (!File.Exists(releaseDllPath))
        {
            // Skip if Release build not available (can build-skip in CI if needed)
            Assert.Skip($"Release DLL not found at {releaseDllPath}. Run 'dotnet publish -c Release' first.");
        }

        var assembly = Assembly.LoadFrom(releaseDllPath);

        // Act: Get all types in the assembly
        var allTypes = assembly.GetTypes();

        // Assert: No type should be in the Picea.Abies.Debugger namespace
        var debuggerTypes = allTypes.Where(t =>
            t.FullName != null && t.FullName.StartsWith("Picea.Abies.Debugger")
        ).ToList();

        Assert.That(debuggerTypes, Is.Empty,
            "Release assembly should not contain any Picea.Abies.Debugger types. " +
            "Ensure #if DEBUG guard wraps the entire Debugger namespace. " +
            $"Found types: {string.Join(", ", debuggerTypes.Select(t => t.FullName))}");
    }

    /// <summary>
    /// Test 4b: Verify that source code in Picea.Abies (Program.cs, HandlerRegistry.cs)
    /// contains #if DEBUG guards around UseDebugger() hooks and debugger setup.
    /// 
    /// Validates the seam: Compile-time guard presence in source.
    /// TODAY: Fails if UseDebugger() hooks exist without #if DEBUG guards.
    /// </summary>
    [Test]
    public void DebuggerCompilationConditionalOnDebugFlag()
    {
        // Arrange
        var programCsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "Picea.Abies", "Program.cs"
        );
        
        var handlerRegistryCsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "Picea.Abies", "HandlerRegistry.cs"
        );

        // Act: Read source files
        var debugGuardFound = 0;
        
        if (File.Exists(programCsPath))
        {
            var programContent = File.ReadAllText(programCsPath);
            if (Regex.IsMatch(programContent, @"#if\s+DEBUG.*?UseDebugger.*?#endif", RegexOptions.Singleline))
            {
                debugGuardFound++;
            }
            else if (programContent.Contains("UseDebugger"))
            {
                Assert.Fail("Program.cs contains UseDebugger() but is NOT guarded by #if DEBUG");
            }
        }

        if (File.Exists(handlerRegistryCsPath))
        {
            var handlerContent = File.ReadAllText(handlerRegistryCsPath);
            if (Regex.IsMatch(handlerContent, @"#if\s+DEBUG.*?(UseDebugger|CreateMessage|Debugger).*?#endif", RegexOptions.Singleline))
            {
                debugGuardFound++;
            }
            else if (handlerContent.Contains("CreateMessage") && handlerContent.Contains("Debugger"))
            {
                Assert.Fail("HandlerRegistry.cs contains debugger hooks but is NOT guarded by #if DEBUG");
            }
        }

        // Assert: At least 1 guard should exist for documentation, but ideally 2+
        // (This is a soft assertion to allow early-stage implementation)
        Assert.That(debugGuardFound, Is.GreaterThanOrEqualTo(1),
            "Should find at least 1 #if DEBUG guard for debugger setup in Program.cs or HandlerRegistry.cs");
    }

    /// <summary>
    /// Test 4c: Verify that the Release build of abies.js core file does NOT contain
    /// references to "Debugger" or "Timeline" (debugger code should not be present).
    /// 
    /// Validates the seam: JS debugger code isolation (not in core abies.js).
    /// TODAY: Passes (vacuously true, debugger.js not created yet). Documents expectation
    /// that release abies.js should be clean of debugger logic.
    /// </summary>
    [Test]
    public void ReleaseAssemblyNoDebuggerReferencesInJavaScript()
    {
        // Arrange
        var abiesJsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "Picea.Abies.Browser", "wwwroot", "abies.js"
        );

        if (!File.Exists(abiesJsPath))
        {
            Assert.Skip($"abies.js not found at {abiesJsPath}");
        }

        // Act: Read the JS file content
        var jsContent = File.ReadAllText(abiesJsPath);

        // Assert: Should NOT contain debugger timeline logic references
        // Note: Generic term "debug" in comments is OK; we're looking for actual logic
        var hasTimelineMount = Regex.IsMatch(jsContent, @"abies-debugger-timeline|timeline.*mount|debugger.*init", RegexOptions.IgnoreCase);
        var hasReplayLogic = Regex.IsMatch(jsContent, @"jump-to-entry|step-forward|step-back|replay.*dispatch", RegexOptions.IgnoreCase);

        Assert.That(hasTimelineMount, Is.False,
            "abies.js should NOT contain timeline DOM mount logic. Move to separate debugger.js.");
        Assert.That(hasReplayLogic, Is.False,
            "abies.js should NOT contain replay state transition logic. Move to separate debugger.js.");
    }

    /// <summary>
    /// Test 4d: Verify that the Picea.Abies namespace itself (non-debugger code) is still
    /// present and functional in Release builds (negative test for over-stripping).
    /// 
    /// Validates the seam: Selective stripping (only debugger, not core framework).
    /// TODAY: Passes (core framework always present).
    /// </summary>
    [Test]
    public void ReleaseAssemblyContainsCoreAbiesFramework()
    {
        // Arrange
        var releaseDllPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "Picea.Abies", "bin", "Release", "net10.0", "Picea.Abies.dll"
        );

        if (!File.Exists(releaseDllPath))
        {
            Assert.Skip($"Release DLL not found at {releaseDllPath}");
        }

        var assembly = Assembly.LoadFrom(releaseDllPath);

        // Act: Get all types in the Picea.Abies namespace (excluding Debugger)
        var allTypes = assembly.GetTypes();
        var coreTypes = allTypes.Where(t =>
            t.FullName != null && 
            t.FullName.StartsWith("Picea.Abies") &&
            !t.FullName.StartsWith("Picea.Abies.Debugger")
        ).ToList();

        // Assert: Core framework types should exist
        Assert.That(coreTypes.Count, Is.GreaterThan(0),
            "Release assembly should contain core Picea.Abies types (do not over-strip)");

        // Check for key expected types
        var hasRuntime = coreTypes.Any(t => t.Name == "Runtime");
        var hasDocument = coreTypes.Any(t => t.Name == "Document");
        var hasNode = coreTypes.Any(t => t.Name == "Node");

        Assert.That(hasRuntime && (hasDocument || hasNode), Is.True,
            "Release assembly should contain core framework types like Runtime, Document, Node");
    }

    /// <summary>
    /// Test 4e: Verify that debugger.js file (if present in source) is NOT copied to
    /// Release publish output for WASM projects.
    /// 
    /// Validates the seam: JS debugger file exclusion from Release bundle.
    /// TODAY: Passes (debugger.js not created yet). Documents expectation.
    /// </summary>
    [Test]
    public void DebuggerJsNotIncludedInReleasePublishOutput()
    {
        // Arrange: Check that debugger.js is NOT in the Release published bundle
        var publishedDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "Picea.Abies.Counter.Wasm", "bin", "Release", "net10.0", "browser-wasm", "AppBundle"
        );

        if (!Directory.Exists(publishedDir))
        {
            // Try alternative publish path
            publishedDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "Picea.Abies.Counter.Wasm", "bin", "Release", "net10.0", "publish", "wwwroot"
            );
        }

        if (!Directory.Exists(publishedDir))
        {
            Assert.Skip($"Published WASM output not found at expected paths. Run 'dotnet publish' first.");
        }

        // Act: Check for debugger.js in published output
        var debuggerJsPath = Path.Combine(publishedDir, "debugger.js");
        var hasDebuggerJs = File.Exists(debuggerJsPath);

        // Assert: debugger.js should NOT be in Release publish output
        Assert.That(hasDebuggerJs, Is.False,
            "debugger.js should not be included in Release publish output. " +
            "Ensure publish profile excludes debugger-only JS files.");
    }
}
