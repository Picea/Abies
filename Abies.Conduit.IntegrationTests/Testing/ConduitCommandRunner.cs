namespace Abies.Conduit.IntegrationTests.Testing;

/// <summary>
/// Deterministic command runner:
/// runs Conduit <see cref="Command"/> by calling the real <see cref="Main.HandleCommand"/>
/// against a fake <see cref="HttpClient"/> (configured via ApiClient).
///
/// It captures all dispatched messages so tests can feed them back into page Update methods.
/// </summary>
internal static class ConduitCommandRunner
{
    public static async Task<IReadOnlyList<Message>> RunAsync(Command command)
    {
        List<Message> dispatched = [];

        await Main.Program.HandleCommand(command, msg =>
            {
                dispatched.Add(msg);
                return default;
            });

        return dispatched;
    }

    public static async Task<IReadOnlyList<Message>> RunAsync(IEnumerable<Command> commands)
    {
        List<Message> dispatched = [];
        foreach (var cmd in commands)
        {
            dispatched.AddRange(await RunAsync(cmd));
        }

        return dispatched;
    }

    public static IEnumerable<Command> Flatten(Command command)
    {
        if (command is Command.None)
        {
            yield break;
        }

        if (command is Command.Batch batch)
        {
            foreach (var item in batch.Commands)
            {
                foreach (var flat in Flatten(item))
                {
                    yield return flat;
                }
            }

            yield break;
        }

        yield return command;
    }
}
