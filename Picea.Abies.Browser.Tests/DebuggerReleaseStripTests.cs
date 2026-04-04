// Copyright (c) 2024 Abies Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

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
    private static readonly string PublishRootDir = Path.Combine(BrowserProjectDir, "bin", "Release", "net10.0", "publish");
    private static readonly string BrowserDll = Path.Combine(PublishRootDir, "Picea.Abies.Browser.dll");
    private static readonly SemaphoreSlim ReleasePublishGate = new(1, 1);
    private static bool _releaseArtifactsPublished;

    /// <summary>
    /// Test 3a: The debugger.js file does NOT exist in the Release published wwwroot/ folder.
    /// Debugger UI module should be completely excluded from Release bundle.
    /// 
    /// Validates the seam: Build exclusion of debug-only JS module.
    /// </summary>
    [Test]
    public async Task ReleaseAssetCancel_DebuggerJSNotIncludedInReleaseBuild()
    {
        await EnsureReleaseArtifacts();

        // Act & Assert: debugger.js must be absent anywhere under publish output.
        var debuggerJsArtifacts = FindPublishArtifactsByFileName("debugger.js");
        await Assert.That(debuggerJsArtifacts).IsEmpty();
    }

    /// <summary>
    /// Test 3b: The core abies.js file contains NO references to debugger-related identifiers.
    /// This ensures that release build of abies.js is free of debugger hooks, mounts, or conditional logic.
    /// 
    /// Validates the seam: Core JS module is clean and release-safe.
    /// </summary>
    [Test]
    public async Task ReleaseAbiesJS_ContainsNoDebuggerReferences()
    {
        await EnsureReleaseArtifacts();

        // Arrange/Act: only inspect emitted publish artifacts.
        var abiesJsArtifacts = FindPublishArtifactsByFileName("abies.js");
        var abiesJsIsPublishContract = IsAbiesJsPartOfPublishContract();

        if (abiesJsArtifacts.Count is 0)
        {
            // Keep release-strip contract artifact-based: debugger.js must not be emitted anywhere.
            var debuggerJsArtifacts = FindPublishArtifactsByFileName("debugger.js");
            await Assert.That(debuggerJsArtifacts).IsEmpty();

            if (abiesJsIsPublishContract)
            {
                throw new InvalidOperationException(
                    $"Release publish contract for {BrowserProjectFilePath} includes abies.js, but no abies.js artifact was emitted under {PublishRootDir}.");
            }

            Console.WriteLine(
                $"Skipping abies.js debugger-reference content assertion because abies.js is not part of this project's publish contract ({BrowserProjectFilePath}).");
            return;
        }

        var abiesJsContent = File.ReadAllText(abiesJsArtifacts[0]);

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
    /// </summary>
    [Test]
    public async Task ReleaseBuildExcludesDebuggerCSharpNamespace_FromAssemblyIL()
    {
        await EnsureReleaseArtifacts();
        EnsurePublishArtifactsExist(BrowserDll);

        // Arrange
        // Act
        await Assert.That(File.Exists(BrowserDll)).IsTrue();

        var debuggerTypes = GetDebuggerTypesInAssembly(BrowserDll);

        // Assert
        await Assert.That(debuggerTypes)
            .IsEmpty();
    }

    /// <summary>
    /// Test 3d: The Release-published wwwroot/ folder contains ONLY expected files
    /// (abies.js and other core assets) and does NOT contain debugger.js or debug symbols.
    /// This is a comprehensive file inventory check.
    /// 
    /// Validates the seam: Release artifact cleanliness.
    /// </summary>
    [Test]
    public async Task ReleaseBuildExcludesDebuggerModule_FromPublishOutput()
    {
        await EnsureReleaseArtifacts();

        // Arrange
        var forbiddenFiles = new[] { "debugger.js", "debugger.js.map", "debugger.ts" };

        var filesInPublish = Directory.GetFiles(PublishRootDir, "*.js", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .ToList();

        // Assert
        foreach (var forbiddenFile in forbiddenFiles)
        {
            await Assert.That(filesInPublish).DoesNotContain(forbiddenFile);
        }

        // No debug symbols
        var debugSymbols = Directory.GetFiles(PublishRootDir, "*.map", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .ToList();
        
        await Assert.That(debugSymbols).IsEmpty();
    }

    /// <summary>
    /// Test 3e: When debugger.js is deliberately created in Debug builds,
    /// it is correctly excluded from Release publish via MSBuild ItemGroup conditions.
    /// 
    /// This test validates the MSBuild exclusion logic.
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

    [Test]
    public async Task ReleasePublishArtifacts_DoNotIncludeDebuggerJs()
    {
        var publishOutputDir = Path.Combine(
            Path.GetTempPath(),
            $"abies-browser-release-{Guid.NewGuid():N}");

        Directory.CreateDirectory(publishOutputDir);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    WorkingDirectory = RepoRoot,
                    ArgumentList =
                    {
                        "publish",
                        BrowserProjectFilePath,
                        "-c",
                        "Release",
                        "-o",
                        publishOutputDir,
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
                throw new InvalidOperationException(
                    $"dotnet publish failed with exit code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
            }

            var publishedFiles = Directory
                .EnumerateFiles(publishOutputDir, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(publishOutputDir, path))
                .ToList();

            await Assert.That(publishedFiles).IsNotEmpty();

            var debuggerArtifacts = publishedFiles
                .Where(path => string.Equals(Path.GetFileName(path), "debugger.js", StringComparison.OrdinalIgnoreCase))
                .ToList();

            await Assert.That(debuggerArtifacts).IsEmpty();
        }
        finally
        {
            try
            {
                if (Directory.Exists(publishOutputDir))
                {
                    Directory.Delete(publishOutputDir, recursive: true);
                }
            }
            catch (IOException)
            {
                // Leave temp artifacts if cleanup races with file locks on CI hosts.
            }
            catch (UnauthorizedAccessException)
            {
                // Leave temp artifacts if cleanup is not permitted by the host environment.
            }
        }
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
                        "publish",
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

            EnsurePublishArtifactsExist(BrowserDll);
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

    private static void EnsurePublishArtifactsExist(params string[] requiredPaths)
    {
        var missingPaths = requiredPaths
            .Where(path => !File.Exists(path) && !Directory.Exists(path))
            .ToArray();

        if (missingPaths.Length is 0)
        {
            return;
        }

        var publishInventory = Directory.Exists(PublishRootDir)
            ? string.Join(
                Environment.NewLine,
                Directory.EnumerateFileSystemEntries(PublishRootDir, "*", SearchOption.AllDirectories)
                    .Take(25)
                    .Select(path => $"  - {Path.GetRelativePath(PublishRootDir, path)}"))
            : "  <publish root directory does not exist>";

        throw new InvalidOperationException(
            $"Release publish artifacts are missing.{Environment.NewLine}" +
            $"Required paths:{Environment.NewLine}{string.Join(Environment.NewLine, missingPaths.Select(path => $"  - {path}"))}{Environment.NewLine}" +
            $"Publish root: {PublishRootDir}{Environment.NewLine}" +
            $"Observed publish inventory (first 25 entries):{Environment.NewLine}{publishInventory}{Environment.NewLine}" +
            $"Regenerate artifacts with:{Environment.NewLine}" +
            $"dotnet publish {BrowserProjectFilePath} -c Release -p:TreatWarningsAsErrors=false -v minimal");
    }

    private static List<string> FindPublishArtifactsByFileName(string fileName)
    {
        if (!Directory.Exists(PublishRootDir))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(PublishRootDir, "*", SearchOption.AllDirectories)
            .Where(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static bool IsAbiesJsPartOfPublishContract()
    {
        if (!File.Exists(BrowserProjectFilePath))
        {
            return false;
        }

        var projectContent = File.ReadAllText(BrowserProjectFilePath);

        // Treat abies.js as part of publish contract only when it is explicitly configured
        // as a publishable output artifact.
        return Regex.IsMatch(
            projectContent,
            @"<(Content|None)\s+[^>]*Include=\""""wwwroot/abies\.js\""""[^>]*\bCopyToPublishDirectory\s*=\s*\""""(Always|PreserveNewest)\""""",
            RegexOptions.IgnoreCase);
    }

    private static IReadOnlyList<string> GetDebuggerTypesInAssembly(string assemblyPath)
    {
        using var assemblyStream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(assemblyStream);

        if (!peReader.HasMetadata)
        {
            throw new InvalidOperationException($"Assembly '{assemblyPath}' does not contain metadata.");
        }

        var metadataReader = peReader.GetMetadataReader();

        return metadataReader.TypeDefinitions
            .Select(typeHandle => metadataReader.GetTypeDefinition(typeHandle))
            .Select(typeDefinition =>
            {
                var typeNamespace = metadataReader.GetString(typeDefinition.Namespace);
                var typeName = metadataReader.GetString(typeDefinition.Name);
                return string.IsNullOrEmpty(typeNamespace)
                    ? typeName
                    : $"{typeNamespace}.{typeName}";
            })
            .Where(fullTypeName =>
                fullTypeName.StartsWith("Picea.Abies.Debugger", StringComparison.Ordinal) ||
                fullTypeName.StartsWith("Picea.Abies.Browser.Debugger", StringComparison.Ordinal))
            .ToArray();
    }
}
