using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using Picea.Abies.Testing;

namespace Picea.Abies.Benchmarks;

/// <summary>
/// Baseline replay throughput benchmark for the testing harness using a deterministic 10k-entry session.
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class TestHarnessReplayBenchmarks
{
    private const int ReplayEntryCount = 10_000;
    private string _sessionJson = null!;

    [GlobalSetup]
    public void Setup() =>
        _sessionJson = BuildReplaySessionJson(ReplayEntryCount);

    [Benchmark(Description = "TestHarness load+replay 10k messages")]
    public int LoadAndReplay10k()
    {
        var harness = TestHarness<ReplayBenchmarkProgram, ReplayBenchmarkModel, Unit>.Create(Unit.Value);
        harness.LoadAndReplaySession(_sessionJson, MapReplayEntryToMessage);
        return harness.Model.Value;
    }

    private static string BuildReplaySessionJson(int entryCount)
    {
        var entries = Enumerable
            .Range(0, entryCount)
            .Select(index => new TestHarnessReplayEntryV1
            {
                Sequence = index,
                MessageType = nameof(Add),
                Payload = JsonSerializer.SerializeToElement(new AddPayload(1))
            })
            .ToArray();

        var session = new TestHarnessReplaySessionV1
        {
            SchemaVersion = TestHarnessReplaySchema.Version1,
            Metadata = new TestHarnessReplayMetadataV1
            {
                SessionId = "benchmark-10k",
                ProgramName = nameof(ReplayBenchmarkProgram),
                ProgramVersion = "1.0.0",
                RecordedAtUnixMs = 1
            },
            Entries = entries
        };

        return TestHarness<ReplayBenchmarkProgram, ReplayBenchmarkModel, Unit>.SerializeReplaySession(session);
    }

    private static Message MapReplayEntryToMessage(TestHarnessReplayEntryV1 entry)
    {
        if (entry.MessageType != nameof(Add))
        {
            throw new InvalidOperationException($"Unsupported replay message type: {entry.MessageType}");
        }

        if (entry.Payload.TryGetProperty("Delta", out var deltaProperty) is false &&
            entry.Payload.TryGetProperty("delta", out deltaProperty) is false)
        {
            throw new InvalidOperationException("Replay payload is invalid: delta is required.");
        }

        return new Add(deltaProperty.GetInt32());
    }

    private sealed record AddPayload(int Delta);

    private sealed record ReplayBenchmarkModel(int Value);

    private interface BenchmarkMessage : Message;

    private sealed record Add(int Delta) : BenchmarkMessage;

    private sealed class ReplayBenchmarkProgram : Program<ReplayBenchmarkModel, Unit>
    {
        public static (ReplayBenchmarkModel, Command) Initialize(Unit argument) =>
            (new ReplayBenchmarkModel(0), Commands.None);

        public static (ReplayBenchmarkModel, Command) Transition(ReplayBenchmarkModel model, Message message) =>
            message switch
            {
                Add add => (model with { Value = model.Value + add.Delta }, Commands.None),
                _ => (model, Commands.None)
            };

        public static Result<Message[], Message> Decide(ReplayBenchmarkModel state, Message command) =>
            command switch
            {
                Add add => Result<Message[], Message>.Ok([add]),
                _ => Result<Message[], Message>.Err(command)
            };

        public static bool IsTerminal(ReplayBenchmarkModel state) => false;

        public static Document View(ReplayBenchmarkModel model) =>
            new("Bench", Html.Elements.div([], []));

        public static Subscription Subscriptions(ReplayBenchmarkModel model) =>
            new Subscription.None();
    }
}
