// =============================================================================
// A complete Abies program, split into Core and View.
// =============================================================================
// CounterProgram is the platform-free half: model, messages, transitions,
// decisions, subscriptions. It mentions no controls, so the same class can back
// a browser view, a server view, or the native view below.
//
// CounterView is the native half. Pairing happens in App.xaml.cs via
// Runtime.RunWithView.
// =============================================================================

using Picea;
using Picea.Abies;
using Picea.Abies.DOM;
using Picea.Abies.Native;
using Picea.Abies.Subscriptions;
using static Picea.Abies.Native.Elements;
using static Picea.Abies.Native.Events;
using static Picea.Abies.Native.Properties;

namespace AbiesApp;

public record CounterModel(int Count);

public record Increment : Message;
public record Decrement : Message;
public record Reset : Message;

public sealed class CounterProgram : ProgramCore<CounterModel, Unit>
{
    public static (CounterModel, Command) Initialize(Unit argument) =>
        (new CounterModel(0), Commands.None);

    public static (CounterModel, Command) Transition(CounterModel model, Message message) =>
        message switch
        {
            Increment => (model with { Count = model.Count + 1 }, Commands.None),
            Decrement => (model with { Count = model.Count - 1 }, Commands.None),
            Reset => (model with { Count = 0 }, Commands.None),
            _ => (model, Commands.None),
        };

    public static Result<Message[], Message> Decide(CounterModel model, Message command) =>
        Result<Message[], Message>.Ok([command]);

    public static bool IsTerminal(CounterModel model) => false;

    public static Subscription Subscriptions(CounterModel model) => new Subscription.None();
}

public sealed class CounterView : ProgramView<CounterModel>
{
    public static Document View(CounterModel model) =>
        new("AbiesApp",
            StackPanel([Spacing(16), HorizontalAlignment(Alignment.Center), VerticalAlignment(Alignment.Center)],
            [
                TextBlock([FontSize(28), FontWeight(TextWeight.Bold), HorizontalAlignment(Alignment.Center)],
                    "AbiesApp"),
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
