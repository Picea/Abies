// =============================================================================
// Patch Interpreter Tests
// =============================================================================
// Drives the real Operations.Diff over native-vocabulary trees and asserts
// the FakeBackend ends up with the right control tree — plus direct patch
// cases for paths the diff reaches rarely (SetChildrenHtml, head patches)
// and the Text/Raw tripwires.
// =============================================================================

using Picea.Abies.DOM;
using Picea.Abies.Native.Rendering;
using Attribute = Picea.Abies.DOM.Attribute;

namespace Picea.Abies.Native.Tests;

public record Ping : Message;
public record Pong : Message;

public class PatchInterpreterTests
{
    private static (PatchInterpreter<FakeControl> Interpreter, FakeBackend Backend) Create()
    {
        var backend = new FakeBackend();
        return (new PatchInterpreter<FakeControl>(backend), backend);
    }

    private static Element Item(string id, string key, string text) =>
        new(id, "TextBlock", [new Attribute($"{id}-k", "key", key), new Attribute($"{id}-t", "Text", text)]);

    // =========================================================================
    // Initial render
    // =========================================================================

    [Test]
    public async Task InitialRender_BuildsControlTree()
    {
        var (interpreter, backend) = Create();
        var view = new Element("root", "StackPanel",
            [new Attribute("a1", "Spacing", "12"), new Attribute("a2", "key", "ignored")],
            new Element("t1", "TextBlock", [new Attribute("a3", "Text", "0")]),
            new Element("b1", "Button",
                [new Attribute("a4", "Content", "+"), new Handler("Click", "c1", new Ping(), "h1")]));

        interpreter.Apply(Operations.Diff(null, view));

        var root = backend.Root!;
        await Assert.That(root.Tag).IsEqualTo("StackPanel");
        await Assert.That(root.Props["Spacing"]).IsEqualTo("12");
        await Assert.That(root.Props.ContainsKey("key")).IsFalse();
        await Assert.That(root.Children).Count().IsEqualTo(2);
        await Assert.That(root.Children[0].Props["Text"]).IsEqualTo("0");
        await Assert.That(root.Children[1].Props["Content"]).IsEqualTo("+");
        await Assert.That(root.Children[1].AttachedEvents.Contains("Click")).IsTrue();
        await Assert.That(interpreter.ControlCount).IsEqualTo(3);
    }

    // =========================================================================
    // Attribute updates
    // =========================================================================

    [Test]
    public async Task AttributeUpdate_SetsNewValue()
    {
        var (interpreter, backend) = Create();
        var old = new Element("t1", "TextBlock", [new Attribute("a1", "Text", "0")]);
        var @new = new Element("t1", "TextBlock", [new Attribute("a1", "Text", "1")]);

        interpreter.Apply(Operations.Diff(null, old));
        interpreter.Apply(Operations.Diff(old, @new));

        await Assert.That(backend.Root!.Props["Text"]).IsEqualTo("1");
    }

    [Test]
    public async Task AttributeRemove_ResetsProperty()
    {
        var (interpreter, backend) = Create();
        var old = new Element("t1", "TextBlock",
            [new Attribute("a1", "Text", "x"), new Attribute("a2", "FontSize", "24")]);
        var @new = new Element("t1", "TextBlock", [new Attribute("a1", "Text", "x")]);

        interpreter.Apply(Operations.Diff(null, old));
        interpreter.Apply(Operations.Diff(old, @new));

        await Assert.That(backend.Root!.Props.ContainsKey("FontSize")).IsFalse();
        await Assert.That(backend.Root!.Props["Text"]).IsEqualTo("x");
    }

    // =========================================================================
    // Handler lifecycle
    // =========================================================================

    [Test]
    public async Task StaticHandler_DispatchesCommand()
    {
        var (interpreter, _) = Create();
        var view = new Element("b1", "Button", [new Handler("Click", "c1", new Ping(), "h1")]);
        interpreter.Apply(Operations.Diff(null, view));

        Message? dispatched = null;
        interpreter.Dispatch = m => { dispatched = m; return ValueTask.CompletedTask; };
        interpreter.OnNativeEvent("b1", "Click", null);

        await Assert.That(dispatched).IsTypeOf<Ping>();
    }

    [Test]
    public async Task UpdateHandler_SwapsMessageWithoutReattaching()
    {
        var (interpreter, backend) = Create();
        var old = new Element("b1", "Button", [new Handler("Click", "c1", new Ping(), "h1")]);
        var @new = new Element("b1", "Button", [new Handler("Click", "c2", new Pong(), "h1")]);

        interpreter.Apply(Operations.Diff(null, old));
        var attachCountAfterFirstRender = backend.Log.Count(l => l.StartsWith("attach"));
        interpreter.Apply(Operations.Diff(old, @new));

        Message? dispatched = null;
        interpreter.Dispatch = m => { dispatched = m; return ValueTask.CompletedTask; };
        interpreter.OnNativeEvent("b1", "Click", null);

        await Assert.That(dispatched).IsTypeOf<Pong>();
        await Assert.That(backend.Log.Count(l => l.StartsWith("attach"))).IsEqualTo(attachCountAfterFirstRender);
    }

    [Test]
    public async Task DataHandler_ReceivesNativeEventData()
    {
        var (interpreter, _) = Create();
        var view = new Element("tb1", "TextBox",
            [new Handler("TextChanged", "c1", null, "h1", o => new Echo(((TextChangedData?)o)!.Text))]);
        interpreter.Apply(Operations.Diff(null, view));

        Message? dispatched = null;
        interpreter.Dispatch = m => { dispatched = m; return ValueTask.CompletedTask; };
        interpreter.OnNativeEvent("tb1", "TextChanged", new TextChangedData("hello"));

        await Assert.That(dispatched).IsTypeOf<Echo>();
        await Assert.That(((Echo)dispatched!).Text).IsEqualTo("hello");
    }

    public record Echo(string Text) : Message;

    // =========================================================================
    // Children: keyed reorder, removal
    // =========================================================================

    [Test]
    public async Task KeyedReorder_MovesExistingControls()
    {
        var (interpreter, backend) = Create();
        var old = new Element("p", "StackPanel", [],
            Item("ia", "a", "A"), Item("ib", "b", "B"), Item("ic", "c", "C"));
        var @new = new Element("p", "StackPanel", [],
            Item("ic", "c", "C"), Item("ia", "a", "A"), Item("ib", "b", "B"));

        interpreter.Apply(Operations.Diff(null, old));
        var createdAfterFirstRender = backend.AllCreated.Count;
        interpreter.Apply(Operations.Diff(old, @new));

        var order = backend.Root!.Children.Select(c => c.Id).ToArray();
        await Assert.That(order).IsEquivalentTo(new[] { "ic", "ia", "ib" });
        await Assert.That(backend.AllCreated.Count).IsEqualTo(createdAfterFirstRender);
    }

    [Test]
    public async Task RemovingAllChildren_EmptiesParentAndTracking()
    {
        var (interpreter, backend) = Create();
        var old = new Element("p", "StackPanel", [],
            Item("ia", "a", "A"), Item("ib", "b", "B"), Item("ic", "c", "C"));
        var @new = new Element("p", "StackPanel", []);

        interpreter.Apply(Operations.Diff(null, old));
        interpreter.Apply(Operations.Diff(old, @new));

        await Assert.That(backend.Root!.Children).IsEmpty();
        await Assert.That(interpreter.ControlCount).IsEqualTo(1);
    }

    /// <summary>
    /// Backends key their event bookkeeping by control instance, so dropping a
    /// subtree must detach its handlers — otherwise the backend holds the dead
    /// control (and its closures) forever.
    /// </summary>
    [Test]
    public async Task RemovedSubtree_DetachesNativeEvents()
    {
        var (interpreter, backend) = Create();
        var button = new Element("b1", "Button",
            [new Attribute("a1", "key", "b"), new Handler("Click", "c1", new Ping(), "h1")]);
        var old = new Element("p", "StackPanel", [], button);
        var @new = new Element("p", "StackPanel", []);

        interpreter.Apply(Operations.Diff(null, old));
        interpreter.Apply(Operations.Diff(old, @new));

        await Assert.That(backend.Log.Count(l => l.StartsWith("attach")))
            .IsEqualTo(backend.Log.Count(l => l.StartsWith("detach")));
        await Assert.That(backend.AllCreated.Single(c => c.Id == "b1").AttachedEvents).IsEmpty();
    }

    /// <summary>Replacing a subtree must detach the handlers of the old one.</summary>
    [Test]
    public async Task ReplacedSubtree_DetachesOldControlEvents()
    {
        var (interpreter, backend) = Create();
        var old = new Element("p", "StackPanel", [],
            new Element("x1", "Button", [new Handler("Click", "c1", new Ping(), "h1")]));
        var @new = new Element("p", "StackPanel", [],
            new Element("x1", "TextBlock", [new Attribute("a1", "Text", "done")]));

        interpreter.Apply(Operations.Diff(null, old));
        interpreter.Apply(Operations.Diff(old, @new));

        await Assert.That(backend.Log.Count(l => l.StartsWith("detach"))).IsEqualTo(1);
    }

    /// <summary>A new root drops the previous tree, detaching all of its events.</summary>
    [Test]
    public async Task NewRoot_DetachesPreviousTreeEvents()
    {
        var (interpreter, backend) = Create();
        var old = new Element("p", "StackPanel", [],
            new Element("b1", "Button", [new Handler("Click", "c1", new Ping(), "h1")]));

        interpreter.Apply(Operations.Diff(null, old));
        interpreter.Apply([new AddRoot(new Element("g", "Grid", []))]);

        await Assert.That(backend.Log.Count(l => l.StartsWith("detach"))).IsEqualTo(1);
    }

    /// <summary>
    /// AddChild appends, so a mid-list insertion relies on the diff emitting a
    /// MoveChild to restore order. This pins that end-to-end behaviour.
    /// </summary>
    [Test]
    public async Task InsertingChildInMiddle_PreservesOrder()
    {
        var (interpreter, backend) = Create();
        var old = new Element("p", "StackPanel", [], Item("ia", "a", "A"), Item("ic", "c", "C"));
        var @new = new Element("p", "StackPanel", [],
            Item("ia", "a", "A"), Item("ib", "b", "B"), Item("ic", "c", "C"));

        interpreter.Apply(Operations.Diff(null, old));
        interpreter.Apply(Operations.Diff(old, @new));

        var order = backend.Root!.Children.Select(c => c.Id).ToArray();
        await Assert.That(order).IsEquivalentTo(new[] { "ia", "ib", "ic" });
    }

    [Test]
    public async Task TagChange_ReplacesControlInPlace()
    {
        var (interpreter, backend) = Create();
        var old = new Element("p", "StackPanel", [],
            new Element("x1", "TextBlock", [new Attribute("a1", "Text", "read")]));
        var @new = new Element("p", "StackPanel", [],
            new Element("x2", "TextBox", [new Attribute("a2", "Text", "edit")]));

        interpreter.Apply(Operations.Diff(null, old));
        interpreter.Apply(Operations.Diff(old, @new));

        await Assert.That(backend.Root!.Children).Count().IsEqualTo(1);
        await Assert.That(backend.Root!.Children[0].Tag).IsEqualTo("TextBox");
        await Assert.That(backend.Root!.Children[0].Props["Text"]).IsEqualTo("edit");
        await Assert.That(interpreter.ControlCount).IsEqualTo(2);
    }

    // =========================================================================
    // Direct patch paths
    // =========================================================================

    [Test]
    public async Task SetChildrenHtml_MaterializesNodeChildren()
    {
        var (interpreter, backend) = Create();
        var parent = new Element("p", "StackPanel", []);
        interpreter.Apply(Operations.Diff(null, parent));

        interpreter.Apply([new SetChildrenHtml(parent,
            [Item("ia", "a", "A"), Item("ib", "b", "B")])]);

        var texts = backend.Root!.Children.Select(c => c.Props["Text"]!).ToArray();
        await Assert.That(texts).IsEquivalentTo(new[] { "A", "B" });
    }

    [Test]
    public async Task AppendChildrenHtml_AppendsAfterExisting()
    {
        var (interpreter, backend) = Create();
        var parent = new Element("p", "StackPanel", [], Item("ia", "a", "A"));
        interpreter.Apply(Operations.Diff(null, parent));

        interpreter.Apply([new AppendChildrenHtml(parent, [Item("ib", "b", "B")])]);

        var order = backend.Root!.Children.Select(c => c.Id).ToArray();
        await Assert.That(order).IsEquivalentTo(new[] { "ia", "ib" });
    }

    [Test]
    public async Task TextPatch_Throws()
    {
        var (interpreter, _) = Create();
        var parent = new Element("p", "StackPanel", []);
        interpreter.Apply(Operations.Diff(null, parent));

        await Assert.That(() => interpreter.Apply([new AddText(parent, new Text("t", "boom"))]))
            .Throws<NotSupportedException>();
    }

    [Test]
    public async Task TextChildInBuiltSubtree_Throws()
    {
        var (interpreter, _) = Create();
        var view = new Element("p", "StackPanel", [], new Text("t", "boom"));

        await Assert.That(() => interpreter.Apply(Operations.Diff(null, view)))
            .Throws<NotSupportedException>();
    }

    [Test]
    public async Task HeadPatches_AreIgnored()
    {
        var (interpreter, backend) = Create();
        var parent = new Element("p", "StackPanel", []);
        interpreter.Apply(Operations.Diff(null, parent));
        var logCount = backend.Log.Count;

        interpreter.Apply([new RemoveHeadElement("some-key")]);

        await Assert.That(backend.Log.Count).IsEqualTo(logCount);
    }
}
