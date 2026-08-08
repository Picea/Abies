using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Picea.Abies.Tests.Debugger;

/// <summary>
/// Publishes a project once per test run so release-output assertions can
/// inspect the result.
/// </summary>
/// <remarks>
/// Two things here are load-bearing, and both exist because this probe caused a
/// recurring CI failure:
///
/// <para>
/// <b>One publish per project.</b> Several tests assert against the same
/// published output and the runner executes them in parallel. Each call used to
/// start its own <c>dotnet publish</c>, so two MSBuild processes wrote the same
/// files concurrently and one lost:
/// <c>The process cannot access the file 'Picea.Abies.deps.json' because it is
/// being used by another process</c>. Results are now memoized per project, so
/// the publish happens once however many tests ask for it — which is also
/// faster.
/// </para>
///
/// <para>
/// <b>Why not also redirect bin/obj.</b> Sending intermediates elsewhere with
/// <c>BaseIntermediateOutputPath</c> looks tidier but breaks the build: the SDK
/// excludes the *configured* intermediate directory from source globs, so the
/// repository's own <c>obj/</c> stops being excluded and its generated
/// <c>Picea.Abies.Version.cs</c> gets compiled a second time (CS0579, duplicate
/// assembly attributes). Serializing the publish is enough on its own.
/// </para>
/// </remarks>
internal static class PublishOutputProbe
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<string>>> _publishes =
        new(StringComparer.OrdinalIgnoreCase);

    public static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Picea.Abies.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not resolve repository root for publish probe.");
    }

    /// <summary>
    /// Publishes <paramref name="projectRelativePath"/> in Release and returns the
    /// output directory. Concurrent callers share a single publish.
    /// </summary>
    public static Task<string> PublishReleaseProject(string projectRelativePath, CancellationToken cancellationToken = default)
    {
        // LazyThreadSafetyMode is the default for Lazy<T>, so only the first
        // caller starts the process; the rest await the same task.
        var publish = _publishes.GetOrAdd(
            projectRelativePath,
            key => new Lazy<Task<string>>(() => PublishOnce(key, cancellationToken)));

        return publish.Value;
    }

    private static async Task<string> PublishOnce(string projectRelativePath, CancellationToken cancellationToken)
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var projectPath = Path.Combine(repositoryRoot, projectRelativePath);

        if (!File.Exists(projectPath))
        {
            throw new InvalidOperationException($"Publish probe project path not found: {projectPath}");
        }

        var outputDirectory = Path.Combine(Path.GetTempPath(), "abies-test-publish", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectPath}\" -c Release -o \"{outputDirectory}\"",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet publish process for release-strip gate.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet publish failed for release-strip gate. ExitCode={process.ExitCode}\nSTDOUT:\n{standardOutput}\nSTDERR:\n{standardError}"
            );
        }

        return outputDirectory;
    }

    public static bool FileContainsUtf8Token(string filePath, string token)
    {
        var bytes = File.ReadAllBytes(filePath);
        var tokenBytes = Encoding.UTF8.GetBytes(token);

        return bytes.AsSpan().IndexOf(tokenBytes) >= 0;
    }
}
