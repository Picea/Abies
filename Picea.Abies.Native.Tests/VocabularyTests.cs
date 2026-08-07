// =============================================================================
// Vocabulary Tests
// =============================================================================
// Covers the controls added beyond the original ten, driving the real
// Operations.Diff so the element shapes are exercised the way a view produces
// them rather than through hand-built patches.
// =============================================================================

using Picea.Abies.DOM;
using Picea.Abies.Native.Rendering;
using static Picea.Abies.Native.Elements;
using static Picea.Abies.Native.Events;
using static Picea.Abies.Native.Properties;

namespace Picea.Abies.Native.Tests;

public record Chose(int Index) : Message;
public record Switched(bool On) : Message;

public class VocabularyTests
{
    private static (PatchInterpreter<FakeControl> Interpreter, FakeBackend Backend) Create()
    {
        var backend = new FakeBackend();
        return (new PatchInterpreter<FakeControl>(backend), backend);
    }

    private static Node Root(params Node[] children) => StackPanel([], children);

    [Test]
    public async Task ComboBox_BuildsItemsAsChildren()
    {
        var (interpreter, backend) = Create();
        var view = (Element)Root(
            ComboBox([SelectedIndex(1)],
            [
                ComboBoxItem([Id("i-a")], "Alpha"),
                ComboBoxItem([Id("i-b")], "Beta"),
            ]));

        interpreter.Apply(Operations.Diff(null, view));

        var combo = backend.Root!.Children[0];
        await Assert.That(combo.Tag).IsEqualTo("ComboBox");
        await Assert.That(combo.Props["SelectedIndex"]).IsEqualTo("1");
        await Assert.That(combo.Children.Select(c => c.Props["Content"]!)).IsEquivalentTo(new[] { "Alpha", "Beta" });
    }

    [Test]
    public async Task ComboBoxItems_ReorderByKeyWithoutRecreating()
    {
        var (interpreter, backend) = Create();

        static Node Options(params string[] names) =>
            ComboBox([Id("combo")],
                [.. names.Select(n => ComboBoxItem([Id($"i-{n}"), Key(n)], n))]);

        var old = (Element)Root(Options("a", "b", "c"));
        var @new = (Element)Root(Options("c", "a", "b"));

        interpreter.Apply(Operations.Diff(null, old));
        var createdAfterFirstRender = backend.AllCreated.Count;
        interpreter.Apply(Operations.Diff(old, @new));

        var order = backend.Root!.Children[0].Children.Select(c => c.Id).ToArray();
        await Assert.That(order).IsEquivalentTo(new[] { "i-c", "i-a", "i-b" });
        await Assert.That(backend.AllCreated.Count).IsEqualTo(createdAfterFirstRender);
    }

    [Test]
    public async Task ToggleSwitch_CarriesStateAndDispatches()
    {
        var (interpreter, backend) = Create();
        var view = (Element)Root(
            ToggleSwitch([Id("sw"), IsOn(true), OnToggled(d => new Switched(d!.IsChecked))]));

        interpreter.Apply(Operations.Diff(null, view));

        var sw = backend.Root!.Children[0];
        await Assert.That(sw.Props["IsOn"]).IsEqualTo("True");

        Message? dispatched = null;
        interpreter.Dispatch = m => { dispatched = m; return ValueTask.CompletedTask; };
        interpreter.OnNativeEvent("sw", "Toggled", new ToggledData(false));

        await Assert.That(((Switched)dispatched!).On).IsFalse();
    }

    [Test]
    public async Task ComboBox_SelectionChangedDispatchesIndex()
    {
        var (interpreter, _) = Create();
        var view = (Element)Root(
            ComboBox([Id("combo"), OnSelectionChanged(d => new Chose(d!.SelectedIndex))], []));

        interpreter.Apply(Operations.Diff(null, view));

        Message? dispatched = null;
        interpreter.Dispatch = m => { dispatched = m; return ValueTask.CompletedTask; };
        interpreter.OnNativeEvent("combo", "SelectionChanged", new SelectionChangedData(2));

        await Assert.That(((Chose)dispatched!).Index).IsEqualTo(2);
    }

    [Test]
    public async Task Progress_CarriesItsProperties()
    {
        var (interpreter, backend) = Create();
        var view = (Element)Root(
            ProgressRing([Id("ring"), IsActive(true)]),
            ProgressBar([Id("bar"), Minimum(0), Maximum(10), Value(4)]));

        interpreter.Apply(Operations.Diff(null, view));

        await Assert.That(backend.Root!.Children[0].Props["IsActive"]).IsEqualTo("True");
        await Assert.That(backend.Root!.Children[1].Props["Value"]).IsEqualTo("4");
    }

    /// <summary>Element content, rather than the string-content overload.</summary>
    [Test]
    public async Task Button_AcceptsAnElementTreeAsContent()
    {
        var (interpreter, backend) = Create();
        var view = (Element)Root(
            Button([Id("btn")],
                StackPanel([Id("btn-inner"), Orientation(StackOrientation.Horizontal)],
                [
                    TextBlock([Id("btn-icon")], "★"),
                    TextBlock([Id("btn-label")], "Star"),
                ])));

        interpreter.Apply(Operations.Diff(null, view));

        var button = backend.Root!.Children[0];
        await Assert.That(button.Tag).IsEqualTo("Button");
        await Assert.That(button.Children[0].Tag).IsEqualTo("StackPanel");
        await Assert.That(button.Children[0].Children.Select(c => c.Props["Text"]!))
            .IsEquivalentTo(new[] { "★", "Star" });
    }

    [Test]
    public async Task ContentControl_WrapsASubtree()
    {
        var (interpreter, backend) = Create();
        var view = (Element)Root(
            ContentControl([Id("host")], TextBlock([Id("inner")], "wrapped")));

        interpreter.Apply(Operations.Diff(null, view));

        await Assert.That(backend.Root!.Children[0].Children[0].Props["Text"]).IsEqualTo("wrapped");
    }
}
