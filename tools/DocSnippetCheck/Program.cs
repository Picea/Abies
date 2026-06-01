// DocSnippetCheck — compiles C# fenced code blocks in the docs against the real
// Abies API so doc/code drift is caught in CI.
//
// Doc snippets are mostly *illustrative fragments* that reference example types
// defined in prose (Model, Msg, MyApp, ...) or are bare API signature listings.
// Compiling all of them produces overwhelming false positives, so this tool is
// OPT-IN: a fence is compiled only when the author marks it self-contained, via
// either an info-string token:
//
//     ```csharp compile
//
// or an HTML comment on the line immediately before the fence:
//
//     <!-- doc-check: compile -->
//
// A fence can be force-skipped (even in --all mode) with `ignore`/`no-compile`
// in the info string or `<!-- doc-check: ignore [reason] -->`.
//
// Usage:
//   dotnet run --project tools/DocSnippetCheck                 (CI mode: compile tagged fences)
//   dotnet run --project tools/DocSnippetCheck -- --all        (audit: try to compile every fence)
//   dotnet run --project tools/DocSnippetCheck -- --list       (also print which fences compiled)
//   dotnet run --project tools/DocSnippetCheck -- <root> ...   (scan a different repo root)
//
// Exit code 0 if every checked fence compiled; non-zero if any failed.

using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static class Program
{
    private static int Main(string[] args)
    {
        bool compileAll = args.Contains("--all");
        bool list = args.Contains("--list");
        string? rootArg = args.FirstOrDefault(a => !a.StartsWith("--"));

        string repoRoot = rootArg is not null ? Path.GetFullPath(rootArg) : FindRepoRoot();
        Console.WriteLine($"DocSnippetCheck — repo root: {repoRoot}");
        Console.WriteLine(compileAll
            ? "Mode: --all (auditing every csharp fence)"
            : "Mode: opt-in (compiling fences tagged `compile`)");

        var references = BuildReferences(repoRoot, out string config);
        Console.WriteLine($"Using Abies DLLs from bin/{config}/net10.0/  ({references.Count} references total)");
        var globalUsings = GlobalUsings();
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var files = CollectMarkdownFiles(repoRoot);
        Console.WriteLine($"Scanning {files.Count} markdown files for csharp/cs fences...\n");

        int compiledCount = 0, untagged = 0;
        var compiledList = new List<(string file, int line)>();
        var skipped = new List<(string file, int line, string reason)>();
        var failed = new List<FenceResult>();

        foreach (var file in files)
        {
            string[] lines = File.ReadAllLines(file);
            foreach (var fence in ExtractFences(lines))
            {
                // Force-skip always wins.
                if (fence.SkipReason is { } reason)
                {
                    skipped.Add((Rel(repoRoot, file), fence.StartLine, reason));
                    continue;
                }

                // Opt-in: in CI mode only tagged fences are checked.
                if (!compileAll && !fence.Compile)
                {
                    untagged++;
                    continue;
                }

                var result = TryCompile(fence.Code, references, globalUsings, parseOptions, fence.Index);
                if (result.Success)
                {
                    compiledCount++;
                    compiledList.Add((Rel(repoRoot, file), fence.StartLine));
                }
                else
                {
                    failed.Add(new FenceResult(Rel(repoRoot, file), fence.StartLine, result.Diagnostics, result.WrappingTried));
                }
            }
        }

        // ---- Summary ----
        Console.WriteLine("================ DocSnippetCheck summary ================");

        if (list && compiledList.Count > 0)
        {
            Console.WriteLine($"Compiled OK ({compiledList.Count}):");
            foreach (var (f, ln) in compiledList)
                Console.WriteLine($"  ✓ {f}:{ln}");
            Console.WriteLine();
        }

        if (skipped.Count > 0)
        {
            Console.WriteLine($"Skipped fences ({skipped.Count}) — explicitly opted out:");
            foreach (var (f, ln, reason) in skipped)
                Console.WriteLine($"  - {f}:{ln}  ({reason})");
            Console.WriteLine();
        }

        if (failed.Count > 0)
        {
            Console.WriteLine($"FAILED fences ({failed.Count}):");
            foreach (var fr in failed)
            {
                Console.WriteLine($"\n  ✗ {fr.File}:{fr.StartLine}   (best wrapping: {fr.WrappingTried})");
                foreach (var d in fr.Diagnostics)
                    Console.WriteLine($"      {d}");
            }
            Console.WriteLine();
        }

        if (compileAll)
            Console.WriteLine($"RESULT: {compiledCount} compiled, {skipped.Count} skipped, {failed.Count} failed.");
        else
            Console.WriteLine($"RESULT: {compiledCount} compiled, {untagged} untagged (not checked), {skipped.Count} skipped, {failed.Count} failed.");
        Console.WriteLine("=========================================================");

        if (!compileAll && compiledCount == 0 && failed.Count == 0)
            Console.WriteLine("NOTE: no fences are tagged `compile`. Tag self-contained examples to give this gate teeth.");

        return failed.Count == 0 ? 0 : 1;
    }

    // -------------------------------------------------------------------------
    // Markdown fence extraction
    // -------------------------------------------------------------------------

    private sealed record Fence(string Code, int StartLine, int Index, string? SkipReason, bool Compile);

    private static readonly Regex FenceOpen =
        new(@"^(?<indent>\s*)```(?<info>[^\r\n]*)$", RegexOptions.Compiled);

    // <!-- doc-check: ignore [reason] -->  on the immediately preceding line.
    private static readonly Regex IgnoreComment =
        new(@"<!--\s*doc-check:\s*ignore\b\s*(?<reason>.*?)\s*-->", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // <!-- doc-check: compile -->  on the immediately preceding line.
    private static readonly Regex CompileComment =
        new(@"<!--\s*doc-check:\s*compile\b\s*-->", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static int _fenceCounter;

    private static IEnumerable<Fence> ExtractFences(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var m = FenceOpen.Match(lines[i]);
            if (!m.Success) continue;

            string info = m.Groups["info"].Value.Trim();
            var tokens = info.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) continue;
            string lang = tokens[0].ToLowerInvariant();
            if (lang != "csharp" && lang != "cs") continue;

            int start = i + 1;
            int end = start;
            while (end < lines.Length && !lines[end].TrimStart().StartsWith("```"))
                end++;

            string code = string.Join("\n", lines[start..Math.Min(end, lines.Length)]);

            // Opt-out / opt-in detection.
            string? skipReason = null;
            bool optIn = false;
            var extra = tokens.Skip(1).Select(t => t.ToLowerInvariant()).ToArray();
            if (extra.Any(t => t is "ignore" or "no-compile" or "nocompile"))
                skipReason = $"info-string opt-out (```{info})";
            else if (extra.Contains("compile"))
                optIn = true;

            if (skipReason is null && !optIn && i > 0)
            {
                var im = IgnoreComment.Match(lines[i - 1]);
                if (im.Success)
                {
                    string r = im.Groups["reason"].Value.Trim();
                    skipReason = r.Length > 0 ? r : "doc-check: ignore (no reason given)";
                }
                else if (CompileComment.IsMatch(lines[i - 1]))
                {
                    optIn = true;
                }
            }

            yield return new Fence(code, i + 1, Interlocked.Increment(ref _fenceCounter), skipReason, optIn);
            i = end; // skip past the closing fence
        }
    }

    // -------------------------------------------------------------------------
    // Compilation with layered wrapping
    // -------------------------------------------------------------------------

    private sealed record CompileResult(bool Success, IReadOnlyList<string> Diagnostics, string WrappingTried);

    private sealed record FenceResult(string File, int StartLine, IReadOnlyList<string> Diagnostics, string WrappingTried);

    private static CompileResult TryCompile(
        string snippet,
        IReadOnlyList<MetadataReference> references,
        string globalUsings,
        CSharpParseOptions parseOptions,
        int index)
    {
        // Layered wrappings. Accept the FIRST that compiles with zero errors.
        var wrappings = new (string Name, string Source, OutputKind Kind)[]
        {
            // (a) as-is: full compilation unit OR top-level statements.
            ("as-is (top-level/compilation-unit)", snippet, OutputKind.ConsoleApplication),
            // (b) class member fragment: methods/fields/properties/records.
            ("class-member", $"static class __DocSnippet_{index}\n{{\n{snippet}\n}}", OutputKind.DynamicallyLinkedLibrary),
            // (c) bare expression fragment, e.g. div([...],[...]).
            ("discard-expression", $"static class __DocSnippet_{index}\n{{\n    static readonly object? __e = (object?)(\n{snippet}\n);\n}}", OutputKind.DynamicallyLinkedLibrary),
            // (d) statement fragment(s), e.g. local statements with `await`.
            ("method-body", $"static class __DocSnippet_{index}\n{{\n    static async System.Threading.Tasks.Task __m()\n    {{\n#pragma warning disable\n{snippet}\n#pragma warning restore\n    }}\n}}", OutputKind.DynamicallyLinkedLibrary),
        };

        (string Name, IReadOnlyList<string> Errors) cleanest = ("(none)", Array.Empty<string>());
        bool first = true;

        foreach (var (name, source, kind) in wrappings)
        {
            var errors = CompileOnce(source, globalUsings, references, parseOptions, kind);
            if (errors.Count == 0)
                return new CompileResult(true, Array.Empty<string>(), name);

            if (first || errors.Count < cleanest.Errors.Count)
                cleanest = (name, errors);
            first = false;
        }

        return new CompileResult(false, cleanest.Errors, cleanest.Name);
    }

    private static IReadOnlyList<string> CompileOnce(
        string source,
        string globalUsings,
        IReadOnlyList<MetadataReference> references,
        CSharpParseOptions parseOptions,
        OutputKind outputKind)
    {
        var usingsTree = CSharpSyntaxTree.ParseText(globalUsings, parseOptions);
        var snippetTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var options = new CSharpCompilationOptions(
            outputKind,
            allowUnsafe: true,
            nullableContextOptions: NullableContextOptions.Enable,
            reportSuppressedDiagnostics: false);

        var compilation = CSharpCompilation.Create(
            assemblyName: "DocSnippet_" + Guid.NewGuid().ToString("N"),
            syntaxTrees: new[] { usingsTree, snippetTree },
            references: references,
            options: options);

        return compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Where(d => d.Id != "CS5001") // no Main; irrelevant — we never emit
            .Select(d => $"{d.Id}: {d.GetMessage()}")
            .Distinct()
            .ToList();
    }

    // -------------------------------------------------------------------------
    // References + global usings
    // -------------------------------------------------------------------------

    private static List<MetadataReference> BuildReferences(string repoRoot, out string config)
    {
        var refs = new List<MetadataReference>();

        // .NET 10 reference assemblies (clean, deterministic — preferred over TPA).
        refs.AddRange(Basic.Reference.Assemblies.Net100.References.All);

        config = "Debug";
        string[] abiesAsms =
        {
            "Picea.Abies", "Picea.Abies.Server", "Picea.Abies.Server.Kestrel",
            "Picea.Abies.Browser", "Picea.Abies.UI",
        };

        if (!File.Exists(AbiesDll(repoRoot, "Picea.Abies", "Debug")) &&
            File.Exists(AbiesDll(repoRoot, "Picea.Abies", "Release")))
        {
            config = "Release";
        }

        foreach (var asm in abiesAsms)
        {
            string path = AbiesDll(repoRoot, asm, config);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"Required Abies assembly not found: {path}. " +
                    $"Build the projects first: dotnet build {asm}/{asm}.csproj -c {config}");
            refs.Add(MetadataReference.CreateFromFile(path));
        }

        // Picea kernel DLL from the NuGet cache, version resolved from Picea.Abies.csproj.
        string piceaVer = ResolvePiceaVersion(repoRoot);
        string nugetRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        string piceaDll = Path.Combine(nugetRoot, "picea", piceaVer, "lib", "net10.0", "Picea.dll");
        if (!File.Exists(piceaDll))
            throw new FileNotFoundException($"Picea kernel DLL not found: {piceaDll}");
        refs.Add(MetadataReference.CreateFromFile(piceaDll));

        // Pull in the transitive runtime DLLs the Abies assemblies sit next to
        // (ASP.NET Core etc.), so types they expose in public signatures resolve.
        string binDir = Path.GetDirectoryName(AbiesDll(repoRoot, "Picea.Abies.Server.Kestrel", config))!;
        var have = new HashSet<string>(
            refs.Select(r => Path.GetFileName(((PortableExecutableReference)r).FilePath ?? ""))
                .Where(n => n.Length > 0), StringComparer.OrdinalIgnoreCase);
        foreach (var dll in Directory.EnumerateFiles(binDir, "*.dll"))
        {
            string name = Path.GetFileName(dll);
            if (have.Add(name))
            {
                try { refs.Add(MetadataReference.CreateFromFile(dll)); }
                catch { /* skip native/unmanaged */ }
            }
        }

        return refs;
    }

    private static string AbiesDll(string repoRoot, string asm, string config) =>
        Path.Combine(repoRoot, asm, "bin", config, "net10.0", asm + ".dll");

    private static string ResolvePiceaVersion(string repoRoot)
    {
        string csproj = Path.Combine(repoRoot, "Picea.Abies", "Picea.Abies.csproj");
        var doc = XDocument.Load(csproj);
        var pr = doc.Descendants("PackageReference")
            .FirstOrDefault(e => string.Equals((string?)e.Attribute("Include"), "Picea", StringComparison.OrdinalIgnoreCase));
        string? ver = (string?)pr?.Attribute("Version");
        if (string.IsNullOrWhiteSpace(ver))
            throw new InvalidOperationException("Could not resolve Picea PackageReference version from Picea.Abies.csproj");
        return ver;
    }

    private static string GlobalUsings() =>
        """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Threading.Tasks;
        global using Picea;
        global using Picea.Abies;
        global using Picea.Abies.DOM;
        global using Picea.Abies.Html;
        global using Picea.Abies.Subscriptions;
        global using static Picea.Abies.Html.Elements;
        global using static Picea.Abies.Html.Attributes;
        global using static Picea.Abies.Html.Events;
        """;

    // -------------------------------------------------------------------------
    // File discovery / paths
    // -------------------------------------------------------------------------

    private static List<string> CollectMarkdownFiles(string repoRoot)
    {
        var files = new List<string>();
        string docs = Path.Combine(repoRoot, "docs");
        if (Directory.Exists(docs))
            files.AddRange(Directory.EnumerateFiles(docs, "*.md", SearchOption.AllDirectories));
        string readme = Path.Combine(repoRoot, "README.md");
        if (File.Exists(readme))
            files.Add(readme);
        files.Sort(StringComparer.Ordinal);
        return files;
    }

    private static string Rel(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Picea.Abies.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Picea.Abies.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate repo root (Picea.Abies.sln). Pass it as an argument.");
    }
}
