namespace Picea.Abies;

public interface Command
{
    sealed record None : Command;

    sealed record Batch(IReadOnlyList<Command> Commands) : Command;
}

public static class Commands
{
    public static Command None => new Command.None();

    public static Command Batch(params Command[] commands) =>
        new Command.Batch(commands);

    public static Command Batch(IReadOnlyList<Command> commands) =>
        new Command.Batch(commands);
}
