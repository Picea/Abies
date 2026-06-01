namespace Picea.Abies.Cli;

public static class Program
{
    public static Task<int> Main(string[] args) =>
        VisualCliApp.InvokeAsync(args);
}
