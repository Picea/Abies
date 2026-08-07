namespace AbiesApp;

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

        // The Abies runtime renders into rootHost and keeps running for the
        // window's lifetime; the task only completes on a fatal error.
        _ = RunAsync(rootHost, MainWindow);
    }

    private static async Task RunAsync(Grid rootHost, Window window)
    {
        try
        {
            // Core and View are separate, so CounterProgram could just as easily
            // back a browser or server view. Swap CounterView for another
            // ProgramView to render the same program differently.
            await Picea.Abies.WinUI.Runtime.RunWithView<CounterProgram, CounterView, CounterModel, Unit>(
                rootHost, window, Unit.Value,
                renderFaulted: ex => Console.Error.WriteLine($"Render fault: {ex}"));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Abies runtime failed: {ex}");
            throw;
        }
    }
}
