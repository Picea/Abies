// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;

namespace Picea.Abies.Browser.Tests;

/// <summary>
/// Validates that debugger code is properly stripped from Release builds.
/// Purpose: Verify that debugger logic is DEBUG-only and completely absent from Release binaries and JS bundles.
/// 
/// Release Strip Contract:
/// - C# Namespace: Picea.Abies.Debugger wrapped in #if DEBUG (not in Release IL)
/// - JS File: debugger.js excluded from Release wwwroot/
/// - Core JS: abies.js contains NO debugger references, mount hooks, or conditional debug checks
/// - Build target: SyncAbiesJs copies canonical abies.js to all consumers (must be Release-safe)
/// 
/// NOTE: These tests are expected to behave as follows:
/// - Tests 3a/3c: FAIL TODAY (file present in Release) → PASS TOMORROW (file excluded)
/// - Test 3b: PASSES TODAY (abies.js has no debugger code yet) → CONTINUES PASSING (never added)
/// 
/// Test Strategy: Artifact inspection
/// - Read wwwroot/ file system after Release publish
/// - I/O checks: file existence, text content scanning
/// - IL inspection: check if Picea.Abies.Debugger namespace present in assembly
/// </summary>
public class DebuggerReleaseStripTests
{
    private string _publishOutputDir = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "..", "..", "..", "Picea.Abies.Browser", "bin", "Release", "net10.0", "publish", "wwwroot"
    );

    /// <summary>
    /// Test 3a: The debugger.js file does NOT exist in the Release published wwwroot/ folder.
    /// Debugger UI module should be completely excluded from Release bundle.
    /// 
    /// Validates the seam: Build exclusion of debug-only JS module.
    /// 
    /// TODAY: FAILS - debugger.js is present (no exclusion rule implemented yet)
    /// TOMORROW: PASSES - once build target excludes debugger.js from Release publish
    /// </summary>
    [Test]
    public async Task ReleaseAssetCancel_DebuggerJSNotIncludedInReleaseBuild()
    {
        // Arrange
        var debuggerJsPath = Path.Combine(_publishOutputDir, "debugger.js");

        // Act & Assert
        await Assert.That(File.Exists(debuggerJsPath)).IsFalse();
    }

    /// <summary>
    /// Test 3b: The core abies.js file contains NO references to debugger-related identifiers.
    /// This ensures that release build of abies.js is free of debugger hooks, mounts, or conditional logic.
    /// 
    /// Validates the seam: Core JS module is clean and release-safe.
    /// 
    /// TODAY: PASSES VACUOUSLY (debugger mount logic not added to abies.js yet)
    /// TOMORROW: CONTINUES PASSING (abies.js never includes debugger code—only debugger.js does)
    /// </summary>
    [Test]
    public async Task ReleaseAbiesJS_ContainsNoDebuggerReferences()
    {
        // Arrange
        var abiesJsPath = Path.Combine(_publishOutputDir, "abies.js");

        // Act
        await Assert.That(File.Exists(abiesJsPath)).IsTrue();

        var abiesJsContent = File.ReadAllText(abiesJsPath);

        // Assert: Search for debugger-specific identifiers
        var debuggerPatterns = new[]
        {
            "abies-debugger",          // Mount point ID
            "debugger-timeline",       // Timeline element
            "DebuggerUI",              // Class name
            "debugger.js",             // Module reference
            "timeline-updated",        // C# response message type
            "jump-to-entry",           // Message type (should only be in debugger.js)
            "step-forward",            // Message type
            "UseDebugger",             // C# hook name
            "__abies.debugger"         // JS namespace
        };

        var foundPatterns = new List<string>();
        foreach (var pattern in debuggerPatterns)
        {
            if (abiesJsContent.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                foundPatterns.Add(pattern);
            }
        }

        await Assert.That(foundPatterns).IsEmpty();
    }

    /// <summary>
    /// Test 3c: The Release build of Picea.Abies.Browser assembly does NOT contain
    /// the Picea.Abies.Debugger namespace in its IL (Intermediate Language).
    /// This validates that C# debugger code was stripped via #if DEBUG preprocessor directives.
    /// 
    /// Validates the seam: C# build-time DEBUG stripping via preprocessor.
    /// 
    /// TODAY: FAILS - Picea.Abies.Debugger namespace present in IL (no #if DEBUG wrapping yet)
    /// TOMORROW: PASSES - once namespace wrapped in #if DEBUG and Release binary rebuild
    /// </summary>
    [Test]
    public async Task ReleaseBuildExcludesDebuggerCSharpNamespace_FromAssemblyIL()
    {
        // Arrange
        var browserDll = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "..", "..", "..", "Picea.Abies.Browser", "bin", "Release", "net10.0", "Picea.Abies.Browser.dll"
        );

        // Act
        await Assert.That(File.Exists(browserDll)).IsTrue();

        // Load assembly and scan types
        var asm = System.Reflection.Assembly.LoadFrom(browserDll);
        var debuggerTypes = asm.GetTypes()
            .Where(t => t.FullName?.StartsWith("Picea.Abies.Debugger") ?? false)
            .ToList();

        // Assert
        await Assert.That(debuggerTypes).IsEmpty();
    }

    /// <summary>
    /// Test 3d: The Release-published wwwroot/ folder contains ONLY expected files
    /// (abies.js and other core assets) and does NOT contain debugger.js or debug symbols.
    /// This is a comprehensive file inventory check.
    /// 
    /// Validates the seam: Release artifact cleanliness.
    /// 
    /// TODAY: FAILS (debugger.js present)
    /// TOMORROW: PASSES (debugger.js excluded)
    /// </summary>
    [Test]
    public async Task ReleaseBuildExcludesDebuggerModule_FromPublishOutput()
    {
        // Arrange
        var forbiddenFiles = new[] { "debugger.js", "debugger.js.map", "debugger.ts" };
        var requiredFiles = new[] { "abies.js" };

        // Act
        await Assert.That(Directory.Exists(_publishOutputDir)).IsTrue();

        var filesInPublish = Directory.GetFiles(_publishOutputDir, "*.js")
            .Select(p => Path.GetFileName(p))
            .ToList();

        // Assert
        foreach (var forbiddenFile in forbiddenFiles)
        {
            await Assert.That(filesInPublish).DoesNotContain(forbiddenFile);
        }

        foreach (var requiredFile in requiredFiles)
        {
            await Assert.That(filesInPublish).Contains(requiredFile);
        }

        // No debug symbols
        var debugSymbols = Directory.GetFiles(_publishOutputDir, "*.map")
            .Select(p => Path.GetFileName(p))
            .ToList();
        
        await Assert.That(debugSymbols).IsEmpty();
    }

    /// <summary>
    /// Test 3e: When debugger.js is deliberately created in Debug builds,
    /// it is correctly excluded from Release publish via MSBuild ItemGroup conditions.
    /// 
    /// This test validates the MSBuild exclusion logic.
    /// 
    /// TODAY: Fails (no item group condition logic)
    /// TOMORROW: Passes when build process implements exclusion
    /// </summary>
    [Test]
    public async Task DebuggerJSExcludedViaProjectFileItemGroup_InReleaseConfiguration()
    {
        // Arrange
        var projectFile = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "..", "..", "..", "Picea.Abies.Browser", "Picea.Abies.Browser.csproj"
        );

        // Act
        await Assert.That(File.Exists(projectFile)).IsTrue();

        var projectContent = File.ReadAllText(projectFile);

        // Assert: Check for ItemGroup condition that excludes debugger.js in Release
        var hasExclusionLogic = Regex.IsMatch(
            projectContent,
            @"<.*?\s+Exclude=""[^""]*debugger\.js[^""]*"".*?Condition=""[^""]*Release",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        ) || Regex.IsMatch(
            projectContent,
            @"Condition=""[^""]*Release[^""]*"".*?Exclude=""[^""]*debugger\.js",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        await Assert.That(hasExclusionLogic).IsTrue();
    }
}
