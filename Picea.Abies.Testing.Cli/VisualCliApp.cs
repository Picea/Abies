using System.CommandLine;
using System.Text;

namespace Picea.Abies.Testing.Cli;

public static class VisualCliApp
{
    public static Task<int> InvokeAsync(string[] args, TextWriter? output = null, TextWriter? error = null)
    {
        var resolvedOutput = output ?? Console.Out;
        var resolvedError = error ?? Console.Error;
        var root = BuildRootCommand(resolvedOutput, resolvedError);

        return root.Parse(args).InvokeAsync();
    }

    public static RootCommand BuildRootCommand(TextWriter output, TextWriter error)
    {
        var root = new RootCommand("Abies testing CLI");
        var visual = new Command("visual", "Visual baseline workflows");

        visual.Add(BuildAcceptCommand(output, error));
        visual.Add(BuildStatusCommand(output));
        visual.Add(BuildReportCommand(output));

        root.Add(visual);
        return root;
    }

    private static Command BuildAcceptCommand(TextWriter output, TextWriter error)
    {
        var command = new Command("accept", "Accept one visual snapshot or all pending snapshots");
        var snapshotArgument = new Argument<string?>("snapshot")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var allOption = new Option<bool>("--all")
        {
            Description = "Accept all pending snapshots."
        };
        var artifactsOption = new Option<string>("--artifacts")
        {
            Description = "Artifacts directory containing *.actual.png files.",
            DefaultValueFactory = _ => VisualBaselineWorkflow.DefaultArtifactsDirectory
        };
        var baselinesOption = new Option<string>("--baselines")
        {
            Description = "Baselines directory receiving accepted snapshots.",
            DefaultValueFactory = _ => VisualBaselineWorkflow.DefaultBaselinesDirectory
        };

        command.Add(snapshotArgument);
        command.Add(allOption);
        command.Add(artifactsOption);
        command.Add(baselinesOption);

        command.SetAction(parseResult =>
        {
            var snapshot = parseResult.GetValue(snapshotArgument);
            var all = parseResult.GetValue(allOption);
            var artifactsDirectory = parseResult.GetValue(artifactsOption)!;
            var baselinesDirectory = parseResult.GetValue(baselinesOption)!;

            if (all)
            {
                if (string.IsNullOrWhiteSpace(snapshot) is false)
                {
                    error.WriteLine("Do not pass <snapshot> when using --all.");
                    return 1;
                }

                return VisualBaselineWorkflow.AcceptAll(artifactsDirectory, baselinesDirectory, output);
            }

            if (string.IsNullOrWhiteSpace(snapshot))
            {
                error.WriteLine("Specify <snapshot> or use --all.");
                return 1;
            }

            return VisualBaselineWorkflow.AcceptSnapshot(snapshot, artifactsDirectory, baselinesDirectory, output);
        });

        return command;
    }

    private static Command BuildStatusCommand(TextWriter output)
    {
        var command = new Command("status", "Show pending visual snapshot mismatches.");
        var artifactsOption = new Option<string>("--artifacts")
        {
            Description = "Artifacts directory containing *.actual.png files.",
            DefaultValueFactory = _ => VisualBaselineWorkflow.DefaultArtifactsDirectory
        };
        var baselinesOption = new Option<string>("--baselines")
        {
            Description = "Baselines directory used for resolved target paths.",
            DefaultValueFactory = _ => VisualBaselineWorkflow.DefaultBaselinesDirectory
        };

        command.Add(artifactsOption);
        command.Add(baselinesOption);

        command.SetAction(parseResult =>
        {
            var artifactsDirectory = parseResult.GetValue(artifactsOption)!;
            var baselinesDirectory = parseResult.GetValue(baselinesOption)!;
            var status = VisualBaselineWorkflow.GetStatus(artifactsDirectory, baselinesDirectory);
            output.WriteLine($"Pending visual snapshots: {status.PendingCount}");

            foreach (var pending in status.PendingSnapshots)
            {
                output.WriteLine($"- {pending.SnapshotName}");
            }

            return 0;
        });

        return command;
    }

    private static Command BuildReportCommand(TextWriter output)
    {
        var command = new Command("report", "Write a visual mismatch report to disk.");
        var outputOption = new Option<string>("--output")
        {
            Description = "Output directory for the report.",
            Required = true
        };
        var artifactsOption = new Option<string>("--artifacts")
        {
            Description = "Artifacts directory containing *.actual.png files.",
            DefaultValueFactory = _ => VisualBaselineWorkflow.DefaultArtifactsDirectory
        };
        var baselinesOption = new Option<string>("--baselines")
        {
            Description = "Baselines directory used for resolved target paths.",
            DefaultValueFactory = _ => VisualBaselineWorkflow.DefaultBaselinesDirectory
        };

        command.Add(outputOption);
        command.Add(artifactsOption);
        command.Add(baselinesOption);

        command.SetAction(parseResult =>
        {
            var reportOutputDirectory = parseResult.GetValue(outputOption)!;
            var artifactsDirectory = parseResult.GetValue(artifactsOption)!;
            var baselinesDirectory = parseResult.GetValue(baselinesOption)!;
            var reportPath = VisualBaselineWorkflow.WriteReport(artifactsDirectory, baselinesDirectory, reportOutputDirectory);
            output.WriteLine($"Report written: {reportPath}");
            return 0;
        });

        return command;
    }
}

public sealed record PendingSnapshot(string SnapshotName, string ActualPath, string BaselinePath, string? DiffPath);

public sealed record VisualStatus(int PendingCount, IReadOnlyList<PendingSnapshot> PendingSnapshots);

public static class VisualBaselineWorkflow
{
    public const string DefaultArtifactsDirectory = "artifacts/visual";
    public const string DefaultBaselinesDirectory = "baselines/visual";

    public static int AcceptSnapshot(string snapshot, string artifactsDirectory, string baselinesDirectory, TextWriter output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot);

        var actualPath = ResolveActualPath(snapshot, artifactsDirectory);
        if (File.Exists(actualPath) is false)
        {
            output.WriteLine($"Snapshot not found: {actualPath}");
            return 1;
        }

        var baselinePath = ResolveBaselinePath(snapshot, baselinesDirectory);
        EnsureParentDirectory(baselinePath);
        File.Copy(actualPath, baselinePath, overwrite: true);

        output.WriteLine($"Accepted: {Path.GetFileName(actualPath)} -> {baselinePath}");
        return 0;
    }

    public static int AcceptAll(string artifactsDirectory, string baselinesDirectory, TextWriter output)
    {
        var status = GetStatus(artifactsDirectory, baselinesDirectory);
        if (status.PendingCount == 0)
        {
            output.WriteLine("No pending visual snapshots.");
            return 0;
        }

        foreach (var pending in status.PendingSnapshots)
        {
            EnsureParentDirectory(pending.BaselinePath);
            File.Copy(pending.ActualPath, pending.BaselinePath, overwrite: true);
            output.WriteLine($"Accepted: {pending.SnapshotName}");
        }

        output.WriteLine($"Accepted {status.PendingCount} snapshots.");
        return 0;
    }

    public static VisualStatus GetStatus(string artifactsDirectory, string baselinesDirectory)
    {
        if (Directory.Exists(artifactsDirectory) is false)
        {
            return new VisualStatus(0, []);
        }

        var pending = Directory
            .EnumerateFiles(artifactsDirectory, "*.actual.png", SearchOption.TopDirectoryOnly)
            .Select(actualPath =>
            {
                var snapshotName = BuildSnapshotName(actualPath);
                var baselinePath = Path.Combine(baselinesDirectory, snapshotName);
                var diffPath = Path.Combine(artifactsDirectory, Path.GetFileNameWithoutExtension(snapshotName) + ".diff.png");

                return new PendingSnapshot(
                    snapshotName,
                    actualPath,
                    baselinePath,
                    File.Exists(diffPath) ? diffPath : null);
            })
            .OrderBy(snapshot => snapshot.SnapshotName, StringComparer.Ordinal)
            .ToArray();

        return new VisualStatus(pending.Length, pending);
    }

    public static string WriteReport(string artifactsDirectory, string baselinesDirectory, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var status = GetStatus(artifactsDirectory, baselinesDirectory);
        var reportPath = Path.Combine(outputDirectory, "visual-report.txt");

        var builder = new StringBuilder();
        builder.AppendLine("Visual Baseline Report");
        builder.AppendLine($"GeneratedAtUtc: {DateTimeOffset.UtcNow:O}");
        builder.AppendLine($"ArtifactsDirectory: {artifactsDirectory}");
        builder.AppendLine($"BaselinesDirectory: {baselinesDirectory}");
        builder.AppendLine($"PendingCount: {status.PendingCount}");

        foreach (var pending in status.PendingSnapshots)
        {
            builder.AppendLine($"- Snapshot: {pending.SnapshotName}");
            builder.AppendLine($"  Actual: {pending.ActualPath}");
            builder.AppendLine($"  Baseline: {pending.BaselinePath}");
            builder.AppendLine($"  Diff: {pending.DiffPath ?? "(none)"}");
        }

        File.WriteAllText(reportPath, builder.ToString(), Encoding.UTF8);
        return reportPath;
    }

    private static string ResolveActualPath(string snapshot, string artifactsDirectory)
    {
        if (Path.IsPathRooted(snapshot) && File.Exists(snapshot))
        {
            return snapshot;
        }

        var snapshotFileName = snapshot.EndsWith(".actual.png", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileName(snapshot)
            : Path.GetFileNameWithoutExtension(snapshot) + ".actual.png";

        return Path.Combine(artifactsDirectory, snapshotFileName);
    }

    private static string ResolveBaselinePath(string snapshot, string baselinesDirectory)
    {
        var snapshotName = snapshot.EndsWith(".actual.png", StringComparison.OrdinalIgnoreCase)
            ? BuildSnapshotName(snapshot)
            : Path.GetFileName(snapshot);

        if (snapshotName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) is false)
        {
            snapshotName += ".png";
        }

        return Path.Combine(baselinesDirectory, snapshotName);
    }

    private static string BuildSnapshotName(string actualPath)
    {
        var actualName = Path.GetFileName(actualPath);
        return actualName.EndsWith(".actual.png", StringComparison.OrdinalIgnoreCase)
            ? actualName.Replace(".actual.png", ".png", StringComparison.OrdinalIgnoreCase)
            : actualName;
    }

    private static void EnsureParentDirectory(string filePath)
    {
        var parent = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(parent) is false)
        {
            Directory.CreateDirectory(parent);
        }
    }
}
