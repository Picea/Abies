// =============================================================================
// Diff Tests — Virtual DOM Diffing Algorithm
// =============================================================================
// Tests the pure Operations.Diff() function that computes minimal patches
// to transform one virtual DOM tree into another. No browser dependencies.
//
// Coverage:
//   * Initial render (null -> node)
//   * Element replacement (tag change)
//   * Attribute diffing (add, remove, update, same-order fast path)
//   * Handler diffing (add, remove, update)
//   * Children diffing (add, remove, clear, reorder)
//   * Text node diffing
//   * Raw HTML diffing
//   * Memo/LazyMemo key comparison
//   * LIS algorithm correctness
//   * Head/tail skip optimization
//   * SetChildrenHtml fast path (0->N children)
//   * Complete replacement fast path
// =============================================================================

using Picea.Abies.DOM;
using Attribute = Picea.Abies.DOM.Attribute;

namespace Picea.Abies.Tests;

[NotInParallel("shared-dom-state")]
public class DiffTests
{
    // =========================================================================
    // Initial Render
    // =========================================================================

    [Test]
    public async Task Diff_NullOldNode_EmitsAddRoot()
    {
        var node = new Element("e1", "div", []);

        var patches = Operations.Diff(null, node);

        await Assert.That(patches).Count().IsEqualTo(1);
        var addRoot = patches[0];
        await Assert.That(addRoot).IsTypeOf<AddRoot>();
        var root = (AddRoot)addRoot;
        await Assert.That(root.Element.Tag).IsEqualTo("div");
    }

    // =========================================================================
    // Identical Nodes — No Patches
    // =========================================================================

    [Test]
    public async Task Diff_IdenticalElements_NoPatch()
    {
        var node = new Element("e1", "div", [new Attribute("a1", "class", "x")]);

        var patches = Operations.Diff(node, node);

        await Assert.That(patches).IsEmpty();
    }

    [Test]
    public async Task Diff_EqualButDifferentInstances_NoPatch()
    {
        var old = new Element("e1", "div", [new Attribute("a1", "class", "x")]);
        var @new = new Element("e1", "div", [new Attribute("a1", "class", "x")]);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).IsEmpty();
    }

    // =========================================================================
    // Element Replacement
    // =========================================================================

    [Test]
    public async Task Diff_DifferentTags_EmitsAddRoot()
    {
        var old = new Element("e1", "div", []);
        var @new = new Element("e1", "span", []);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).Count().IsEqualTo(1);
        var addRoot = patches[0];
        await Assert.That(addRoot).IsTypeOf<AddRoot>();
    }

    [Test]
    public async Task Diff_DifferentTagsNested_EmitsReplaceChild()
    {
        var oldChild = new Element("c1", "div", []);
        var newChild = new Element("c1", "span", []);
        var oldParent = new Element("p1", "section", [], oldChild);
        var newParent = new Element("p1", "section", [], newChild);

        var patches = Operations.Diff(oldParent, newParent);

        await Assert.That(patches.Any(p => p is ReplaceChild)).IsTrue();
    }

    // =========================================================================
    // Attribute Diffing
    // =========================================================================

    [Test]
    public async Task Diff_AddAttribute_EmitsAddAttribute()
    {
        var old = new Element("e1", "div", []);
        var @new = new Element("e1", "div", [new Attribute("a1", "class", "active")]);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).Count().IsEqualTo(1);
        var add = patches[0];
        await Assert.That(add).IsTypeOf<AddAttribute>();
        var addAttr = (AddAttribute)add;
        await Assert.That(addAttr.Attribute.Name).IsEqualTo("class");
        await Assert.That(addAttr.Attribute.Value).IsEqualTo("active");
    }

    [Test]
    public async Task Diff_RemoveAttribute_EmitsRemoveAttribute()
    {
        var old = new Element("e1", "div", [new Attribute("a1", "class", "active")]);
        var @new = new Element("e1", "div", []);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).Count().IsEqualTo(1);
        var remove = patches[0];
        await Assert.That(remove).IsTypeOf<RemoveAttribute>();
    }

    [Test]
    public async Task Diff_UpdateAttributeValue_EmitsUpdateAttribute()
    {
        var old = new Element("e1", "div", [new Attribute("a1", "class", "old")]);
        var @new = new Element("e1", "div", [new Attribute("a1", "class", "new")]);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).Count().IsEqualTo(1);
        var update = patches[0];
        await Assert.That(update).IsTypeOf<UpdateAttribute>();
        var updateAttr = (UpdateAttribute)update;
        await Assert.That(updateAttr.Value).IsEqualTo("new");
    }

    [Test]
    public async Task Diff_SameAttributes_NoPatch()
    {
        var old = new Element("e1", "div",
        [
            new Attribute("a1", "class", "x"),
            new Attribute("a2", "title", "y")
        ]);
        var @new = new Element("e1", "div",
        [
            new Attribute("a1", "class", "x"),
            new Attribute("a2", "title", "y")
        ]);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).IsEmpty();
    }

    // =========================================================================
    // Handler Diffing
    // =========================================================================

    [Test]
    public async Task Diff_AddHandler_EmitsAddHandler()
    {
        var old = new Element("e1", "button", []);
        var handler = new Handler("click", "cmd-1", null, "h1");
        var @new = new Element("e1", "button", [handler]);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).Count().IsEqualTo(1);
        var add = patches[0];
        await Assert.That(add).IsTypeOf<AddHandler>();
        var addHandler = (AddHandler)add;
        await Assert.That(addHandler.Handler.EventName).IsEqualTo("click");
        await Assert.That(addHandler.Handler.CommandId).IsEqualTo("cmd-1");
    }

    [Test]
    public async Task Diff_RemoveHandler_EmitsRemoveHandler()
    {
        var handler = new Handler("click", "cmd-1", null, "h1");
        var old = new Element("e1", "button", [handler]);
        var @new = new Element("e1", "button", []);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).Count().IsEqualTo(1);
        var remove = patches[0];
        await Assert.That(remove).IsTypeOf<RemoveHandler>();
    }

    [Test]
    public async Task Diff_UpdateHandler_EmitsUpdateHandler()
    {
        var oldHandler = new Handler("click", "cmd-1", null, "h1");
        var newHandler = new Handler("click", "cmd-2", null, "h1");
        var old = new Element("e1", "button", [oldHandler]);
        var @new = new Element("e1", "button", [newHandler]);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).Count().IsEqualTo(1);
        var update = patches[0];
        await Assert.That(update).IsTypeOf<UpdateHandler>();
        var updateHandler = (UpdateHandler)update;
        await Assert.That(updateHandler.OldHandler.CommandId).IsEqualTo("cmd-1");
        await Assert.That(updateHandler.NewHandler.CommandId).IsEqualTo("cmd-2");
    }

    [Test]
    public async Task Diff_HandlerNameIsFullAttributeName()
    {
        var handler = new Handler("click", "cmd-1", null, "h1");
        var old = new Element("e1", "button", [handler]);

        var newHandler = new Handler("click", "cmd-2", null, "h1");
        var @new = new Element("e1", "button", [newHandler]);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).Count().IsEqualTo(1);
        var update = (UpdateHandler)patches[0];
        await Assert.That(update.OldHandler.Name).IsEqualTo("data-event-click");
        await Assert.That(update.NewHandler.Name).IsEqualTo("data-event-click");
    }

    // =========================================================================
    // Children Diffing — Basic
    // =========================================================================

    [Test]
    public async Task Diff_AddChild_EmitsSetChildrenHtml()
    {
        var old = new Element("e1", "div", []);
        var @new = new Element("e1", "div", [], new Element("c1", "span", []));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is SetChildrenHtml)).IsTrue();
    }

    [Test]
    public async Task Diff_RemoveAllChildren_EmitsClearChildren()
    {
        var old = new Element("e1", "div", [],
            new Element("c1", "span", []),
            new Element("c2", "span", []));
        var @new = new Element("e1", "div", []);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is ClearChildren)).IsTrue();
    }

    [Test]
    public async Task Diff_AppendChild_EmitsAppendChildrenHtml()
    {
        var old = new Element("e1", "div", [],
            new Element("c1", "span", []));
        var @new = new Element("e1", "div", [],
            new Element("c1", "span", []),
            new Element("c2", "span", []));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is AppendChildrenHtml)).IsTrue();
    }

    [Test]
    public async Task Diff_RemoveChild_EmitsRemoveChild()
    {
        var old = new Element("e1", "div", [],
            new Element("c1", "span", []),
            new Element("c2", "span", []));
        var @new = new Element("e1", "div", [],
            new Element("c1", "span", []));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is RemoveChild)).IsTrue();
    }

    // =========================================================================
    // Children Diffing — Keyed Reorder (LIS)
    // =========================================================================

    [Test]
    public async Task Diff_SwapTwoChildren_EmitsMoveChild()
    {
        var old = new Element("e1", "ul", [],
            new Element("c1", "li", [], new Text("t1", "A")),
            new Element("c2", "li", [], new Text("t2", "B")));
        var @new = new Element("e1", "ul", [],
            new Element("c2", "li", [], new Text("t2", "B")),
            new Element("c1", "li", [], new Text("t1", "A")));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is MoveChild)).IsTrue();
    }

    [Test]
    public async Task Diff_SwapTwoInThousand_MinimalMoves()
    {
        var oldChildren = Enumerable.Range(0, 1000)
            .Select(i => (Node)new Element($"c{i}", "li", [], new Text($"t{i}", $"Item {i}")))
            .ToArray();

        var newOrder = (int[])[0, 998, .. Enumerable.Range(2, 996), 1, 999];
        var newChildren = newOrder
            .Select(i => (Node)new Element($"c{i}", "li", [], new Text($"t{i}", $"Item {i}")))
            .ToArray();

        var old = new Element("e1", "ul", [], oldChildren);
        var @new = new Element("e1", "ul", [], newChildren);

        var patches = Operations.Diff(old, @new);

        var moveCount = patches.Count(p => p is MoveChild);
        await Assert.That(moveCount).IsEqualTo(2);
    }

    [Test]
    public async Task Diff_ReverseOrder_EmitsMoves()
    {
        var old = new Element("e1", "ul", [],
            new Element("c1", "li", []),
            new Element("c2", "li", []),
            new Element("c3", "li", []));
        var @new = new Element("e1", "ul", [],
            new Element("c3", "li", []),
            new Element("c2", "li", []),
            new Element("c1", "li", []));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is AddChild)).IsFalse();
        await Assert.That(patches.Any(p => p is RemoveChild)).IsFalse();
        await Assert.That(patches.Any(p => p is MoveChild)).IsTrue();
    }

    // =========================================================================
    // Children Diffing — Complete Replacement Fast Path
    // =========================================================================

    [Test]
    public async Task Diff_AllDifferentKeys_EmitsClearAndSetChildrenHtml()
    {
        var oldChildren = Enumerable.Range(0, 10)
            .Select(i => (Node)new Element($"a{i}", "li", []))
            .ToArray();
        var newChildren = Enumerable.Range(0, 10)
            .Select(i => (Node)new Element($"b{i}", "li", []))
            .ToArray();

        var old = new Element("e1", "ul", [], oldChildren);
        var @new = new Element("e1", "ul", [], newChildren);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is ClearChildren)).IsTrue();
        await Assert.That(patches.Any(p => p is SetChildrenHtml)).IsTrue();
    }

    // =========================================================================
    // Children Diffing — Head/Tail Skip
    // =========================================================================

    [Test]
    public async Task Diff_AppendToEnd_HeadSkipMatchesExisting()
    {
        var old = new Element("e1", "div", [],
            new Element("c1", "p", []),
            new Element("c2", "p", []));
        var @new = new Element("e1", "div", [],
            new Element("c1", "p", []),
            new Element("c2", "p", []),
            new Element("c3", "p", []));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is AppendChildrenHtml)).IsTrue();
        await Assert.That(patches.Any(p => p is RemoveChild)).IsFalse();
        await Assert.That(patches.Any(p => p is MoveChild)).IsFalse();
    }

    [Test]
    public async Task Diff_RemoveFromEnd_HeadSkipMatchesRemaining()
    {
        var old = new Element("e1", "div", [],
            new Element("c1", "p", []),
            new Element("c2", "p", []),
            new Element("c3", "p", []));
        var @new = new Element("e1", "div", [],
            new Element("c1", "p", []),
            new Element("c2", "p", []));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is RemoveChild)).IsTrue();
        await Assert.That(patches.Any(p => p is AddChild)).IsFalse();
    }

    // =========================================================================
    // Text Node Diffing
    // =========================================================================

    [Test]
    public async Task Diff_UpdateText_EmitsUpdateText()
    {
        var old = new Element("e1", "p", [], new Text("t1", "old text"));
        var @new = new Element("e1", "p", [], new Text("t1", "new text"));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is UpdateText)).IsTrue();
    }

    [Test]
    public async Task Diff_SameText_NoPatch()
    {
        var old = new Element("e1", "p", [], new Text("t1", "same"));
        var @new = new Element("e1", "p", [], new Text("t1", "same"));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).IsEmpty();
    }

    [Test]
    public async Task Diff_UpdateText_MixedContent_TargetsSpecificTextNode()
    {
        var old = new Element("e1", "p", [],
            new Text("t1", "prefix"),
            new Element("c1", "strong", [], new Text("t3", "middle")),
            new Text("t2", "old suffix"));
        var @new = new Element("e1", "p", [],
            new Text("t1", "prefix"),
            new Element("c1", "strong", [], new Text("t3", "middle")),
            new Text("t2", "new suffix"));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Count).IsEqualTo(1);
        await Assert.That(patches[0] is UpdateText).IsTrue();

        var patch = (UpdateText)patches[0];
        await Assert.That(patch.Parent.Id).IsEqualTo("e1");
        await Assert.That(patch.Node.Id).IsEqualTo("t2");
        await Assert.That(patch.Text).IsEqualTo("new suffix");
        await Assert.That(patch.NewId).IsEqualTo("t2");
    }

    [Test]
    public async Task Diff_RemoveText_MixedContent_TargetsSpecificTextNode()
    {
        var old = new Element("e1", "p", [],
            new Text("t1", "prefix"),
            new Element("c1", "strong", [], new Text("t3", "middle")),
            new Text("t2", "remove me"));
        var @new = new Element("e1", "p", [],
            new Text("t1", "prefix"),
            new Element("c1", "strong", [], new Text("t3", "middle")));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Count).IsEqualTo(1);
        await Assert.That(patches[0] is RemoveText).IsTrue();

        var patch = (RemoveText)patches[0];
        await Assert.That(patch.Parent.Id).IsEqualTo("e1");
        await Assert.That(patch.Child.Id).IsEqualTo("t2");
    }

    // =========================================================================
    // Raw HTML Diffing
    // =========================================================================

    [Test]
    public async Task Diff_UpdateRawHtml_EmitsUpdateRaw()
    {
        var old = new Element("e1", "div", [], new RawHtml("r1", "<b>old</b>"));
        var @new = new Element("e1", "div", [], new RawHtml("r1", "<b>new</b>"));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is UpdateRaw)).IsTrue();
    }

    // =========================================================================
    // Memo Node Diffing
    // =========================================================================

    [Test]
    public async Task Diff_MemoSameKey_NoPatch()
    {
        var inner = new Element("e1", "div", []);
        var old = new Memo<int>("m1", 42, inner);
        var @new = new Memo<int>("m1", 42, inner);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).IsEmpty();
    }

    [Test]
    public async Task Diff_MemoDifferentKey_EmitsPatches()
    {
        var oldInner = new Element("e1", "div", [new Attribute("a1", "class", "old")]);
        var newInner = new Element("e1", "div", [new Attribute("a1", "class", "new")]);
        var old = new Memo<int>("m1", 1, oldInner);
        var @new = new Memo<int>("m1", 2, newInner);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).IsNotEmpty();
    }

    // =========================================================================
    // LazyMemo Node Diffing
    // =========================================================================

    [Test]
    public async Task Diff_LazyMemoSameKey_NoPatch()
    {
        var inner = new Element("e1", "span", []);
        var old = new LazyMemo<string>("l1", "key1", () => inner);
        var @new = new LazyMemo<string>("l1", "key1", () => inner);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).IsEmpty();
    }

    [Test]
    public async Task Diff_LazyMemoDifferentKey_EmitsPatches()
    {
        var old = new LazyMemo<string>("l1", "key1",
            () => new Element("e1", "span", [new Attribute("a1", "class", "old")]));
        var @new = new LazyMemo<string>("l1", "key2",
            () => new Element("e1", "span", [new Attribute("a1", "class", "new")]));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).IsNotEmpty();
    }

    // =========================================================================
    // Void Elements — Children are ignored
    // =========================================================================

    [Test]
    public async Task Diff_VoidElementNoChildren_NoPatch()
    {
        var old = new Element("e1", "br", []);
        var @new = new Element("e1", "br", []);

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches).IsEmpty();
    }

    // =========================================================================
    // Mixed operations — attributes + children in same diff
    // =========================================================================

    [Test]
    public async Task Diff_AttributeAndChildrenChange_EmitsBoth()
    {
        var old = new Element("e1", "div",
            [new Attribute("a1", "class", "old")],
            new Element("c1", "span", []));
        var @new = new Element("e1", "div",
            [new Attribute("a1", "class", "new")],
            new Element("c1", "span", []),
            new Element("c2", "span", []));

        var patches = Operations.Diff(old, @new);

        await Assert.That(patches.Any(p => p is UpdateAttribute)).IsTrue();
        await Assert.That(patches.Any(p => p is AppendChildrenHtml or AddChild)).IsTrue();
    }

    // =========================================================================
    // Event Handler Cache (FrozenDictionary)
    // =========================================================================

    [Test]
    public async Task Handler_DataEventAttributeNameIsCached()
    {
        var h1 = new Handler("click", "cmd-1", null, "h1");
        var h2 = new Handler("click", "cmd-2", null, "h2");

        await Assert.That(h1.Name).IsEqualTo(h2.Name);
        await Assert.That(h1.Name).IsEqualTo("data-event-click");
    }

    // =========================================================================
    // View Cache — lazy memo returns same reference for same key
    // =========================================================================

    [Test]
    public async Task LazyMemo_SameKeyReturnsSameReference_EnablesRefEqualBailout()
    {
        var inner = new Element("e1", "div", []);
        var lazy1 = new LazyMemo<int>("l1", 42, () => inner);
        var lazy2 = new LazyMemo<int>("l1", 42, () => inner);

        // Keys match via value equality — diff should produce no patches
        var patches = Operations.Diff(lazy1, lazy2);
        await Assert.That(patches).IsEmpty();
    }
}
