using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;

namespace Picea.Abies.TaskTimer.Native;

public partial class App : Application
{
    public App() => InitializeComponent();

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window();

        var rootHost = new Grid();
        MainWindow.Content = rootHost;
        MainWindow.SetWindowIcon();
        MainWindow.Activate();

        _ = RunAsync(rootHost, MainWindow);

        // ABIES_INTERACT=<path.png> drives the app through a scripted session
        // and exits non-zero if any step fails. Unlike the Counter sample's
        // snapshot, this asserts the app *responds*, not just that it draws.
        if (Environment.GetEnvironmentVariable("ABIES_INTERACT") is { Length: > 0 } path)
            _ = InteractAndExitAsync(rootHost, path);
    }

    private static async Task RunAsync(Grid rootHost, Window window)
    {
        try
        {
            await WinUI.Runtime.RunWithView<TaskTimerProgram, TaskTimerView, TaskTimerModel, Unit>(
                rootHost, window, Unit.Value,
                renderFaulted: ex => Console.Error.WriteLine($"Render fault: {ex}"));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Abies runtime failed: {ex}");
            throw;
        }
    }

    // =========================================================================
    // Scripted interaction
    // =========================================================================
    // Every step goes through real native input: text is written to the actual
    // TextBox (raising TextChanged) and buttons are invoked through their
    // automation peers (raising Click). Nothing calls the patch interpreter or
    // the runtime directly, so a pass means the whole loop works —
    // native event → handler → dispatch → update → diff → patch → control.

    private static async Task InteractAndExitAsync(FrameworkElement root, string snapshotPath)
    {
        var startupDelay = IntFromEnv("ABIES_INTERACT_STARTUP_MS", 8000);
        var tickWindow = IntFromEnv("ABIES_INTERACT_TICK_MS", 3500);

        await Task.Delay(startupDelay);

        // Watchdog: a step that never completes must fail the run rather than
        // hang until the CI job times out.
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(90));
            Console.Error.WriteLine("Interaction watchdog: the script did not finish within 90s.");
            Environment.Exit(2);
        });

        root.DispatcherQueue.TryEnqueue(async void () =>
        {
            try
            {
                await RunScriptAsync(root, tickWindow);
                await SnapshotAsync(root, snapshotPath);
                Console.Out.WriteLine("Interaction script passed.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Interaction script failed: {ex.Message}");
                try
                {
                    await SnapshotAsync(root, snapshotPath);
                }
                catch (Exception snapshotError)
                {
                    Console.Error.WriteLine($"(could not capture failure snapshot: {snapshotError.Message})");
                }
                Environment.Exit(1);
            }
        });
    }

    private static async Task RunScriptAsync(FrameworkElement root, int tickWindow)
    {
        // 1. Two-way input: writing to the real TextBox raises TextChanged.
        var input = FindFirst<TextBox>(root) ?? throw new InvalidOperationException("No TextBox rendered.");
        input.Text = "Write the docs";
        await Settle();

        // 2. Click Add through its automation peer.
        Invoke(FindButton(root, "Add") ?? throw new InvalidOperationException("No Add button rendered."));
        await Settle();

        // The row only exists if DraftChanged and AddTask both round-tripped.
        if (FindTextBlock(root, "Write the docs") is null)
        {
            throw new InvalidOperationException(
                "Adding a task did not reach the view — text input or click dispatch is broken.");
        }

        // 3. Start the timer.
        Invoke(FindButton(root, "Start") ?? throw new InvalidOperationException("No Start button rendered."));
        await Settle();

        if (FindButton(root, "Stop") is null)
        {
            throw new InvalidOperationException("Toggling the task did not re-render the button label.");
        }

        // 4. Let the interval subscription tick. Ticks originate on a threadpool
        //    thread, so a pass also proves dispatcher marshaling works.
        await Task.Delay(tickWindow);

        var elapsed = FindElapsed(root)
            ?? throw new InvalidOperationException("No elapsed label rendered.");
        if (elapsed == 0)
        {
            throw new InvalidOperationException(
                $"Interval subscription produced no ticks in {tickWindow}ms — elapsed is still 0s.");
        }

        Console.Out.WriteLine($"✓ text input, click dispatch and {elapsed} interval tick(s) all round-tripped");
    }

    /// <summary>Yields long enough for a dispatch to complete and re-render.</summary>
    private static Task Settle() => Task.Delay(400);

    private static int IntFromEnv(string name, int fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } raw && int.TryParse(raw, out var v)
            ? v
            : fallback;

    // =========================================================================
    // Visual tree helpers
    // =========================================================================

    private static void Invoke(Button button)
    {
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button)
            ?? throw new InvalidOperationException("No automation peer for the button.");
        var invoke = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider
            ?? throw new InvalidOperationException("Button peer does not support Invoke.");
        invoke.Invoke();
    }

    private static Button? FindButton(DependencyObject root, string content) =>
        Descendants(root).OfType<Button>().FirstOrDefault(b => (b.Content as string) == content);

    private static TextBlock? FindTextBlock(DependencyObject root, string text) =>
        Descendants(root).OfType<TextBlock>().FirstOrDefault(t => t.Text == text);

    /// <summary>Reads the first "Ns" label back as a number.</summary>
    private static int? FindElapsed(DependencyObject root) =>
        Descendants(root)
            .OfType<TextBlock>()
            .Select(t => t.Text)
            .Where(t => t.EndsWith('s') && int.TryParse(t[..^1], out _))
            .Select(t => (int?)int.Parse(t[..^1]))
            .FirstOrDefault();

    private static T? FindFirst<T>(DependencyObject root) where T : DependencyObject =>
        Descendants(root).OfType<T>().FirstOrDefault();

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var nested in Descendants(child))
                yield return nested;
        }
    }

    // =========================================================================
    // Snapshot
    // =========================================================================

    /// <summary>
    /// Must be awaited, never blocked on. This runs on the dispatcher thread and
    /// RenderAsync needs that same thread to complete, so calling
    /// GetAwaiter().GetResult() here deadlocks on Windows App SDK — the Uno Skia
    /// head happens to tolerate it, which is exactly the kind of difference the
    /// Windows CI head exists to catch.
    /// </summary>
    private static async Task SnapshotAsync(FrameworkElement root, string path)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(root);
        var pixels = (await bitmap.GetPixelsAsync()).ToArray();

        if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0 || pixels.Length == 0)
            throw new InvalidOperationException("Empty render target.");

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
    }
}
