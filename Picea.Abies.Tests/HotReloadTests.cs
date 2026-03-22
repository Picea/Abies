#if DEBUG
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Html.Elements;

namespace Picea.Abies.Tests;

[NotInParallel("hot-reload-registry")]
public sealed class HotReloadTests
{
    [Test]
    public async Task MetadataUpdateHandler_RoutesNotificationsToMatchingAssembly()
    {
        var matching = new TestHotReloadRuntime();
        var mismatch = new TestHotReloadRuntime();

        using var _ = HotReloadRuntimeRegistry.Register(typeof(HotReloadTests).Assembly, matching);
        using var __ = HotReloadRuntimeRegistry.Register(typeof(Uri).Assembly, mismatch);

        AbiesMetadataUpdateHandler.UpdateApplication([typeof(HotReloadTests)]);

        await Assert.That(matching.RefreshCount).IsEqualTo(1);
        await Assert.That(mismatch.RefreshCount).IsEqualTo(0);
    }

    [Test]
    public async Task MetadataUpdateHandler_AssemblyMismatch_IsSafeNoOp()
    {
        var runtime = new TestHotReloadRuntime();

        using var _ = HotReloadRuntimeRegistry.Register(typeof(HotReloadTests).Assembly, runtime);

        AbiesMetadataUpdateHandler.UpdateApplication([typeof(string)]);

        await Assert.That(runtime.RefreshCount).IsEqualTo(0);
    }

    [Test]
    public async Task Runtime_HotReloadRefresh_RerendersAndPreservesModel()
    {
        HotReloadTestProgram.ViewRevision = 0;

        var appliedBatches = new List<IReadOnlyList<Patch>>();

        static ValueTask<Result<Message[], PipelineError>> Interpret(Command command) =>
            ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

        using var runtime = await Runtime<HotReloadTestProgram, HotReloadModel, Unit>.Start(
            apply: patches => appliedBatches.Add(patches.ToArray()),
            interpreter: Interpret);

        await runtime.Dispatch(new HotReloadIncrement());

        await Assert.That(runtime.Model.Count).IsEqualTo(1);

        var batchesBeforeRefresh = appliedBatches.Count;

        HotReloadTestProgram.ViewRevision = 1;
        AbiesMetadataUpdateHandler.UpdateApplication([typeof(HotReloadTestProgram)]);

        await Assert.That(runtime.Model.Count).IsEqualTo(1);
        await Assert.That(appliedBatches.Count).IsEqualTo(batchesBeforeRefresh + 1);
    }

    private sealed class TestHotReloadRuntime : IHotReloadRuntime
    {
        private int _refreshCount;

        public int RefreshCount => _refreshCount;

        public void RefreshViewFromCurrentModel() => Interlocked.Increment(ref _refreshCount);
    }

    private sealed class HotReloadIncrement : Message;

    private sealed record HotReloadModel(int Count);

    private sealed class HotReloadTestProgram : Program<HotReloadModel, Unit>
    {
        public static int ViewRevision { get; set; }

        public static (HotReloadModel, Command) Initialize(Unit argument) =>
            (new HotReloadModel(0), Commands.None);

        public static (HotReloadModel, Command) Transition(HotReloadModel model, Message message) =>
            message switch
            {
                HotReloadIncrement => (model with { Count = model.Count + 1 }, Commands.None),
                _ => (model, Commands.None)
            };

        public static Document View(HotReloadModel model) =>
            new(
                $"Hot Reload {ViewRevision}",
                div([], [text($"count:{model.Count};view:{ViewRevision}")]));

        public static Subscription Subscriptions(HotReloadModel model) =>
            SubscriptionModule.None;
    }
}
#endif
