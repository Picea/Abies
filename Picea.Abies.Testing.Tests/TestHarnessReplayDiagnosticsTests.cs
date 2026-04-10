using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using System.Text.Json;

namespace Picea.Abies.Testing.Tests;

public sealed class TestHarnessReplayDiagnosticsTests
{
    [Test]
    public async Task ExportReplaySession_CapturesDispatchAndReplayMessages()
    {
        var harness = TestHarness<ReplayProgram, ReplayModel, Unit>.Create(Unit.Value);
        harness.Dispatch(new Add(2));
        harness.Dispatch(new Add(3));
        harness.LoadAndReplaySession(BuildReplaySessionJson(5), MapReplayEntryToMessage);

        var exported = harness.ExportReplaySession(
            MapHistoryEntryToReplayPayload,
            metadata: CreateMetadata("export-session-1"));

        await Assert.That(exported.SchemaVersion).IsEqualTo(TestHarnessReplaySchema.Version1);
        await Assert.That(exported.Entries.Count).IsEqualTo(3);
        await Assert.That(ParseAddPayload(exported.Entries[0].Payload)).IsEqualTo(new Add(2));
        await Assert.That(ParseAddPayload(exported.Entries[1].Payload)).IsEqualTo(new Add(3));
        await Assert.That(ParseAddPayload(exported.Entries[2].Payload)).IsEqualTo(new Add(5));
        await Assert.That(harness.MessageHistory[0].Source).IsEqualTo(TestHarnessMessageSource.Dispatch);
        await Assert.That(harness.MessageHistory[1].Source).IsEqualTo(TestHarnessMessageSource.Dispatch);
        await Assert.That(harness.MessageHistory[2].Source).IsEqualTo(TestHarnessMessageSource.Replay);
    }

    [Test]
    public async Task ExportReplaySessionJson_ProducesLoadableSession()
    {
        var harness = TestHarness<ReplayProgram, ReplayModel, Unit>.Create(Unit.Value);
        harness.Dispatch(new Add(7));

        var sessionJson = harness.ExportReplaySessionJson(
            MapHistoryEntryToReplayPayload,
            metadata: CreateMetadata("export-json-session"));

        var loaded = TestHarness<ReplayProgram, ReplayModel, Unit>.LoadReplaySession(sessionJson);

        await Assert.That(loaded.Metadata.SessionId).IsEqualTo("export-json-session");
        await Assert.That(loaded.Entries.Count).IsEqualTo(1);
        await Assert.That(ParseAddPayload(loaded.Entries[0].Payload)).IsEqualTo(new Add(7));
    }

    [Test]
    public async Task ReplaySession_RecordsReplayMetadataHistory()
    {
        var harness = TestHarness<ReplayProgram, ReplayModel, Unit>.Create(Unit.Value);

        harness.LoadAndReplaySession(BuildReplaySessionJson(1, 2), MapReplayEntryToMessage);

        await Assert.That(harness.ReplayMetadataHistory.Count).IsEqualTo(1);
        await Assert.That(harness.ReplayMetadataHistory[0].SessionId).IsEqualTo("replay-session");
        await Assert.That(harness.ReplayMetadataHistory[0].ProgramName).IsEqualTo(nameof(ReplayProgram));
    }

    [Test]
    public async Task DiagnosticsHistory_TracksMessagesDecisionsAndCommands()
    {
        var harness = TestHarness<ReplayProgram, ReplayModel, Unit>.Create(Unit.Value);
        harness.MockCommand<AddFromCommand>(command => new Add(command.Delta));

        harness.Dispatch(new QueueAdd(4));
        harness.DrainCommands();

        await Assert.That(harness.MessageHistory.Count).IsEqualTo(2);
        await Assert.That(harness.MessageHistory[0].Message).IsTypeOf<QueueAdd>();
        await Assert.That(harness.MessageHistory[1].Message).IsTypeOf<Add>();

        await Assert.That(harness.DecisionHistory.Count).IsEqualTo(2);
        await Assert.That(harness.DecisionHistory[0].TriggerMessage).IsTypeOf<QueueAdd>();
        await Assert.That(harness.DecisionHistory[0].OutputMessage).IsTypeOf<QueueAdd>();
        await Assert.That(harness.DecisionHistory[0].IsError).IsFalse();
        await Assert.That(harness.DecisionHistory[1].TriggerMessage).IsTypeOf<Add>();
        await Assert.That(harness.DecisionHistory[1].OutputMessage).IsTypeOf<Add>();
        await Assert.That(harness.DecisionHistory[1].IsError).IsFalse();

        await Assert.That(harness.CommandHistory.Count).IsEqualTo(2);
        await Assert.That(harness.CommandHistory[0].Command).IsTypeOf<AddFromCommand>();
        await Assert.That(harness.CommandHistory[0].Stage).IsEqualTo(TestHarnessCommandStage.Enqueued);
        await Assert.That(harness.CommandHistory[1].Command).IsTypeOf<AddFromCommand>();
        await Assert.That(harness.CommandHistory[1].Stage).IsEqualTo(TestHarnessCommandStage.Dequeued);
    }

    private static string BuildReplaySessionJson(params int[] deltas)
    {
        var entries = deltas
            .Select((delta, index) => new TestHarnessReplayEntryV1
            {
                Sequence = index,
                MessageType = nameof(Add),
                Payload = JsonSerializer.SerializeToElement(new AddPayload(delta))
            })
            .ToArray();

        var session = new TestHarnessReplaySessionV1
        {
            SchemaVersion = TestHarnessReplaySchema.Version1,
            Metadata = new TestHarnessReplayMetadataV1
            {
                SessionId = "replay-session",
                ProgramName = nameof(ReplayProgram),
                ProgramVersion = "1.0.0",
                RecordedAtUnixMs = 1
            },
            Entries = entries
        };

        return TestHarness<ReplayProgram, ReplayModel, Unit>.SerializeReplaySession(session);
    }

    private static Message MapReplayEntryToMessage(TestHarnessReplayEntryV1 entry)
    {
        if (entry.MessageType != nameof(Add))
        {
            throw new InvalidOperationException($"Unsupported replay message type: {entry.MessageType}");
        }

        return ParseAddPayload(entry.Payload);
    }

    private static JsonElement MapHistoryEntryToReplayPayload(TestHarnessMessageHistoryEntry entry) =>
        entry.Message switch
        {
            Add add => JsonSerializer.SerializeToElement(new AddPayload(add.Delta)),
            _ => throw new InvalidOperationException($"Unsupported message for replay export: {entry.MessageType}")
        };

    private static Add ParseAddPayload(JsonElement payload)
    {
        if (payload.TryGetProperty("delta", out var deltaProperty) is false &&
            payload.TryGetProperty("Delta", out deltaProperty) is false)
        {
            throw new InvalidOperationException("Replay payload is invalid: delta is required.");
        }

        return new Add(deltaProperty.GetInt32());
    }

    private static TestHarnessReplayMetadataV1 CreateMetadata(string sessionId) =>
        new()
        {
            SessionId = sessionId,
            ProgramName = nameof(ReplayProgram),
            ProgramVersion = "1.0.0",
            RecordedAtUnixMs = 1
        };

    private sealed record ReplayModel(int Value);

    private interface ReplayMessage : Message;

    private sealed record Add(int Delta) : ReplayMessage;

    private sealed record QueueAdd(int Delta) : ReplayMessage;

    private sealed record AddPayload(int Delta);

    private sealed record CommandRejected(string Reason) : ReplayMessage;

    private sealed record AddFromCommand(int Delta) : Command;

    private sealed class ReplayProgram : Program<ReplayModel, Unit>
    {
        public static (ReplayModel, Command) Initialize(Unit argument) =>
            (new ReplayModel(0), Commands.None);

        public static (ReplayModel, Command) Transition(ReplayModel model, Message message) =>
            message switch
            {
                Add add => (model with { Value = model.Value + add.Delta }, Commands.None),
                QueueAdd queue => (model, new AddFromCommand(queue.Delta)),
                CommandRejected => (model, Commands.None),
                _ => (model, Commands.None)
            };

        public static Result<Message[], Message> Decide(ReplayModel state, Message command) =>
            command switch
            {
                Add add => Result<Message[], Message>.Ok([add]),
                QueueAdd queue => Result<Message[], Message>.Ok([queue]),
                _ => Result<Message[], Message>.Err(new CommandRejected($"Unsupported command: {command.GetType().Name}"))
            };

        public static bool IsTerminal(ReplayModel state) => false;

        public static Document View(ReplayModel model) =>
            new("Replay", Html.Elements.div([], []));

        public static Subscription Subscriptions(ReplayModel model) =>
            new Subscription.None();
    }
}
