// =============================================================================
// LocalNuGetFeed — Pack-Once Shared Infrastructure for Template E2E Tests
// =============================================================================
// Creates a temporary NuGet feed containing locally-packed versions of all
// Abies packages, then installs the template package so that `dotnet new`
// can scaffold real projects from local sources.
//
// Lifecycle (TUnit ClassDataSource with SharedType.PerTestSession):
//   InitializeAsync  → pack + install (once per test session)
//   DisposeAsync      → uninstall templates + delete temp feed
//
// The NBGV version (e.g. "1.0.342-rc-0002-g6ae8d9a842") is discovered from
// the .nupkg filenames after packing, then exposed via PackageVersion so that
// fixtures can patch generated csproj files.
// =============================================================================

using TUnit.Core.Interfaces;

namespace Picea.Abies.Templates.Testing.E2E.Infrastructure;

/// <summary>
/// Shared per-session resource that packs all Abies NuGet packages into a
/// temporary feed directory and installs the template package.
/// </summary>
public sealed class LocalNuGetFeed : IAsyncInitializer, IAsyncDisposable
{
    private static readonly string[] _projectsToPack =
    [
        "Picea.Abies",
        "Picea.Abies.Browser",
        "Picea.Abies.Server",
        "Picea.Abies.Server.Kestrel",
        "Picea.Abies.Templates"
    ];

    /// <summary>Absolute path to the temporary NuGet feed directory.</summary>
    public string FeedDir { get; private set; } = "";

    /// <summary>
    /// The NBGV-generated package version discovered from .nupkg filenames.
    /// Used by fixtures to patch template csproj <c>Version="1.0.0-*"</c> references.
    /// </summary>
    public string PackageVersion { get; private set; } = "";

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        FeedDir = Path.Join(Path.GetTempPath(), $"abies-e2e-feed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(FeedDir);

        var solutionDir = FindSolutionDirectory();

        Console.WriteLine($"[LocalNuGetFeed] Packing to {FeedDir}");

        // Pack each project sequentially (dotnet pack accepts one project at a time)
        foreach (var project in _projectsToPack)
        {
            var projectPath = Path.Join(solutionDir, project, $"{project}.csproj");
            Console.WriteLine($"[LocalNuGetFeed] Packing {project}...");
            await DotNetCli.PackAsync(projectPath, FeedDir);
        }

        // Discover the NBGV version from one of the generated .nupkg filenames.
        // Pattern: Picea.Abies.{version}.nupkg — we pick Picea.Abies (the root).
        PackageVersion = DiscoverVersion("Picea.Abies");
        Console.WriteLine($"[LocalNuGetFeed] Discovered version: {PackageVersion}");

        // Install the template package from the feed
        var templateNupkg = Directory.GetFiles(FeedDir, "Picea.Abies.Templates.*.nupkg")
            .FirstOrDefault()
            ?? throw new FileNotFoundException(
                "Picea.Abies.Templates .nupkg not found in feed directory.");

        Console.WriteLine($"[LocalNuGetFeed] Installing templates from {templateNupkg}");
        await DotNetCli.InstallTemplateAsync(templateNupkg);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("[LocalNuGetFeed] Cleaning up...");

        // Best-effort uninstall
        await DotNetCli.UninstallTemplateAsync("Picea.Abies.Templates");

        // Delete the temp feed directory
        try
        {
            if (Directory.Exists(FeedDir))
                Directory.Delete(FeedDir, recursive: true);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[LocalNuGetFeed] Warning: Could not delete {FeedDir}: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts the version string from the .nupkg filename for a given package ID.
    /// </summary>
    /// <example>
    /// "Picea.Abies.1.0.342-rc-0002-g6ae8d9a842.nupkg" → "1.0.342-rc-0002-g6ae8d9a842"
    /// </example>
    private string DiscoverVersion(string packageId)
    {
        var nupkgs = Directory.GetFiles(FeedDir, $"{packageId}.*.nupkg");

        // Filter out packages whose ID is a superstring (e.g. Picea.Abies.Browser)
        var exactMatch = nupkgs
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .FirstOrDefault(name =>
            {
                // Name is "Picea.Abies.1.0.342-rc-0002-g..." — after removing the
                // package ID prefix + dot, the remainder must start with a digit (version).
                var prefix = packageId + ".";
                if (!name!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return false;

                var remainder = name[prefix.Length..];
                return remainder.Length > 0 && char.IsDigit(remainder[0]);
            });

        if (exactMatch is null)
            throw new FileNotFoundException(
                $"Could not find .nupkg for {packageId} in {FeedDir}. " +
                $"Found: [{string.Join(", ", nupkgs.Select(Path.GetFileName))}]");

        return exactMatch[(packageId.Length + 1)..];
    }

    private static string FindSolutionDirectory()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (File.Exists(Path.Join(dir, "Picea.Abies.sln")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException(
            "Could not find solution directory (containing Picea.Abies.sln). " +
            "Ensure the test is run from within the Abies repository.");
    }
}
