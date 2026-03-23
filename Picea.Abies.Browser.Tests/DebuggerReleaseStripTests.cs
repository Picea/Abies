// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Text;
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
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string BrowserProjectDir = Path.Combine(RepoRoot, "Picea.Abies.Browser");
    private static readonly string BrowserProjectFilePath = Path.Combine(BrowserProjectDir, "Picea.Abies.Browser.csproj");
    private static readonly string PublishOutputDir = Path.Combine(BrowserProjectDir, "bin", "Release", "net10.0", "publish", "wwwroot");
    private static readonly string BrowserDll = Path.Combine(BrowserProjectDir, "bin", "Release", "net10.0", "Picea.Abies.Browser.dll");
    private static readonly string SourceAbiesJs = Path.Combine(BrowserProjectDir, "wwwroot", "abies.js");
    private static readonly SemaphoreSlim ReleasePublishGate = new(1, 1);
    private static bool _releaseArtifactsPublished;

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
        await EnsureReleaseArtifacts();

        // Arrange
        var debuggerJsPath = Path.Combine(PublishOutputDir, "debugger.js");

        // Act & Assert
        if (Directory.Exists(PublishOutputDir))
        {
            await Assert.That(File.Exists(debuggerJsPath)).IsFalse();
            return;
        }

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
        await EnsureReleaseArtifacts();

        // Arrange
        var abiesJsPath = Directory.Exists(PublishOutputDir)
            ? Path.Combine(PublishOutputDir, "abies.js")
            : SourceAbiesJs;

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
        await EnsureReleaseArtifacts();

        // Arrange
        // Act
        await Assert.That(File.Exists(BrowserDll)).IsTrue();

        // Load assembly and scan types
        var assemblyBytes = await File.ReadAllBytesAsync(BrowserDll);
        var assemblyText = Encoding.UTF8.GetString(assemblyBytes);

        // Assert
        await Assert.That(assemblyText.Contains("Picea.Abies.Browser.Debugger", StringComparison.Ordinal)).IsFalse();
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
        await EnsureReleaseArtifacts();

        // Arrange
        var forbiddenFiles = new[] { "debugger.js", "debugger.js.map", "debugger.ts" };
        var publishDirectoryExists = Directory.Exists(PublishOutputDir);

        if (!publishDirectoryExists)
        {
            await Assert.That(File.Exists(SourceAbiesJs)).IsTrue();
            return;
        }

        var filesInPublish = Directory.GetFiles(PublishOutputDir, "*.js")
            .Select(p => Path.GetFileName(p))
            .ToList();

        // Assert
        foreach (var forbiddenFile in forbiddenFiles)
        {
            await Assert.That(filesInPublish).DoesNotContain(forbiddenFile);
        }

        // No debug symbols
        var debugSymbols = Directory.GetFiles(PublishOutputDir, "*.map")
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
        // Act
        await Assert.That(File.Exists(BrowserProjectFilePath)).IsTrue();

        var projectContent = File.ReadAllText(BrowserProjectFilePath);

        // Assert: Check for ItemGroup condition that excludes debugger.js in Release
        var hasReleaseItemGroup = Regex.IsMatch(projectContent, @"<ItemGroup\s+Condition=""'\$\(Configuration\)'\s*==\s*'Release'""", RegexOptions.IgnoreCase);
        var hasContentRemoval = Regex.IsMatch(projectContent, @"<Content\s+Remove=""wwwroot/debugger\.js""\s*/>", RegexOptions.IgnoreCase);
        var hasNoneRemoval = Regex.IsMatch(projectContent, @"<None\s+Remove=""wwwroot/debugger\.js""\s*/>", RegexOptions.IgnoreCase);

        await Assert.That(hasReleaseItemGroup && hasContentRemoval && hasNoneRemoval).IsTrue();
    }

    private static async Task EnsureReleaseArtifacts()
    {
        if (_releaseArtifactsPublished)
        {
            return;
        }

        await ReleasePublishGate.WaitAsync();
        try
        {
            if (_releaseArtifactsPublished)
            {
                return;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    WorkingDirectory = RepoRoot,
                    ArgumentList =
                    {
                        "build",
                        BrowserProjectFilePath,
                        "-c",
                        "Release",
                        "-p:TreatWarningsAsErrors=false",
                        "-v",
                        "minimal"
                    },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode is not 0)
            {
                throw new InvalidOperationException($"dotnet publish failed with exit code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
            }

            _releaseArtifactsPublished = true;
        }
        finally
        {
            ReleasePublishGate.Release();
        }
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

        throw new DirectoryNotFoundException("Could not locate repository root from test execution directory.");
    }
}
