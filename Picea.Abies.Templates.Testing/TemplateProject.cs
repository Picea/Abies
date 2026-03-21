using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Picea.Abies.Templates.Testing;

/// <summary>
/// Scaffolds a project from a local dotnet template, patches its csproj to use
/// ProjectReferences instead of NuGet PackageReferences, builds and optionally
/// runs the project as an external process.
/// </summary>
/// <remarks>
/// <para>
/// Templates reference Abies NuGet packages (e.g. <c>Picea.Abies.Server.Kestrel</c>).
/// Since we want to test against the <em>current</em> source code rather than a
/// published package, the csproj is rewritten at scaffold time to point at the
/// repo's project files via absolute-path <c>ProjectReference</c> elements.
/// </para>
/// </remarks>
public sealed partial class TemplateProject : IAsyncDisposable
{
    private static readonly string _repoRoot = Path.GetFullPath(
        Path.Join(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static readonly string _templatesDir =
        Path.Join(_repoRoot, "Picea.Abies.Templates", "templates");

    /// <summary>
    /// Maps NuGet package names to their corresponding project file paths
    /// (relative to the repo root).
    /// </summary>
    private static readonly Dictionary<string, string> _packageToProjectMap = new()
    {
        ["Picea.Abies.Server.Kestrel"] =
            Path.Join(_repoRoot, "Picea.Abies.Server.Kestrel", "Picea.Abies.Server.Kestrel.csproj"),
        ["Picea.Abies.Browser"] =
            Path.Join(_repoRoot, "Picea.Abies.Browser", "Picea.Abies.Browser.csproj"),
    };

    /// <summary>
    /// Serializes <c>dotnet new install</c> / <c>dotnet new uninstall</c> calls.
    /// Template installation is global machine state and is not safe to run
    /// concurrently (xUnit runs tests in parallel by default).
    /// </summary>
    private static readonly SemaphoreSlim _templateLock = new(1, 1);

    private readonly string _tempDir;
    private readonly string _projectDir;
    private readonly string _projectName;
    private readonly string _templateShortName;
    private Process? _runProcess;
    private bool _disposed;

    /// <summary>Base URL of the running server (available after <see cref="RunAsync"/>).</summary>
    public string? BaseUrl { get; private set; }

    /// <summary>Absolute path to the scaffolded project directory.</summary>
    public string ProjectDir => _projectDir;

    private TemplateProject(string tempDir, string projectDir, string projectName, string templateShortName)
    {
        _tempDir = tempDir;
        _projectDir = projectDir;
        _projectName = projectName;
        _templateShortName = templateShortName;
    }

    // -----------------------------------------------------------------------
    // Factory
    // -----------------------------------------------------------------------

    /// <summary>
    /// Scaffolds a new project from <paramref name="templateShortName"/>, patches
    /// the csproj for local ProjectReferences, and builds it.
    /// </summary>
    /// <remarks>
    /// The scaffolded project is placed under <c>{RepoRoot}/.test-output/</c>
    /// rather than the system temp directory. On macOS, <c>/var</c> is a symlink to
    /// <c>/private/var</c>, which causes MSBuild to mis-resolve transitive
    /// <c>ProjectReference</c> paths when the consumer and the referenced projects
    /// sit on different sides of the symlink boundary.
    /// </remarks>
    public static async Task<TemplateProject> CreateAsync(
        string templateShortName,
        string projectName,
        CancellationToken ct = default)
    {
        var tempDir = Path.Join(_repoRoot, ".test-output", $"template-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var projectDir = Path.Join(tempDir, projectName);

        try
        {
            var templateDir = Path.Join(_templatesDir, templateShortName);

            await _templateLock.WaitAsync(ct);
            try
            {
                await RunDotnetAsync($"new install \"{templateDir}\" --force", tempDir, ct);

                // Keep template installation and scaffold invocation in the same
                // critical section so another test cannot uninstall the template
                // between these two commands.
                await RunDotnetAsync($"new {templateShortName} -n {projectName} -o \"{projectDir}\"", tempDir, ct);
            }
            finally
            {
                _templateLock.Release();
            }

            PatchCsproj(projectDir, projectName);

            await RunDotnetAsync($"build \"{projectDir}\" -c Debug", tempDir, ct);

            return new TemplateProject(tempDir, projectDir, projectName, templateShortName);
        }
        catch
        {
            // Clean up on failure — uninstall template and remove temp dir.
            var templateDir = Path.Join(_templatesDir, templateShortName);
            try
            {
                await _templateLock.WaitAsync(CancellationToken.None);
                try
                {
                    await RunDotnetAsync(
                        $"new uninstall \"{templateDir}\"", _repoRoot, CancellationToken.None);
                }
                finally
                {
                    _templateLock.Release();
                }
            }
            catch (InvalidOperationException)
            {
                // Best-effort template uninstall.
            }

            TryDeleteDirectory(tempDir);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // Run / Stop
    // -----------------------------------------------------------------------

    /// <summary>
    /// Starts the scaffolded project as a background process on a random port.
    /// Waits until the server responds to HTTP requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Microsoft.NET.Sdk.Web</c> projects (server templates) honour the
    /// <c>--urls</c> CLI argument to set the listen address.
    /// </para>
    /// <para>
    /// <c>Microsoft.NET.Sdk.WebAssembly</c> projects use <c>WasmAppHost</c> which
    /// picks its own port and prints <c>App url: http://…</c> to stdout. For these
    /// projects we parse the actual URL from the process output.
    /// </para>
    /// </remarks>
    public async Task RunAsync(CancellationToken ct = default)
    {
        if (IsWebAssemblyProject())
        {
            await RunWasmAsync(ct);
        }
        else
        {
            await RunServerAsync(ct);
        }
    }

    private async Task RunServerAsync(CancellationToken ct)
    {
        var port = GetRandomPort();
        BaseUrl = $"http://localhost:{port}";

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_projectDir}\" --no-build --urls {BaseUrl}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        _runProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet run");

        await WaitForServerReadyAsync(BaseUrl, 30, ct);
    }

    /// <summary>
    /// Starts a WebAssembly project and captures the actual dev server URL from
    /// <c>WasmAppHost</c>'s <c>App url: http://…</c> stdout output.
    /// </summary>
    private async Task RunWasmAsync(CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_projectDir}\" --no-build",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        _runProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet run");

        // WasmAppHost prints "App url: http://localhost:{port}/..." to stdout.
        // Parse the first http URL from that line to determine the actual port.
        var urlFound = new TaskCompletionSource<string>();
        var outputBuffer = new System.Text.StringBuilder();

        _ = Task.Run(async () =>
        {
            while (await _runProcess.StandardOutput.ReadLineAsync(ct) is { } line)
            {
                outputBuffer.AppendLine(line);
                Console.WriteLine($"[wasm-host] {line}");

                var match = AppUrlPattern().Match(line);
                if (match.Success)
                {
                    // Extract the base URL (scheme + host + port) without query args.
                    var url = match.Groups[1].Value;
                    var uri = new Uri(url);
                    var baseUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
                    urlFound.TrySetResult(baseUrl);
                }
            }
        }, ct);

        // Also drain stderr.
        _ = Task.Run(async () =>
        {
            while (await _runProcess.StandardError.ReadLineAsync(ct) is { } line)
            {
                Console.Error.WriteLine($"[wasm-host:err] {line}");
            }
        }, ct);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            BaseUrl = await urlFound.Task.WaitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"WasmAppHost did not print an App URL within 30 seconds.\n" +
                $"Stdout so far:\n{outputBuffer}");
        }

        await WaitForServerReadyAsync(BaseUrl, 60, ct);
    }

    [GeneratedRegex(@"App url:\s+(https?://\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex AppUrlPattern();

    /// <summary>Kills the running server process, if any.</summary>
    public void Stop()
    {
        if (_runProcess is { HasExited: false } p)
        {
            try
            {
                p.Kill(entireProcessTree: true);
                p.WaitForExit(5_000);
            }
            catch (InvalidOperationException)
            {
                // Process already exited between the HasExited check and Kill().
            }
        }
    }

    // -----------------------------------------------------------------------
    // Dispose
    // -----------------------------------------------------------------------

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        Stop();

        // Uninstall the template (best-effort, serialized with other install/uninstall).
        var templateDir = Path.Join(_templatesDir, _templateShortName);
        try
        {
            await _templateLock.WaitAsync(CancellationToken.None);
            try
            {
                await RunDotnetAsync(
                    $"new uninstall \"{templateDir}\"", _tempDir, CancellationToken.None);
            }
            finally
            {
                _templateLock.Release();
            }
        }
        catch (InvalidOperationException)
        {
            // Template uninstall is best-effort.
        }

        TryDeleteDirectory(_tempDir);
    }

    // -----------------------------------------------------------------------
    // Internals
    // -----------------------------------------------------------------------

    /// <summary>
    /// Replaces NuGet <c>PackageReference</c> elements with local <c>ProjectReference</c>
    /// elements so the scaffolded project builds against the current repo source.
    /// </summary>
    /// <remarks>
    /// For WebAssembly projects that reference <c>Picea.Abies.Browser</c>, this also
    /// copies static assets (<c>abies.js</c>, <c>abies-otel.js</c>) into the project's
    /// <c>wwwroot/</c> directory. This is necessary because <c>Picea.Abies.Browser</c>
    /// uses the plain <c>Microsoft.NET.Sdk</c> — its <c>wwwroot/</c> files are not
    /// automatically served as static web assets by the WebAssembly SDK dev server
    /// when referenced via <c>ProjectReference</c>.
    /// </remarks>
    private static void PatchCsproj(string projectDir, string projectName)
    {
        var csprojPath = Path.Join(projectDir, $"{projectName}.csproj");
        var content = File.ReadAllText(csprojPath);

        var referencesBrowser = false;

        foreach (var (packageName, projectPath) in _packageToProjectMap)
        {
            // Match: <PackageReference Include="PackageName" Version="..." />
            var pattern = $"""<PackageReference Include="{packageName}" Version="[^"]*"\s*/>""";
            var replacement = $"""<ProjectReference Include="{projectPath}" />""";
            var newContent = Regex.Replace(content, pattern, _ => replacement);

            // Only flag browser reference if the csproj actually contained the package.
            if (packageName == "Picea.Abies.Browser" && newContent != content)
                referencesBrowser = true;

            content = newContent;
        }

        File.WriteAllText(csprojPath, content);

        // Copy static assets that would normally come from the NuGet package.
        if (referencesBrowser)
            CopyBrowserStaticAssets(projectDir);
    }

    /// <summary>
    /// Copies <c>abies.js</c> and <c>abies-otel.js</c> from the
    /// <c>Picea.Abies.Browser/wwwroot/</c> source directory into the scaffolded
    /// project's <c>wwwroot/</c> so that the <c>WasmAppHost</c> dev server can
    /// serve them at the root path (matching the template's <c>index.html</c> refs).
    /// </summary>
    private static void CopyBrowserStaticAssets(string projectDir)
    {
        var browserWwwroot = Path.Join(_repoRoot, "Picea.Abies.Browser", "wwwroot");
        var targetWwwroot = Path.Join(projectDir, "wwwroot");
        Directory.CreateDirectory(targetWwwroot);

        foreach (var fileName in new[] { "abies.js", "abies-otel.js" })
        {
            var source = Path.Join(browserWwwroot, fileName);
            if (File.Exists(source))
                File.Copy(source, Path.Join(targetWwwroot, fileName), overwrite: true);
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when the scaffolded project uses
    /// <c>Microsoft.NET.Sdk.WebAssembly</c>.
    /// </summary>
    private bool IsWebAssemblyProject()
    {
        var csprojPath = Path.Join(_projectDir, $"{_projectName}.csproj");
        var content = File.ReadAllText(csprojPath);
        return content.Contains("Sdk.WebAssembly", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetRandomPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForServerReadyAsync(
        string baseUrl, int timeoutSeconds, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var response = await http.GetAsync(baseUrl, ct);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
                // Server not ready yet.
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                // HTTP timeout — server not ready yet.
            }

            await Task.Delay(250, ct);
        }

        throw new TimeoutException(
            $"Template server at {baseUrl} did not become ready within {timeoutSeconds} seconds.");
    }

    private static async Task RunDotnetAsync(
        string arguments, string workingDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start: dotnet {arguments}");

        // Read output/error to prevent deadlocks.
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet {arguments} failed (exit {process.ExitCode}).\n" +
                $"STDOUT:\n{stdout}\n" +
                $"STDERR:\n{stderr}");
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup — file may be locked.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup — insufficient permissions.
        }
    }
}
