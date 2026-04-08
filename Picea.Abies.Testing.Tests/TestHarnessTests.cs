using System.Text.Json;
using System.Text.Json.Nodes;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;

namespace Picea.Abies.Testing.Tests;

public sealed class TestHarnessTests
{
    [Test]
    public async Task Dispatch_AppliesDecidedEventsAndTransitionsModel()
    {
        var harness = TestHarness<TestProgram, TestModel, Unit>.Create(Unit.Value);

        harness.Dispatch(new Add(3));

        await Assert.That(harness.Model.Value).IsEqualTo(3);
    }

    [Test]
    public async Task DispatchMany_AppliesMessagesInOrder()
    {
        var harness = TestHarness<TestProgram, TestModel, Unit>.Create(Unit.Value);

        harness.DispatchMany([
            new Add(2),
            new Add(5),
            new Add(-1)
        ]);

        await Assert.That(harness.Model.Value).IsEqualTo(6);
    }

    [Test]
    public async Task DrainCommands_UsesTypedMocksAndClearsQueue()
    {
        var harness = TestHarness<TestProgram, TestModel, Unit>.Create(Unit.Value);
        harness.MockCommand<AddFromCommand>(command => new Add(command.Delta));

        harness.Dispatch(new QueueAdd(4));
        await Assert.That(harness.PendingCommandCount).IsEqualTo(1);

        harness.DrainCommands();

        await Assert.That(harness.Model.Value).IsEqualTo(4);
        await Assert.That(harness.PendingCommandCount).IsEqualTo(0);
    }

    [Test]
    public async Task DrainCommands_ThrowsWhenMockMissing()
    {
        var harness = TestHarness<TestProgram, TestModel, Unit>.Create(Unit.Value);

        harness.Dispatch(new QueueAdd(1));

        var act = () => harness.DrainCommands();
        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DrainCommands_UsesDeterministicIterationGuard()
    {
        var harness = TestHarness<TestProgram, TestModel, Unit>.Create(
            Unit.Value,
            new TestHarnessOptions(MaxTransitions: 100, MaxDrainIterations: 3));

        harness.MockCommand<LoopCommand>(_ => new QueueLoop());
        harness.Dispatch(new StartLoop());

        var act = () => harness.DrainCommands();
        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DispatchMany_ThrowsWhenMessagesIsNull()
    {
        var harness = TestHarness<TestProgram, TestModel, Unit>.Create(Unit.Value);

        var act = () => harness.DispatchMany(null!);
        await Assert.That(act).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task MockCommand_SingleMessageOverload_ThrowsWhenNull()
    {
        var harness = TestHarness<TestProgram, TestModel, Unit>.Create(Unit.Value);

        var act = () => harness.MockCommand((Func<AddFromCommand, Message>)null!);
        await Assert.That(act).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Dispatch_UsesTransitionGuardForNonTerminatingFlow()
    {
        var harness = TestHarness<TestProgram, TestModel, Unit>.Create(
            Unit.Value,
            new TestHarnessOptions(MaxTransitions: 2, MaxDrainIterations: 100));

        harness.MockCommand<LoopCommand>(_ => [new QueueLoop()]);
        harness.Dispatch(new StartLoop());

        var act = () => harness.DrainCommands();
        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Create_ThrowsWhenOptionsInvalid()
    {
        var act = () => TestHarness<TestProgram, TestModel, Unit>.Create(
            Unit.Value,
            new TestHarnessOptions(MaxTransitions: 0, MaxDrainIterations: 1));

        await Assert.That(act).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task LoadAndReplaySession_AppliesMessagesInOrder()
    {
        var harness = TestHarness<TestProgram, TestModel, Unit>.Create(Unit.Value);
        var sessionJson = BuildReplaySessionJson(2, 5, -1);

        harness.LoadAndReplaySession(sessionJson, MapReplayEntryToMessage);

        await Assert.That(harness.Model.Value).IsEqualTo(6);
    }

    [Test]
    public async Task LoadReplaySession_RejectsUnknownSchemaVersion()
    {
        var replayDocument = JsonNode.Parse(BuildReplaySessionJson(1))!.AsObject();
        replayDocument["schemaVersion"] = "v2";

        var act = () => TestHarness<TestProgram, TestModel, Unit>.LoadReplaySession(replayDocument.ToJsonString());

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task LoadReplaySession_RejectsMalformedPayloadEntries()
    {
        var replayDocument = JsonNode.Parse(BuildReplaySessionJson(1))!.AsObject();
        var firstEntry = replayDocument["entries"]!.AsArray()[0]!.AsObject();
        firstEntry["payload"] = "invalid";

        var act = () => TestHarness<TestProgram, TestModel, Unit>.LoadReplaySession(replayDocument.ToJsonString());

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task LoadReplaySession_RejectsMalformedJson()
    {
        var act = () => TestHarness<TestProgram, TestModel, Unit>.LoadReplaySession("{");

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task LoadReplaySession_RejectsNullMetadata()
    {
        var replayDocument = JsonNode.Parse(BuildReplaySessionJson(1))!.AsObject();
        replayDocument["metadata"] = null;

        var act = () => TestHarness<TestProgram, TestModel, Unit>.LoadReplaySession(replayDocument.ToJsonString());

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task LoadReplaySession_RejectsNullEntry()
    {
        var replayDocument = JsonNode.Parse(BuildReplaySessionJson(1))!.AsObject();
        replayDocument["entries"]!.AsArray()[0] = null;

        var act = () => TestHarness<TestProgram, TestModel, Unit>.LoadReplaySession(replayDocument.ToJsonString());

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task LoadReplaySession_RejectsSequenceMismatch()
    {
        var replayDocument = JsonNode.Parse(BuildReplaySessionJson(1))!.AsObject();
        var firstEntry = replayDocument["entries"]!.AsArray()[0]!.AsObject();
        firstEntry["sequence"] = 2;

        var act = () => TestHarness<TestProgram, TestModel, Unit>.LoadReplaySession(replayDocument.ToJsonString());

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ReplaySession_IsDeterministicForSameSession()
    {
        var sessionJson = BuildReplaySessionJson(4, -1, 3, 6);
        var session = TestHarness<TestProgram, TestModel, Unit>.LoadReplaySession(sessionJson);

        var firstRun = TestHarness<TestProgram, TestModel, Unit>.Create(Unit.Value);
        var secondRun = TestHarness<TestProgram, TestModel, Unit>.Create(Unit.Value);

        firstRun.ReplaySession(session, MapReplayEntryToMessage);
        secondRun.ReplaySession(session, MapReplayEntryToMessage);

        await Assert.That(firstRun.Model.Value).IsEqualTo(secondRun.Model.Value);
        await Assert.That(firstRun.Model.Value).IsEqualTo(12);
    }

    [Test]
    public async Task LoadAndReplaySession_RejectsInvalidEntryPayload()
    {
        var harness = TestHarness<TestProgram, TestModel, Unit>.Create(Unit.Value);
        var replayDocument = JsonNode.Parse(BuildReplaySessionJson(1))!.AsObject();
        var firstEntry = replayDocument["entries"]!.AsArray()[0]!.AsObject();
        firstEntry["payload"] = new JsonObject();

        var act = () => harness.LoadAndReplaySession(replayDocument.ToJsonString(), MapReplayEntryToMessage);

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    private static string BuildReplaySessionJson(params int[] deltas)
    {
        var entries = deltas
            .Select((delta, index) => new TestHarnessReplayEntryV1
            {
                Sequence = index,
                MessageType = nameof(Add),
                Payload = JsonSerializer.SerializeToElement(new AddReplayPayload(delta))
            })
            .ToArray();

        var session = new TestHarnessReplaySessionV1
        {
            SchemaVersion = TestHarnessReplaySchema.Version1,
            Metadata = new TestHarnessReplayMetadataV1
            {
                SessionId = "session-165",
                ProgramName = nameof(TestProgram),
                ProgramVersion = "1.0.0",
                RecordedAtUnixMs = 1
            },
            Entries = entries
        };

        return JsonSerializer.Serialize(session);
    }

    private static Message MapReplayEntryToMessage(TestHarnessReplayEntryV1 entry) =>
        entry.MessageType switch
        {
            nameof(Add) => ParseAddReplayMessage(entry.Payload),
            _ => throw new InvalidOperationException($"Unsupported replay message type: {entry.MessageType}")
        };

    private static Message ParseAddReplayMessage(JsonElement payload)
    {
        if (payload.TryGetProperty("delta", out var deltaProperty) is false &&
            payload.TryGetProperty("Delta", out deltaProperty) is false)
        {
            throw new InvalidOperationException("Replay payload is invalid: delta is required.");
        }

        return new Add(deltaProperty.GetInt32());
    }

    private sealed record AddReplayPayload(int Delta);

    public sealed record TestModel(int Value);

    public interface TestMessage : Message;
    public sealed record Add(int Delta) : TestMessage;
    public sealed record QueueAdd(int Delta) : TestMessage;
    public sealed record StartLoop : TestMessage;
    public sealed record QueueLoop : TestMessage;
    public sealed record CommandRejected(string Reason) : TestMessage;

    public sealed record AddFromCommand(int Delta) : Command;
    public sealed record LoopCommand : Command;

    public sealed class TestProgram : Program<TestModel, Unit>
    {
        public static (TestModel, Command) Initialize(Unit argument) =>
            (new TestModel(0), Commands.None);

        public static (TestModel, Command) Transition(TestModel model, Message message) =>
            message switch
            {
                Add add => (model with { Value = model.Value + add.Delta }, Commands.None),
                QueueAdd queue => (model, new AddFromCommand(queue.Delta)),
                QueueLoop => (model, new LoopCommand()),
                CommandRejected => (model, Commands.None),
                _ => (model, Commands.None)
            };

        public static Result<Message[], Message> Decide(TestModel state, Message command) =>
            command switch
            {
                Add add => Result<Message[], Message>.Ok([add]),
                QueueAdd queue => Result<Message[], Message>.Ok([queue]),
                StartLoop => Result<Message[], Message>.Ok([new QueueLoop()]),
                QueueLoop loop => Result<Message[], Message>.Ok([loop]),
                _ => Result<Message[], Message>.Err(new CommandRejected($"Unsupported command: {command.GetType().Name}"))
            };

        public static bool IsTerminal(TestModel state) => false;

        public static Document View(TestModel model) =>
            new("Test", Html.Elements.div([], []));

        public static Subscription Subscriptions(TestModel model) =>
            new Subscription.None();
    }
}
