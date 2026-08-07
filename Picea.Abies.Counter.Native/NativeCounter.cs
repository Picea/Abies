// =============================================================================
// NativeCounterView — Native View over the Shared Counter Program
// =============================================================================
// This file is the whole native app: a View, and nothing else. Model, update,
// decisions, termination and subscriptions all come from the existing
// CounterProgram, shared verbatim with the WASM and server hosts.
//
// The pairing happens at the bootstrap call site via
// WithView<CounterProgram, NativeCounterView, ...> — see App.xaml.cs. Before
// the Program contract was split into ProgramCore + ProgramView, this class
// also had to hand-forward five members to CounterProgram, because C# cannot
// inherit static abstract implementations.
// =============================================================================

using Picea.Abies.DOM;
using Picea.Abies.Native;
using static Picea.Abies.Native.Elements;
using static Picea.Abies.Native.Events;
using static Picea.Abies.Native.Properties;

namespace Picea.Abies.Counter.Native;

public sealed class NativeCounterView : ProgramView<CounterModel>
{
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
