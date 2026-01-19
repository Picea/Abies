using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Abies.Conduit.IntegrationTests.Testing;

/// <summary>
/// Deterministic MVU "mini runtime" for tests:
/// - Apply an initial interaction message (from DOM harness) to Update, capturing its Command.
/// - Execute produced command(s) through the real Conduit command handler.
/// - Feed dispatched messages back into Update.
/// - Repeat until no commands/messages remain or a max-step limit is exceeded.
///
/// Design goals (avoid false positives):
/// - Hard step limits.
/// - Strict mode can fail if any dispatched message isn't handled by Update (i.e. yields Commands.None and leaves model unchanged).
/// - No clock/timeouts; pure iteration.
/// </summary>
internal static class MvuLoopRuntime
{
    public sealed record Options(
        int MaxIterations = 25,
        bool StrictUnhandledMessages = true,
        bool RequireQuiescence = true);

    public sealed record StepLog(
        int Iteration,
        Abies.Message? AppliedMessage,
        int CommandsExecuted,
        int MessagesDispatched);

    public sealed record RunResult<TModel>(
        TModel Model,
        IReadOnlyList<StepLog> Steps,
        int TotalCommandsExecuted,
        int TotalMessagesDispatched);

    public static async Task<RunResult<TModel>> RunUntilQuiescentAsync<TModel>(
        TModel initialModel,
        Func<Abies.Message, TModel, (TModel model, Abies.Command command)> update,
    Abies.Message initialMessage,
        Options? options = null)
    {
        options ??= new Options();

        var model = initialModel;
        List<StepLog> steps = [];

        var pendingMessages = new Queue<Abies.Message>();
        pendingMessages.Enqueue(initialMessage);

        var pendingCommands = new Queue<Abies.Command>();

        var totalCommandsExecuted = 0;
        var totalMessagesDispatched = 0;

    for (var iteration = 1; iteration <= options.MaxIterations; iteration++)
        {
            Abies.Message? applied = null;

            // 1) Apply exactly one pending UI/dispatched message (if any)
            if (pendingMessages.TryDequeue(out var msg))
            {
                applied = msg;

                var before = model;
                var (nextModel, cmd) = update(msg, model);
                model = nextModel;

                // Treat the returned command as work to do
                if (cmd is not Abies.Command.None)
                {
                    foreach (var flat in ConduitCommandRunner.Flatten(cmd))
                        pendingCommands.Enqueue(flat);
                }

                if (options.StrictUnhandledMessages && cmd is Abies.Command.None && Equals(before, model))
                {
                    throw new InvalidOperationException(
                        $"Unhandled message: {msg.GetType().FullName}. " +
                        "Update returned Commands.None and did not change the model.");
                }
            }

            // 2) Execute all accumulated commands and enqueue their dispatched messages
            var executedThisIter = 0;
            var dispatchedThisIter = 0;
            while (pendingCommands.TryDequeue(out var cmd))
            {
                executedThisIter++;
                totalCommandsExecuted++;

                var dispatched = await ConduitCommandRunner.RunAsync(cmd);
                dispatchedThisIter += dispatched.Count;
                totalMessagesDispatched += dispatched.Count;

                foreach (var d in dispatched)
                    pendingMessages.Enqueue(d);
            }

            steps.Add(new StepLog(iteration, applied, executedThisIter, dispatchedThisIter));

            // 3) Quiescence condition: no more messages and no more commands.
            if (pendingMessages.Count == 0 && pendingCommands.Count == 0)
            {
                return new RunResult<TModel>(model, steps, totalCommandsExecuted, totalMessagesDispatched);
            }
        }

        if (options.RequireQuiescence)
        {
            var pendingTypes = pendingMessages
                .Select(m => m.GetType().FullName ?? m.GetType().Name)
                .Distinct()
                .Take(5)
                .ToArray();
            throw new TimeoutException(
                $"MVU loop did not reach quiescence within {options.MaxIterations} iterations. " +
                $"PendingMessages={pendingMessages.Count}, PendingCommands={pendingCommands.Count}. " +
                $"PendingMessageTypes=[{string.Join(", ", pendingTypes)}]");
        }

        return new RunResult<TModel>(model, steps, totalCommandsExecuted, totalMessagesDispatched);
    }
}
