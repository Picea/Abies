using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;

namespace Picea.Abies.Counter.Native;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window();

        var rootHost = new Grid();
        MainWindow.Content = rootHost;
        MainWindow.SetWindowIcon();
        MainWindow.Activate();

        // The Abies runtime renders into rootHost and keeps running for the
        // window's lifetime; the returned Task only completes on fatal error.
        _ = RunAsync(rootHost, MainWindow);

        // Headless visual check: ABIES_SNAPSHOT=<path.png> rasterizes the tree
        // to a PNG shortly after startup and exits — no OS screen-capture
        // permission involved. Used in CI to prove the renderer actually draws
        // on each head, rather than merely compiling.
        if (Environment.GetEnvironmentVariable("ABIES_SNAPSHOT") is { Length: > 0 } snapshotPath)
            _ = SnapshotAndExitAsync(rootHost, snapshotPath);
    }

    private static async Task SnapshotAndExitAsync(FrameworkElement root, string path)
    {
        // Give the runtime time to start and lay out. Configurable because a
        // cold CI runner is much slower than a warm desktop.
        var delay = Environment.GetEnvironmentVariable("ABIES_SNAPSHOT_DELAY_MS") is { Length: > 0 } raw
            && int.TryParse(raw, out var parsed)
                ? parsed
                : 4000;
        await Task.Delay(delay);

        // Watchdog: without it, a head that never reaches the render callback
        // leaves the process hanging until the CI job times out, with no
        // indication of what went wrong.
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(60));
            Console.Error.WriteLine("Snapshot watchdog: render did not complete within 60s of the delay elapsing.");
            Environment.Exit(2);
        });

        root.DispatcherQueue.TryEnqueue(async void () =>
        {
            try
            {
                var bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(root);
                var pixels = (await bitmap.GetPixelsAsync()).ToArray();

                if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0 || pixels.Length == 0)
                {
                    Console.Error.WriteLine(
                        $"Snapshot failed: empty render target ({bitmap.PixelWidth}x{bitmap.PixelHeight}, {pixels.Length} bytes).");
                    Environment.Exit(1);
                }

                // A uniform image means the window came up but nothing drew —
                // which is exactly the failure a "did it build?" check misses.
                if (IsUniform(pixels))
                {
                    Console.Error.WriteLine(
                        $"Snapshot failed: {bitmap.PixelWidth}x{bitmap.PixelHeight} image is a single flat colour, so no controls rendered.");
                    Environment.Exit(1);
                }

                var info = new SKImageInfo(bitmap.PixelWidth, bitmap.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var skBitmap = new SKBitmap(info);
                Marshal.Copy(pixels, 0, skBitmap.GetPixels(), pixels.Length);
                using var image = SKImage.FromBitmap(skBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                await using (var stream = File.OpenWrite(path))
                {
                    data.SaveTo(stream);
                }

                Console.Out.WriteLine($"Snapshot written: {path} ({bitmap.PixelWidth}x{bitmap.PixelHeight})");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Snapshot failed: {ex}");
                Environment.Exit(1);
            }
        });
    }

    /// <summary>True when every pixel is identical — i.e. nothing was drawn.</summary>
    private static bool IsUniform(byte[] bgra)
    {
        for (var i = 4; i + 3 < bgra.Length; i += 4)
        {
            if (bgra[i] != bgra[0] || bgra[i + 1] != bgra[1] ||
                bgra[i + 2] != bgra[2] || bgra[i + 3] != bgra[3])
            {
                return false;
            }
        }
        return true;
    }

    private static async Task RunAsync(Grid rootHost, Window window)
    {
        try
        {
            await WinUI.Runtime.Run<NativeCounterProgram, CounterModel, Unit>(
                rootHost, window, Unit.Value);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Abies runtime failed: {ex}");
            throw;
        }
    }
}
