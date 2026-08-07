// =============================================================================
// Runtime Integration Test — Full MVU Loop, Headless
// =============================================================================
// Proves the whole native pipeline with no UI framework at all:
// Program (native-vocabulary View) → Runtime.Start → Diff → PatchInterpreter
// → FakeBackend, and back: native event → handler → Runtime.Dispatch →
// re-render → patched fake tree.
//
// The View uses the real Elements/Properties/Events factories, which also
// exercises Praefixum id interception in a consumer assembly.
// =============================================================================

using Picea.Abies.DOM;
using Picea.Abies.Native.Rendering;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Native.Elements;
using static Picea.Abies.Native.Events;
using static Picea.Abies.Native.Properties;

namespace Picea.Abies.Native.Tests;

public record CountModel(int Count);
public record Increment : Message;

public sealed class NativeTestProgram : Program<CountModel, Unit>
{
    public static (CountModel, Command) Initialize(Unit _) => (new CountModel(0), Commands.None);

    public static (CountModel, Command) Transition(CountModel model, Message message) =>
        message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            _ => (model, Commands.None)
        };

    public static Result<Message[], Message> Decide(CountModel _, Message command) =>
        Result<Message[], Message>.Ok([command]);

    public static bool IsTerminal(CountModel _) => false;

    public static Document View(CountModel model) =>
        new("Native Test",
            StackPanel([Spacing(8)],
            [
                TextBlock([FontSize(24)], model.Count.ToString()),
                Button([OnClick(new Increment())], "+"),
            ]));

    public static Subscription Subscriptions(CountModel _) => new Subscription.None();
}

public class RuntimeIntegrationTests
{
    private static ValueTask<Result<Message[], PipelineError>> NoOpInterpreter(Command _) =>
        ValueTask.FromResult(Result<Message[], PipelineError>.Ok([]));

    [Test]
    public async Task FullLoop_RenderDispatchRerender()
    {
        var backend = new FakeBackend();
        var interpreter = new PatchInterpreter<FakeControl>(backend);
        string? title = null;

        using var runtime = await Runtime<NativeTestProgram, CountModel, Unit>.Start(
            interpreter.Apply, NoOpInterpreter, Unit.Value,
            titleChanged: t => title = t);

        // Initial render reached the fake backend.
        var root = backend.Root!;
        await Assert.That(root.Tag).IsEqualTo("StackPanel");
        await Assert.That(root.Children[0].Props["Text"]).IsEqualTo("0");
        await Assert.That(title).IsEqualTo("Native Test");

        // A native click resolves the handler and produces the message...
        Message? clicked = null;
        interpreter.Dispatch = m => { clicked = m; return ValueTask.CompletedTask; };
        var button = root.Children[1];
        await Assert.That(button.AttachedEvents.Contains("Click")).IsTrue();
        interpreter.OnNativeEvent(button.Id, "Click", null);
        await Assert.That(clicked).IsTypeOf<Increment>();

        // ...and dispatching it re-renders through the same pipeline.
        var result = await runtime.Dispatch(clicked!);
        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(root.Children[0].Props["Text"]).IsEqualTo("1");
    }
}
