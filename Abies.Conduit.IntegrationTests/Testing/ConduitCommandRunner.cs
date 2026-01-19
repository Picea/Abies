using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Abies.Conduit.IntegrationTests.Testing;

/// <summary>
/// Deterministic command runner:
/// runs Conduit <see cref="Abies.Command"/> by calling the real <see cref="Abies.Conduit.Main.HandleCommand"/>
/// against a fake <see cref="System.Net.Http.HttpClient"/> (configured via ApiClient).
///
/// It captures all dispatched messages so tests can feed them back into page Update methods.
/// </summary>
internal static class ConduitCommandRunner
{
    public static async Task<IReadOnlyList<Abies.Message>> RunAsync(Abies.Command command)
    {
        var dispatched = new List<Abies.Message>();

    await Abies.Conduit.Main.Program.HandleCommand(command, msg =>
        {
            dispatched.Add(msg);
            return default;
        });

        return dispatched;
    }

    public static async Task<IReadOnlyList<Abies.Message>> RunAsync(IEnumerable<Abies.Command> commands)
    {
        var dispatched = new List<Abies.Message>();
        foreach (var cmd in commands)
        {
            dispatched.AddRange(await RunAsync(cmd));
        }

        return dispatched;
    }

    public static IEnumerable<Abies.Command> Flatten(Abies.Command command)
    {
        if (command is Abies.Command.None)
            yield break;

        if (command is Abies.Command.Batch batch)
        {
            foreach (var item in batch.Commands)
            foreach (var flat in Flatten(item))
                yield return flat;
            yield break;
        }

        yield return command;
    }
}
