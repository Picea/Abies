using System.Diagnostics;
using System.Text;

namespace Picea.Abies.Tests.Debugger;

internal static class PublishOutputProbe
{
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

    public static async Task<string> PublishReleaseProject(string projectRelativePath, CancellationToken cancellationToken = default)
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
