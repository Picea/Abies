using Microsoft.Playwright;
using Picea.Abies.DOM;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Picea.Abies.Testing;

/// <summary>
/// Result for a visual comparison operation.
/// </summary>
public sealed record VisualComparisonResult(
    bool IsMatch,
    long PixelErrorCount,
    double PixelErrorPercentage,
    double MeanError,
    double AbsoluteError,
    string BaselinePath,
    string? ActualPath,
    string? DiffPath,
    byte[] BaselineBytes,
    byte[] ActualBytes,
    byte[]? DiffBytes,
    bool BaselineCreated);

/// <summary>
/// Tolerance configuration for visual assertions.
/// </summary>
public sealed record VisualComparisonTolerance(
    int MaxPixelErrorCount = 0,
    double MaxPixelErrorPercentage = 0,
    double MaxMeanError = 0,
    double MaxAbsoluteError = 0,
    byte PerChannelThreshold = 0)
{
    /// <summary>
    /// Strict tolerance where every pixel channel must match exactly.
    /// </summary>
    public static VisualComparisonTolerance Strict { get; } = new();
}

/// <summary>
/// Options for capturing and storing visual comparison artifacts.
/// </summary>
public sealed record VisualComparisonOptions(
    int ViewportWidth = 1280,
    int ViewportHeight = 720,
    bool FullPage = true,
    string? ArtifactDirectory = null,
    VisualComparisonTolerance? Tolerance = null,
    bool BlockExternalResources = true)
{
    /// <summary>
    /// Resolve tolerance to the strict default when not explicitly configured.
    /// </summary>
    public VisualComparisonTolerance ResolvedTolerance => Tolerance ?? VisualComparisonTolerance.Strict;
}

/// <summary>
/// Extension methods for visual comparison and assertion workflows.
/// </summary>
public static class TestHarnessVisualExtensions
{
    extension<TProgram, TModel, TArgument>(TestHarness<TProgram, TModel, TArgument> harness)
        where TProgram : Program<TModel, TArgument>
    {
        /// <summary>
        /// Renders the current model using <typeparamref name="TProgram"/>, captures a screenshot with Playwright,
        /// and compares it against a baseline image.
        /// </summary>
        /// <param name="page">Playwright page used for rendering and screenshot capture.</param>
        /// <param name="baselinePath">Baseline image path.</param>
        /// <param name="options">Optional visual comparison options.</param>
        /// <returns>Computed visual comparison result.</returns>
        public async Task<VisualComparisonResult> CompareVisual(
            IPage page,
            string baselinePath,
            VisualComparisonOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(harness);
            ArgumentNullException.ThrowIfNull(page);
            ValidateBaselinePath(baselinePath);

            var resolvedOptions = options ?? new VisualComparisonOptions();
            ValidateOptions(resolvedOptions);

            var document = TProgram.View(harness.Model);
            var htmlContent = RenderDocumentHtml(document, resolvedOptions.BlockExternalResources);

            await page.SetViewportSizeAsync(resolvedOptions.ViewportWidth, resolvedOptions.ViewportHeight);

            await page.SetContentAsync(htmlContent, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            var actualBytes = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Type = ScreenshotType.Png,
                FullPage = resolvedOptions.FullPage
            });

            return harness.CompareVisual(
                actualBytes,
                baselinePath,
                resolvedOptions);
        }

        /// <summary>
        /// Compares a screenshot buffer against a baseline image file.
        /// </summary>
        /// <param name="actualScreenshot">Screenshot bytes in PNG format.</param>
        /// <param name="baselinePath">Baseline image path.</param>
        /// <param name="options">Optional visual comparison options.</param>
        /// <returns>Computed visual comparison result.</returns>
        public VisualComparisonResult CompareVisual(
            byte[] actualScreenshot,
            string baselinePath,
            VisualComparisonOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(harness);
            ArgumentNullException.ThrowIfNull(actualScreenshot);
            ValidateBaselinePath(baselinePath);

            var resolvedOptions = options ?? new VisualComparisonOptions();
            ValidateOptions(resolvedOptions);
            var tolerance = resolvedOptions.ResolvedTolerance;

            EnsureParentDirectory(baselinePath);

            if (File.Exists(baselinePath) is false)
            {
                File.WriteAllBytes(baselinePath, actualScreenshot);
                return new VisualComparisonResult(
                    IsMatch: true,
                    PixelErrorCount: 0,
                    PixelErrorPercentage: 0,
                    MeanError: 0,
                    AbsoluteError: 0,
                    BaselinePath: baselinePath,
                    ActualPath: null,
                    DiffPath: null,
                    BaselineBytes: actualScreenshot,
                    ActualBytes: actualScreenshot,
                    DiffBytes: null,
                    BaselineCreated: true);
            }

            var baselineBytes = File.ReadAllBytes(baselinePath);
            var metrics = CompareImages(baselineBytes, actualScreenshot, tolerance.PerChannelThreshold);

            var isMatch =
                metrics.PixelErrorCount <= tolerance.MaxPixelErrorCount &&
                metrics.PixelErrorPercentage <= tolerance.MaxPixelErrorPercentage &&
                metrics.MeanError <= tolerance.MaxMeanError &&
                metrics.AbsoluteError <= tolerance.MaxAbsoluteError;

            var (actualPath, diffPath, diffBytes) = isMatch
                ? (null, null, null)
                : WriteMismatchArtifacts(baselinePath, resolvedOptions, actualScreenshot, metrics.DiffBytes);

            return new VisualComparisonResult(
                IsMatch: isMatch,
                PixelErrorCount: metrics.PixelErrorCount,
                PixelErrorPercentage: metrics.PixelErrorPercentage,
                MeanError: metrics.MeanError,
                AbsoluteError: metrics.AbsoluteError,
                BaselinePath: baselinePath,
                ActualPath: actualPath,
                DiffPath: diffPath,
                BaselineBytes: baselineBytes,
                ActualBytes: actualScreenshot,
                DiffBytes: diffBytes,
                BaselineCreated: false);
        }

        /// <summary>
        /// Asserts visual match for the harness using Playwright screenshot capture.
        /// </summary>
        /// <param name="page">Playwright page used for rendering and screenshot capture.</param>
        /// <param name="baselinePath">Baseline image path.</param>
        /// <param name="options">Optional visual comparison options.</param>
        public async Task AssertVisualMatch(
            IPage page,
            string baselinePath,
            VisualComparisonOptions? options = null)
        {
            var result = await harness.CompareVisual(page, baselinePath, options);
            ThrowWhenMismatch(result);
        }

        /// <summary>
        /// Asserts visual match for an existing screenshot buffer.
        /// </summary>
        /// <param name="actualScreenshot">Screenshot bytes in PNG format.</param>
        /// <param name="baselinePath">Baseline image path.</param>
        /// <param name="options">Optional visual comparison options.</param>
        public void AssertVisualMatch(
            byte[] actualScreenshot,
            string baselinePath,
            VisualComparisonOptions? options = null)
        {
            var result = harness.CompareVisual(actualScreenshot, baselinePath, options);
            ThrowWhenMismatch(result);
        }
    }

    private static void ThrowWhenMismatch(VisualComparisonResult result)
    {
        if (result.IsMatch)
        {
            return;
        }

        var message = string.Create(CultureInfo.InvariantCulture, $"""
            Visual comparison failed.
            Baseline: {result.BaselinePath}
            Actual: {result.ActualPath}
            Diff: {result.DiffPath}
            PixelErrorCount: {result.PixelErrorCount}
            PixelErrorPercentage: {result.PixelErrorPercentage:F6}
            MeanError: {result.MeanError:F6}
            AbsoluteError: {result.AbsoluteError:F6}
            """);

        throw new InvalidOperationException(message);
    }

    private static string RenderDocumentHtml(Document document, bool blockExternalResources)
    {
        var head = string.Concat(document.Head.Select(content => content.ToHtml()));
        if (blockExternalResources)
        {
            head = StripExternalResourceTags(head);
        }
        var body = Render.Html(document.Body);
                var encodedTitle = WebUtility.HtmlEncode(document.Title);

        return $"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8" />
                            <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{encodedTitle}</title>
              {head}
            </head>
            <body>
              {body}
            </body>
            </html>
            """;
    }

    private static string StripExternalResourceTags(string headHtml)
    {
        if (string.IsNullOrWhiteSpace(headHtml))
        {
            return headHtml;
        }

        if (Regex.IsMatch(headHtml, "https?://", RegexOptions.IgnoreCase) is false)
        {
            return headHtml;
        }

        var strippedLinkTags = Regex.Replace(
            headHtml,
            "<link\\b[^>]*href\\s*=\\s*['\"][^'\"]*https?://[^'\"]*['\"][^>]*>",
            string.Empty,
            RegexOptions.IgnoreCase);

        var strippedScriptTags = Regex.Replace(
            strippedLinkTags,
            "<script\\b[^>]*src\\s*=\\s*['\"][^'\"]*https?://[^'\"]*['\"][^>]*>\\s*</script>",
            string.Empty,
            RegexOptions.IgnoreCase);

        return strippedScriptTags;
    }

    private static void ValidateBaselinePath(string baselinePath)
    {
        if (string.IsNullOrWhiteSpace(baselinePath))
        {
            throw new ArgumentException("Baseline path cannot be empty.", nameof(baselinePath));
        }
    }

    private static void ValidateOptions(VisualComparisonOptions options)
    {
        if (options.ViewportWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Viewport width must be greater than zero.");
        }

        if (options.ViewportHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Viewport height must be greater than zero.");
        }

        var tolerance = options.ResolvedTolerance;
        if (tolerance.MaxPixelErrorCount < 0 ||
            tolerance.MaxPixelErrorPercentage < 0 ||
            tolerance.MaxPixelErrorPercentage > 1 ||
            tolerance.MaxMeanError < 0 ||
            tolerance.MaxAbsoluteError < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Tolerance values must be valid. MaxPixelErrorPercentage must be in the [0, 1] range.");
        }
    }

    private static void EnsureParentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
    }

    private static (string actualPath, string diffPath, byte[] diffBytes) WriteMismatchArtifacts(
        string baselinePath,
        VisualComparisonOptions options,
        byte[] actualBytes,
        byte[] diffBytes)
    {
        var artifactDirectory = ResolveArtifactDirectory(options.ArtifactDirectory);
        Directory.CreateDirectory(artifactDirectory);

        var artifactStem = BuildArtifactStem(baselinePath);
        var actualPath = Path.Combine(artifactDirectory, $"{artifactStem}.actual.png");
        var diffPath = Path.Combine(artifactDirectory, $"{artifactStem}.diff.png");

        File.WriteAllBytes(actualPath, actualBytes);
        File.WriteAllBytes(diffPath, diffBytes);

        return (actualPath, diffPath, diffBytes);
    }

    private static string ResolveArtifactDirectory(string? artifactDirectory)
    {
        if (string.IsNullOrWhiteSpace(artifactDirectory) is false)
        {
            return artifactDirectory;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "visual");
    }

    private static string BuildArtifactStem(string baselinePath)
    {
        var fullPathWithoutExtension = Path.GetFullPath(Path.ChangeExtension(baselinePath, null) ?? baselinePath);
        var cwd = Path.GetFullPath(Directory.GetCurrentDirectory());
        var relativeOrFull = Path.GetRelativePath(cwd, fullPathWithoutExtension);

        var chars = relativeOrFull
            .Select(ch =>
            {
                if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar)
                {
                    return '_';
                }

                return Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch;
            })
            .ToArray();

        var stem = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(stem) ? "snapshot" : stem;
    }

    private static VisualMetrics CompareImages(byte[] baselinePng, byte[] actualPng, byte perChannelThreshold)
    {
        using var baselineImage = Image.Load<Rgba32>(baselinePng);
        using var actualImage = Image.Load<Rgba32>(actualPng);

        if (baselineImage.Width != actualImage.Width || baselineImage.Height != actualImage.Height)
        {
            throw new InvalidOperationException($"Visual comparison requires equal image dimensions. Baseline: {baselineImage.Width}x{baselineImage.Height}, Actual: {actualImage.Width}x{actualImage.Height}.");
        }

        var width = baselineImage.Width;
        var height = baselineImage.Height;
        var totalPixels = (long)width * height;
        using var diffImage = new Image<Rgba32>(width, height);

        long pixelErrorCount = 0;
        double absoluteErrorTotal = 0;
        double meanErrorTotal = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var baselinePixel = baselineImage[x, y];
                var actualPixel = actualImage[x, y];

                var dr = Math.Abs(actualPixel.R - baselinePixel.R);
                var dg = Math.Abs(actualPixel.G - baselinePixel.G);
                var db = Math.Abs(actualPixel.B - baselinePixel.B);
                var da = Math.Abs(actualPixel.A - baselinePixel.A);

                var hasError =
                    dr > perChannelThreshold ||
                    dg > perChannelThreshold ||
                    db > perChannelThreshold ||
                    da > perChannelThreshold;

                if (hasError)
                {
                    pixelErrorCount++;
                }

                var absolute = (dr + dg + db + da) / (4d * 255d);
                var mean = Math.Sqrt((dr * dr + dg * dg + db * db + da * da) / (4d * 255d * 255d));

                absoluteErrorTotal += absolute;
                meanErrorTotal += mean;

                diffImage[x, y] = hasError ? new Rgba32(255, 0, 0, 255) : baselinePixel;
            }
        }

        using var diffStream = new MemoryStream();
        diffImage.SaveAsPng(diffStream);

        var pixelErrorPercentage = totalPixels == 0
            ? 0
            : pixelErrorCount / (double)totalPixels;

        var meanError = totalPixels == 0
            ? 0
            : meanErrorTotal / totalPixels;

        var absoluteError = totalPixels == 0
            ? 0
            : absoluteErrorTotal / totalPixels;

        return new VisualMetrics(
            pixelErrorCount,
            pixelErrorPercentage,
            meanError,
            absoluteError,
            diffStream.ToArray());
    }

    private sealed record VisualMetrics(
        long PixelErrorCount,
        double PixelErrorPercentage,
        double MeanError,
        double AbsoluteError,
        byte[] DiffBytes);
}