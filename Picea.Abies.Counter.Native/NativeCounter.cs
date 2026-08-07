// =============================================================================
// NativeCounterProgram — Shared Model/Update, Native View
// =============================================================================
// Delegates Initialize/Transition/Decide/IsTerminal/Subscriptions to the
// existing CounterProgram (shared with the WASM and server hosts) and
// supplies only a native-vocabulary View. This is the whole point of the
// spike: one pure program, three renderers (browser DOM, server, native
// WinUI controls).
// =============================================================================

using Picea.Abies.DOM;
using Picea.Abies.Native;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Native.Elements;
using static Picea.Abies.Native.Events;
using static Picea.Abies.Native.Properties;

namespace Picea.Abies.Counter.Native;

public sealed class NativeCounterProgram : Program<CounterModel, Unit>
{
    public static (CounterModel, Command) Initialize(Unit argument) => CounterProgram.Initialize(argument);

    public static (CounterModel, Command) Transition(CounterModel model, Message message) => CounterProgram.Transition(model, message);

    public static Result<Message[], Message> Decide(CounterModel model, Message command) => CounterProgram.Decide(model, command);

    public static bool IsTerminal(CounterModel model) => CounterProgram.IsTerminal(model);

    public static Subscription Subscriptions(CounterModel model) => CounterProgram.Subscriptions(model);

    public static Document View(CounterModel model) =>
        new("Abies Counter",
            StackPanel([Spacing(16), HorizontalAlignment(Alignment.Center), VerticalAlignment(Alignment.Center)],
            [
                TextBlock([FontSize(28), FontWeight(TextWeight.Bold), HorizontalAlignment(Alignment.Center)],
                    "Abies Counter"),
                StackPanel([Orientation(StackOrientation.Horizontal), Spacing(12), HorizontalAlignment(Alignment.Center)],
                [
                    Button([OnClick(new Decrement()), Width(48)], "−"),
                    TextBlock([FontSize(24), Width(64), VerticalAlignment(Alignment.Center), HorizontalAlignment(Alignment.Center)],
                        model.Count.ToString()),
                    Button([OnClick(new Increment()), Width(48)], "+"),
                ]),
                Button([OnClick(new Reset()), HorizontalAlignment(Alignment.Center)], "Reset"),
            ]));
}
