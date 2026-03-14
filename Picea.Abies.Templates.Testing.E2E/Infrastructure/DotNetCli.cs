// =============================================================================
// DotNetCli — Shell Helper for dotnet CLI Operations
// =============================================================================
// Provides async wrappers around dotnet CLI commands used during template E2E
// testing: pack, new, build, run. Each method captures stdout/stderr and throws
// on non-zero exit codes (unless explicitly suppressed).
// =============================================================================

using System.Diagnostics;

namespace Picea.Abies.Templates.Testing.E2E.Infrastructure;

/// <summary>
/// Async wrappers for dotnet CLI commands with captured output.
/// </summary>
public static class DotNetCli
{
    /// <summary>
    /// Runs an arbitrary <c>dotnet</c> command and returns captured output.
    /// </summary>
    /// <param name="arguments">Arguments to pass to <c>dotnet</c>.</param>
    /// <param name="workingDirectory">Working directory for the process.</param>
    /// <param name="environmentVariables">Optional environment variables to set.</param>
    /// <param name="timeoutSeconds">Maximum wall-clock time before the process is killed.</param>
    /// <returns>A tuple of (exitCode, stdout, stderr).</returns>
    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        string arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null,
        int timeoutSeconds = 300)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (workingDirectory is not null)
            psi.WorkingDirectory = workingDirectory;

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
                psi.Environment[key] = value;
        }

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Read stdout/stderr concurrently to avoid deadlocks
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync()
            .WaitAsync(TimeSpan.FromSeconds(timeoutSeconds));

        var stdOut = await stdOutTask;
        var stdErr = await stdErrTask;

        return (process.ExitCode, stdOut, stdErr);
    }

    /// <summary>
    /// Packs a project into a NuGet package.
    /// </summary>
    public static async Task PackAsync(string projectPath, string outputDir)
    {
        var (exitCode, stdOut, stdErr) = await RunAsync(
            $"pack \"{projectPath}\" -c Release -o \"{outputDir}\" --no-restore",
            timeoutSeconds: 120);

        if (exitCode != 0)
            throw new InvalidOperationException(
                $"dotnet pack failed for {projectPath}.\n" +
                $"Exit code: {exitCode}\n" +
                $"stdout: {stdOut}\n" +
                $"stderr: {stdErr}");
    }

    /// <summary>
    /// Creates a new project from an installed template.
    /// </summary>
    public static async Task NewAsync(
        string templateName, string projectName, string outputDir)
    {
        var (exitCode, stdOut, stdErr) = await RunAsync(
            $"new {templateName} -n {projectName} -o \"{outputDir}\"",
            timeoutSeconds: 60);

        if (exitCode != 0)
            throw new InvalidOperationException(
                $"dotnet new {templateName} failed.\n" +
                $"Exit code: {exitCode}\n" +
                $"stdout: {stdOut}\n" +
                $"stderr: {stdErr}");
    }

    /// <summary>
    /// Builds a project.
    /// </summary>
    public static async Task BuildAsync(string projectDir, int timeoutSeconds = 300)
    {
        var (exitCode, stdOut, stdErr) = await RunAsync(
            "build -c Debug",
            workingDirectory: projectDir,
            timeoutSeconds: timeoutSeconds);

        if (exitCode != 0)
            throw new InvalidOperationException(
                $"dotnet build failed in {projectDir}.\n" +
                $"Exit code: {exitCode}\n" +
                $"stdout: {stdOut}\n" +
                $"stderr: {stdErr}");
    }

    /// <summary>
    /// Starts <c>dotnet run</c> as a background process, returning the <see cref="Process"/>
    /// handle so the caller can manage its lifetime.
    /// </summary>
    public static Process StartRun(
        string projectDir, int port, Dictionary<string, string>? extraEnv = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --urls http://localhost:{port}",
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.Environment["ASPNETCORE_URLS"] = $"http://localhost:{port}";
        psi.Environment["DOTNET_ENVIRONMENT"] = "Development";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        // Use the E2ETest launch profile we wrote into launchSettings.json.
        // This is critical for WasmAppHost which ignores --urls and ASPNETCORE_URLS.
        psi.Environment["DOTNET_LAUNCH_PROFILE"] = "E2ETest";

        if (extraEnv is not null)
        {
            foreach (var (key, value) in extraEnv)
                psi.Environment[key] = value;
        }

        var process = new Process { StartInfo = psi };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                Console.WriteLine($"  [app stdout] {e.Data}");
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                Console.WriteLine($"  [app stderr] {e.Data}");
        };

        process.Start();

        // Drain stdout/stderr asynchronously to prevent buffer deadlock
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    /// <summary>
    /// Installs a dotnet template from a .nupkg file path.
    /// </summary>
    public static async Task InstallTemplateAsync(string nupkgPath)
    {
        var (exitCode, stdOut, stdErr) = await RunAsync(
            $"new install \"{nupkgPath}\" --force",
            timeoutSeconds: 60);

        if (exitCode != 0)
            throw new InvalidOperationException(
                $"dotnet new install failed for {nupkgPath}.\n" +
                $"Exit code: {exitCode}\n" +
                $"stdout: {stdOut}\n" +
                $"stderr: {stdErr}");
    }

    /// <summary>
    /// Uninstalls a dotnet template package by ID.
    /// </summary>
    public static async Task UninstallTemplateAsync(string packageId)
    {
        // Best-effort — ignore failures during cleanup
        await RunAsync($"new uninstall {packageId}", timeoutSeconds: 30);
    }
}
