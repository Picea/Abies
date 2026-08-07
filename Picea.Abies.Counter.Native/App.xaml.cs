using Microsoft.UI.Xaml.Media.Imaging;
using Picea;
using SkiaSharp;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Resizetizer;

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

        // Headless visual check: ABIES_SNAPSHOT=<path.png> renders the tree
        // to a PNG shortly after startup and exits — no OS screen-capture
        // permission involved.
        if (Environment.GetEnvironmentVariable("ABIES_SNAPSHOT") is { Length: > 0 } snapshotPath)
            _ = SnapshotAndExitAsync(rootHost, snapshotPath);
    }

    private static async Task SnapshotAndExitAsync(FrameworkElement root, string path)
    {
        await Task.Delay(4000);
        root.DispatcherQueue.TryEnqueue(async void () =>
        {
            try
            {
                var bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(root);
                var pixels = (await bitmap.GetPixelsAsync()).ToArray();

                var info = new SKImageInfo(bitmap.PixelWidth, bitmap.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var skBitmap = new SKBitmap(info);
                Marshal.Copy(pixels, 0, skBitmap.GetPixels(), pixels.Length);
                using var image = SKImage.FromBitmap(skBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                await using var stream = File.OpenWrite(path);
                data.SaveTo(stream);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Snapshot failed: {ex}");
                Environment.Exit(1);
            }
        });
    }

    private static async Task RunAsync(Grid rootHost, Window window)
    {
        try
        {
            await global::Picea.Abies.WinUI.Runtime.Run<NativeCounterProgram, CounterModel, Unit>(
                rootHost, window, Unit.Value);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Abies runtime failed: {ex}");
            throw;
        }
    }
}
