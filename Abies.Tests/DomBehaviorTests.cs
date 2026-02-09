using System.Runtime.Versioning;
using Abies.DOM;
using DOMAttribute = Abies.DOM.Attribute;

namespace Abies.Tests;

[SupportedOSPlatform("browser")]
public class DomBehaviorTests
{
    private record DummyMessage() : Message;

    [Fact]
    public void AddRoot_ShouldRenderCorrectly()
    {
        var newDom = new Element("1", "div", [],
            new Text("2", "Hello"));

        var patches = Operations.Diff(null, newDom);
        var result = ApplyPatches(null, patches, null);

        Assert.Equal(Render.Html(newDom), Render.Html(result!));
    }

    [Fact]
    public void ReplaceChild_ShouldUpdateTree()
    {
        var oldDom = new Element("1", "div", [],
            new Element("2", "span", [], new Text("3", "Old")));

        var newDom = new Element("1", "div", [],
            new Element("4", "p", [], new Text("5", "New")));

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(newDom), Render.Html(result));
    }

    [Fact]
    public void AttributeChanges_ShouldReflectInResult()
    {
        var oldDom = new Element("1", "button",
            new DOMAttribute[] { new DOMAttribute("a1", "class", "btn") },
            []);

        var newDom = new Element("1", "button",
            new DOMAttribute[]
            {
                new DOMAttribute("a1", "class", "btn-primary"),
                new Handler("click", "cmd1", new DummyMessage(), "h1")
            },
            []);

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(newDom), Render.Html(result));
    }

    [Fact]
    public void AttributeIdChange_ShouldNotRemoveAttribute()
    {
        var oldDom = new Element("1", "div",
            new DOMAttribute[] { new DOMAttribute("a1", "class", "foo") },
            []);

        var newDom = new Element("1", "div",
            new DOMAttribute[] { new DOMAttribute("a2", "class", "foo") },
            []);

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(newDom), Render.Html(result));
    }

    [Fact]
    public void AttributeIdChange_WithValueChange_ShouldUpdateAttribute()
    {
        var oldDom = new Element("1", "div",
            new DOMAttribute[] { new DOMAttribute("a1", "class", "inactive") },
            []);

        var newDom = new Element("1", "div",
            new DOMAttribute[] { new DOMAttribute("a2", "class", "active") },
            []);

        var alignedNew = PreserveIdsForTest(oldDom, newDom);
        var patches = Operations.Diff(oldDom, alignedNew);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(alignedNew), Render.Html(result));
        Assert.Contains(patches, p => p is UpdateAttribute);
        Assert.DoesNotContain(patches, p => p is RemoveAttribute);
    }

    [Fact]
    public void HandlerIdChange_ShouldUpdateHandler()
    {
        var oldDom = new Element("1", "button",
            new DOMAttribute[] { new Handler("click", "cmd-old", new DummyMessage(), "h1") },
            []);

        var newDom = new Element("1", "button",
            new DOMAttribute[] { new Handler("click", "cmd-new", new DummyMessage(), "h2") },
            []);

        var alignedNew = PreserveIdsForTest(oldDom, newDom);
        var patches = Operations.Diff(oldDom, alignedNew);

        Assert.Contains(patches, p => p is UpdateHandler);
        Assert.DoesNotContain(patches, p => p is RemoveHandler);
        Assert.DoesNotContain(patches, p => p is AddHandler);
    }

    [Fact]
    public void Render_ShouldIncludeElementIds()
    {
        var dom = new Element("el1", "div", [],
            new Element("child", "span", [], new Text("t", "hi")));

        var html = Render.Html(dom);

        Assert.Contains("id=\"el1\"", html);
        Assert.Contains("id=\"child\"", html);
    }

    [Fact]
    public void TextUpdate_ShouldUpdateTextContent()
    {
        var oldDom = new Element("1", "h1", [],
            new Text("2", "Sign up"));

        var newDom = new Element("1", "h1", [],
            new Text("3", "Sign in"));

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(newDom), Render.Html(result));

        // Verify that an UpdateText patch was generated
        Assert.Contains(patches, p => p is UpdateText);
        var textPatch = patches.OfType<UpdateText>().First();
        Assert.Equal("Sign in", textPatch.Text);
    }

    [Fact]
    public void TextUpdate_WithPreservedIds_ShouldUpdateTextContent()
    {
        // Simulate the ID preservation scenario
        var oldDom = new Element("1", "h1", [],
            new Text("2", "Sign up"));

        var newDomBeforePreservation = new Element("1", "h1", [],
            new Text("3", "Sign in"));

        // Simulate what PreserveIds does - preserve the old text ID but use new text content
        var newDomAfterPreservation = new Element("1", "h1", [],
            new Text("2", "Sign in")); // Same ID as old, new content

        var patches = Operations.Diff(oldDom, newDomAfterPreservation);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(newDomAfterPreservation), Render.Html(result));

        // Verify that an UpdateText patch was generated
        Assert.Contains(patches, p => p is UpdateText);
        var textPatch = patches.OfType<UpdateText>().First();
        Assert.Equal("Sign in", textPatch.Text);
        Assert.Equal("2", textPatch.Node.Id); // Should use the preserved ID
    }

    [Fact]
    public void KeyedChildren_Reorder_ShouldUseMoveChild()
    {
        // Per ADR-016: Element Id is used for keyed diffing, not data-key attribute.
        // When children have different IDs in different order, the algorithm detects reordering.
        // With LIS optimization, reordering uses MoveChild instead of Remove+Add.
        // 
        // IMPORTANT: In a pure reorder, the NEW virtual DOM should have the SAME IDs as the old DOM.
        // This is because Abies identifies elements by their ID, and a reorder doesn't change IDs.
        // The MoveChild operation moves elements by their existing IDs, not creating new ones.
        var oldDom = new Element("root", "div", [],
            new Element("item-a", "div", new DOMAttribute[]
            {
                new DOMAttribute("ca", "class", "item")
            }, new Text("ta", "A")),
            new Element("item-b", "div", new DOMAttribute[]
            {
                new DOMAttribute("cb", "class", "item")
            }, new Text("tb", "B")));

        // For a pure reorder test, new DOM has SAME IDs but DIFFERENT ORDER
        // This simulates what happens when the same elements are rendered in a different order
        var newDom = new Element("root", "div", [],
            new Element("item-b", "div", new DOMAttribute[]
            {
                new DOMAttribute("cb", "class", "item")  // Same IDs as old
            }, new Text("tb", "B")),
            new Element("item-a", "div", new DOMAttribute[]
            {
                new DOMAttribute("ca", "class", "item")  // Same IDs as old
            }, new Text("ta", "A")));

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(newDom), Render.Html(result));
        // With LIS optimization, reordering uses MoveChild instead of Remove+Add
        Assert.Contains(patches, p => p is MoveChild);
        Assert.DoesNotContain(patches, p => p is RemoveChild);
        Assert.DoesNotContain(patches, p => p is AddChild);
    }

    [Fact]
    public void KeyedChildren_SameOrder_ShouldDiffInPlace()
    {
        // Per ADR-016: Element Id is used for keyed diffing.
        // When children have the same IDs in the same order, diff in place.
        var oldDom = new Element("root", "div", [],
            new Element("item-a", "div", new DOMAttribute[]
            {
                new DOMAttribute("ca", "class", "item")
            }, new Text("ta", "Old")),
            new Element("item-b", "div", new DOMAttribute[]
            {
                new DOMAttribute("cb", "class", "item")
            }, new Text("tb", "Old")));

        var newDom = new Element("root", "div", [],
            new Element("item-a", "div", new DOMAttribute[]
            {
                new DOMAttribute("ca2", "class", "item")
            }, new Text("ta2", "New")),
            new Element("item-b", "div", new DOMAttribute[]
            {
                new DOMAttribute("cb2", "class", "item")
            }, new Text("tb2", "New")));

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(newDom), Render.Html(result));
        // Same IDs, same order: should update in place, not remove/add
        Assert.DoesNotContain(patches, p => p is RemoveChild);
        Assert.DoesNotContain(patches, p => p is AddChild);
    }

    [Fact]
    public void KeyedChildren_DifferentIds_ShouldReplaceList()
    {
        // Per ADR-016: When child IDs change completely, remove old and add new.
        // This is the navigation scenario: unauthenticated (login, register) -> authenticated (editor, settings, profile)
        var oldDom = new Element("root", "ul", [],
            new Element("nav-home", "li", [], new Text("t1", "Home")),
            new Element("nav-login", "li", [], new Text("t2", "Sign in")),
            new Element("nav-register", "li", [], new Text("t3", "Sign up")));

        var newDom = new Element("root", "ul", [],
            new Element("nav-home", "li", [], new Text("t1", "Home")),
            new Element("nav-editor", "li", [], new Text("t4", "New Article")),
            new Element("nav-settings", "li", [], new Text("t5", "Settings")),
            new Element("nav-profile-bob", "li", [], new Text("t6", "bob")));

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(newDom), Render.Html(result));

        // Should remove nav-login and nav-register
        var removePatches = patches.OfType<RemoveChild>().ToList();
        Assert.Equal(2, removePatches.Count);
        Assert.Contains(removePatches, p => p.Child.Id == "nav-login");
        Assert.Contains(removePatches, p => p.Child.Id == "nav-register");

        // Should add nav-editor, nav-settings, nav-profile-bob
        var addPatches = patches.OfType<AddChild>().ToList();
        Assert.Equal(3, addPatches.Count);
        Assert.Contains(addPatches, p => p.Child.Id == "nav-editor");
        Assert.Contains(addPatches, p => p.Child.Id == "nav-settings");
        Assert.Contains(addPatches, p => p.Child.Id == "nav-profile-bob");
    }

    [Fact]
    public void KeyedChildren_SharedHomeLink_ShouldBePreserved()
    {
        // Per ADR-016: Elements with matching IDs should be diffed in place, not replaced.
        // The "Home" link has the same ID in both states, so it should be updated, not replaced.
        var oldDom = new Element("root", "ul", [],
            new Element("nav-home", "li", new DOMAttribute[] { new DOMAttribute("c1", "class", "nav-item") },
                new Text("t1", "Home")),
            new Element("nav-login", "li", new DOMAttribute[] { new DOMAttribute("c2", "class", "nav-item") },
                new Text("t2", "Sign in")));

        var newDom = new Element("root", "ul", [],
            new Element("nav-home", "li", new DOMAttribute[] { new DOMAttribute("c1", "class", "nav-item active") },
                new Text("t1", "Home")),
            new Element("nav-editor", "li", new DOMAttribute[] { new DOMAttribute("c3", "class", "nav-item") },
                new Text("t3", "New Article")));

        var patches = Operations.Diff(oldDom, newDom);

        // nav-home should have an attribute update (class changed), not be removed/added
        var removePatches = patches.OfType<RemoveChild>().ToList();
        var addPatches = patches.OfType<AddChild>().ToList();

        // Only nav-login should be removed
        Assert.Single(removePatches);
        Assert.Equal("nav-login", removePatches[0].Child.Id);

        // Only nav-editor should be added
        Assert.Single(addPatches);
        Assert.Equal("nav-editor", addPatches[0].Child.Id);

        // nav-home's class should be updated
        var attrPatches = patches.OfType<UpdateAttribute>().ToList();
        Assert.Contains(attrPatches, p => p.Element.Id == "nav-home" && p.Attribute.Name == "class");
    }

    [Fact]
    public void LegacyDataKey_ShouldStillWork()
    {
        // Backward compatibility: data-key attribute should still work for keyed diffing
        // even though ADR-016 recommends using element Id instead.
        var oldDom = new Element("root", "div", [],
            new Element("1", "div", new DOMAttribute[]
            {
                new DOMAttribute("ka", "data-key", "a"),
                new DOMAttribute("ca", "class", "item")
            }, new Text("ta", "A")),
            new Element("2", "div", new DOMAttribute[]
            {
                new DOMAttribute("kb", "data-key", "b"),
                new DOMAttribute("cb", "class", "item")
            }, new Text("tb", "B")));

        var newDom = new Element("root", "div", [],
            new Element("3", "div", new DOMAttribute[]
            {
                new DOMAttribute("kb2", "data-key", "b"),
                new DOMAttribute("cb2", "class", "item")
            }, new Text("tb2", "B")),
            new Element("4", "div", new DOMAttribute[]
            {
                new DOMAttribute("ka2", "data-key", "a"),
                new DOMAttribute("ca2", "class", "item")
            }, new Text("ta2", "A")));

        var patches = Operations.Diff(oldDom, newDom);

        // Keys are reordered, so should use MoveChild (optimized reordering)
        Assert.Contains(patches, p => p is MoveChild);
    }

    private static Node? ApplyPatches(Node? root, IEnumerable<Patch> patches, Node? initialRoot)
    {
        var current = root;
        foreach (var patch in patches)
        {
            current = ApplyPatch(current, patch, initialRoot);
        }
        return current;
    }

    private static Node? ApplyPatch(Node? root, Patch patch, Node? initialRoot)
    {
        return patch switch
        {
            AddRoot ar => ar.Element,
            ReplaceChild rc => ReplaceNode(root!, rc.OldElement, rc.NewElement),
            AddChild ac => UpdateElement(root!, ac.Parent.Id, e => e with { Children = e.Children.Append(ac.Child).ToArray() }),
            RemoveChild rc => UpdateElement(root!, rc.Parent.Id, e => e with { Children = e.Children.Where(c => c.Id != rc.Child.Id).ToArray() }),
            MoveChild mc => UpdateElement(root!, mc.Parent.Id, e =>
            {
                // Remove child from current position
                var children = e.Children.Where(c => c.Id != mc.Child.Id).ToList();
                // Find position to insert (before the specified element, or at end)
                int insertIndex = mc.BeforeId != null
                    ? children.FindIndex(c => c.Id == mc.BeforeId)
                    : children.Count;
                if (insertIndex < 0)
                {
                    insertIndex = children.Count;
                }

                children.Insert(insertIndex, mc.Child);
                return e with { Children = children.ToArray() };
            }),
            UpdateAttribute ua => UpdateElement(root!, ua.Element.Id, e =>
            {
                var updated = false;
                var attrs = e.Attributes.Select(a =>
                {
                    if (a.Name == ua.Attribute.Name)
                    {
                        updated = true;
                        return a with { Value = ua.Value };
                    }
                    return a;
                }).ToArray();
                if (!updated)
                {
                    attrs = attrs.Append(ua.Attribute with { Value = ua.Value }).ToArray();
                }

                return e with { Attributes = attrs };
            }),
            AddAttribute aa => UpdateElement(root!, aa.Element.Id, e =>
            {
                var attrs = e.Attributes.Where(a => a.Name != aa.Attribute.Name).Append(aa.Attribute).ToArray();
                return e with { Attributes = attrs };
            }),
            RemoveAttribute ra => UpdateElement(root!, ra.Element.Id, e => e with { Attributes = e.Attributes.Where(a => a.Name != ra.Attribute.Name).ToArray() }),
            AddHandler ah => UpdateElement(root!, ah.Element.Id, e => e with { Attributes = e.Attributes.Append(ah.Handler).ToArray() }),
            RemoveHandler rh => UpdateElement(root!, rh.Element.Id, e => e with { Attributes = e.Attributes.Where(a => a.Id != rh.Handler.Id).ToArray() }),
            UpdateText ut => ReplaceNode(root!, ut.Node, new Text(ut.NewId, ut.Text)),
            _ => root
        };
    }

    private static Node ReplaceNode(Node node, Node target, Node newNode)
    {
        if (ReferenceEquals(node, target) || node.Id == target.Id)
        {
            return newNode;
        }

        if (node is Element el)
        {
            var newChildren = el.Children.Select(c => ReplaceNode(c, target, newNode)).ToArray();
            return el with { Children = newChildren };
        }
        return node;
    }

    private static Node PreserveIdsForTest(Node? oldNode, Node newNode)
    {
        if (oldNode is Element oldElement && newNode is Element newElement && oldElement.Tag == newElement.Tag)
        {
            var attrs = new DOMAttribute[newElement.Attributes.Length];
            for (int i = 0; i < newElement.Attributes.Length; i++)
            {
                var attr = newElement.Attributes[i];
                var oldAttr = Array.Find(oldElement.Attributes, a => a.Name == attr.Name);
                var attrId = oldAttr?.Id ?? attr.Id;

                if (attr.Name == "id")
                {
                    attrs[i] = attr with { Id = attrId, Value = oldElement.Id };
                }
                else
                {
                    attrs[i] = attr with { Id = attrId };
                }
            }

            var children = new Node[newElement.Children.Length];
            for (int i = 0; i < newElement.Children.Length; i++)
            {
                var oldChild = i < oldElement.Children.Length ? oldElement.Children[i] : null;
                children[i] = PreserveIdsForTest(oldChild, newElement.Children[i]);
            }

            return new Element(oldElement.Id, newElement.Tag, attrs, children);
        }

        if (oldNode is Text oldText && newNode is Text newText)
        {
            return new Text(oldText.Id, newText.Value);
        }

        if (newNode is Element newElem)
        {
            var children = new Node[newElem.Children.Length];
            for (int i = 0; i < newElem.Children.Length; i++)
            {
                children[i] = PreserveIdsForTest(null, newElem.Children[i]);
            }

            return new Element(newElem.Id, newElem.Tag, newElem.Attributes, children);
        }

        return newNode;
    }

    private static Node UpdateElement(Node node, string targetId, Func<Element, Element> update)
    {
        if (node is Element el)
        {
            if (el.Id == targetId)
            {
                var updated = update(el);
                return updated;
            }

            var newChildren = el.Children.Select(c => UpdateElement(c, targetId, update)).ToArray();
            return el with { Children = newChildren };
        }
        return node;
    }

    #region Memo Node Tests

    [Fact]
    public void MemoNode_ShouldRenderCachedContent()
    {
        // Create a simple element wrapped in memo
        var innerElement = new Element("inner-1", "div", [], new Text("text-1", "Hello"));
        var memoNode = new Memo<string>("memo-1", "key-1", innerElement);

        // Render should produce the inner element's HTML
        var html = Render.Html(memoNode);

        Assert.Contains("Hello", html);
        Assert.Contains("div", html);
        Assert.Contains("inner-1", html);
        // The memo wrapper itself should not appear in output
        Assert.DoesNotContain("memo-1", html);
    }

    [Fact]
    public void MemoNode_InParent_ShouldRenderCorrectly()
    {
        // Create a parent with memo children (simulating a list)
        var row1 = new Element("row-1", "tr", [], new Text("text-1", "Row 1"));
        var row2 = new Element("row-2", "tr", [], new Text("text-2", "Row 2"));
        var memo1 = new Memo<int>("m-1", 1, row1);
        var memo2 = new Memo<int>("m-2", 2, row2);

        var tbody = new Element("tbody-1", "tbody", [], memo1, memo2);

        var html = Render.Html(tbody);

        // Both rows should be rendered
        Assert.Contains("Row 1", html);
        Assert.Contains("Row 2", html);
        Assert.Contains("row-1", html);
        Assert.Contains("row-2", html);
        // Memo wrappers should not appear
        Assert.DoesNotContain("m-1", html);
        Assert.DoesNotContain("m-2", html);
    }

    [Fact]
    public void MemoNode_DiffWithSameKey_ShouldSkipSubtreeDiff()
    {
        // Old and new trees with same memo key
        var oldInner = new Element("inner-1", "div", [], new Text("text-1", "Old"));
        var newInner = new Element("inner-1", "div", [], new Text("text-1", "New")); // Different text!
        var oldMemo = new Memo<string>("m-1", "same-key", oldInner);
        var newMemo = new Memo<string>("m-1", "same-key", newInner);

        var patches = Operations.Diff(oldMemo, newMemo);

        // With same key, should produce NO patches (subtree is skipped)
        Assert.Empty(patches);
    }

    [Fact]
    public void MemoNode_DiffWithDifferentKey_ShouldDiffSubtrees()
    {
        // Old and new trees with different memo keys
        var oldInner = new Element("inner-1", "div", [], new Text("text-1", "Old"));
        var newInner = new Element("inner-1", "div", [], new Text("text-1", "New"));
        var oldMemo = new Memo<string>("m-1", "key-1", oldInner);
        var newMemo = new Memo<string>("m-1", "key-2", newInner);

        var patches = Operations.Diff(oldMemo, newMemo);

        // With different keys, should produce patches for the text change
        Assert.NotEmpty(patches);
        Assert.Contains(patches, p => p is UpdateText);
    }

    [Fact]
    public void MemoNode_InitialRender_ShouldCreateElements()
    {
        // Simulating initial render: diff from null to memo node
        var inner = new Element("row-1", "tr", [], new Text("text-1", "Hello"));
        var memoNode = new Memo<int>("m-1", 42, inner);
        var parent = new Element("tbody-1", "tbody", [], memoNode);

        var patches = Operations.Diff(null, parent);

        // Should have AddRoot patch
        Assert.NotEmpty(patches);
        Assert.Contains(patches, p => p is AddRoot);
    }

    [Fact]
    public void MemoNode_EmptyToFilled_ShouldCreateChildren()
    {
        // This simulates the benchmark scenario:
        // 1. Initial render: tbody with no children
        // 2. After "Create 1000 rows": tbody with 1000 memo-wrapped children
        var emptyTbody = new Element("tbody-1", "tbody", []);

        var row1 = new Element("row-1", "tr", [], new Text("t-1", "Row 1"));
        var row2 = new Element("row-2", "tr", [], new Text("t-2", "Row 2"));
        var memo1 = new Memo<int>("m-1", 1, row1);
        var memo2 = new Memo<int>("m-2", 2, row2);
        var filledTbody = new Element("tbody-1", "tbody", [], memo1, memo2);

        var patches = Operations.Diff(emptyTbody, filledTbody);

        // Should have AddChild patches for the new rows
        Assert.NotEmpty(patches);
        // The rows should be inserted as children
        Assert.Contains(patches, p => p is AddChild);
    }

    [Fact]
    public void MemoNode_InsertChildPatch_ShouldContainCachedContent()
    {
        // Verify that AddChild patches contain the actual element, not the memo wrapper
        var emptyTbody = new Element("tbody-1", "tbody", []);

        var row = new Element("row-1", "tr", [], new Text("t-1", "Hello"));
        var memoRow = new Memo<int>("m-1", 42, row);
        var filledTbody = new Element("tbody-1", "tbody", [], memoRow);

        var patches = Operations.Diff(emptyTbody, filledTbody);

        // Find the AddChild patch
        var addPatches = patches.OfType<AddChild>().ToList();
        Assert.Single(addPatches);
        var addPatch = addPatches[0];

        // The added child should be the actual element (row), not the memo
        Assert.Equal("row-1", addPatch.Child.Id);
    }

    #endregion

    #region Lazy Memo Node Tests

    [Fact]
    public void LazyMemoNode_ShouldDeferEvaluation()
    {
        // Create a lazy memo with a factory that tracks invocations
        int invocationCount = 0;
        Node Factory()
        {
            invocationCount++;
            return new Element("inner-1", "div", [], new Text("text-1", "Hello"));
        }

        var lazyMemo = new LazyMemo<string>("lazy-1", "key-1", Factory);

        // Factory should not be called at construction time
        Assert.Equal(0, invocationCount);

        // Evaluate should call the factory
        var result = lazyMemo.Evaluate();
        Assert.Equal(1, invocationCount);
        Assert.NotNull(result);
    }

    [Fact]
    public void LazyMemoNode_DiffWithSameKey_ShouldNotEvaluate()
    {
        // Create old lazy memo with a cached result
        int oldInvocationCount = 0;
        var oldCached = new Element("inner-1", "div", [], new Text("text-1", "Old"));
        var oldLazy = new LazyMemo<string>("lazy-1", "same-key", () =>
        {
            oldInvocationCount++;
            return oldCached;
        }, oldCached);

        // Create new lazy memo with same key
        int newInvocationCount = 0;
        var newLazy = new LazyMemo<string>("lazy-1", "same-key", () =>
        {
            newInvocationCount++;
            return new Element("inner-1", "div", [], new Text("text-1", "New"));
        });

        Operations.ResetMemoCounters();
        var patches = Operations.Diff(oldLazy, newLazy);

        // With same key, should produce NO patches (factory never called)
        Assert.Empty(patches);
        Assert.Equal(0, newInvocationCount);
        Assert.Equal(1, Operations.MemoHits);
        Assert.Equal(0, Operations.MemoMisses);
    }

    [Fact]
    public void LazyMemoNode_DiffWithDifferentKey_ShouldEvaluate()
    {
        // Create old lazy memo with a cached result
        var oldCached = new Element("inner-1", "div", [], new Text("text-1", "Old"));
        var oldLazy = new LazyMemo<string>("lazy-1", "key-1", () => oldCached, oldCached);

        // Create new lazy memo with different key
        int newInvocationCount = 0;
        var newLazy = new LazyMemo<string>("lazy-1", "key-2", () =>
        {
            newInvocationCount++;
            return new Element("inner-1", "div", [], new Text("text-1", "New"));
        });

        Operations.ResetMemoCounters();
        var patches = Operations.Diff(oldLazy, newLazy);

        // With different keys, should evaluate and produce patches
        Assert.Equal(1, newInvocationCount);
        Assert.NotEmpty(patches);
        Assert.Contains(patches, p => p is UpdateText);
        Assert.Equal(0, Operations.MemoHits);
        Assert.Equal(1, Operations.MemoMisses);
    }

    [Fact]
    public void LazyMemoNode_ShouldRenderContent()
    {
        // Create a lazy memo
        var inner = new Element("inner-1", "div", [], new Text("text-1", "Hello"));
        var lazyMemo = new LazyMemo<string>("lazy-1", "key-1", () => inner);

        // Render should evaluate and produce the inner element's HTML
        var html = Render.Html(lazyMemo);

        Assert.Contains("Hello", html);
        Assert.Contains("div", html);
        Assert.Contains("inner-1", html);
    }

    [Fact]
    public void LazyMemoNode_InParent_ShouldRenderCorrectly()
    {
        // Create a parent with lazy memo children (simulating a list)
        var row1 = new Element("row-1", "tr", [], new Text("text-1", "Row 1"));
        var row2 = new Element("row-2", "tr", [], new Text("text-2", "Row 2"));
        var lazy1 = new LazyMemo<int>("l-1", 1, () => row1);
        var lazy2 = new LazyMemo<int>("l-2", 2, () => row2);

        var tbody = new Element("tbody-1", "tbody", [], lazy1, lazy2);

        var html = Render.Html(tbody);

        // Both rows should be rendered
        Assert.Contains("Row 1", html);
        Assert.Contains("Row 2", html);
    }

    [Fact]
    public void LazyMemoNode_SelectScenario_ShouldMinimizeEvaluations()
    {
        // Simulate the benchmark select scenario:
        // 1000 rows, selecting row 5 (was unselected), unselecting row 999 (was selected)

        // Create old tree with row 5 unselected, row 999 selected
        var oldRows = new List<Node>();
        var oldFactoryCalls = new int[10];
        for (int i = 0; i < 10; i++)
        {
            int idx = i;
            bool wasSelected = i == 9; // Row 9 was selected
            var elem = new Element($"row-{i}", "tr",
                [new DOMAttribute($"class-{i}", "class", wasSelected ? "danger" : "")],
                new Text($"text-{i}", $"Row {i}"));
            oldRows.Add(new LazyMemo<(int, bool)>($"lazy-{i}", (i, wasSelected), () =>
            {
                oldFactoryCalls[idx]++;
                return elem;
            }, elem)); // Pre-populate with cached value
        }
        var oldTbody = new Element("tbody-1", "tbody", [], oldRows.ToArray());

        // Create new tree with row 5 selected, row 999 unselected
        var newRows = new List<Node>();
        var newFactoryCalls = new int[10];
        for (int i = 0; i < 10; i++)
        {
            int idx = i;
            bool isSelected = i == 5; // Row 5 is now selected
            var elem = new Element($"row-{i}", "tr",
                [new DOMAttribute($"class-{i}", "class", isSelected ? "danger" : "")],
                new Text($"text-{i}", $"Row {i}"));
            newRows.Add(new LazyMemo<(int, bool)>($"lazy-{i}", (i, isSelected), () =>
            {
                newFactoryCalls[idx]++;
                return elem;
            }));
        }
        var newTbody = new Element("tbody-1", "tbody", [], newRows.ToArray());

        Operations.ResetMemoCounters();
        var patches = Operations.Diff(oldTbody, newTbody);

        // Only rows 5 and 9 should have their factories called (key changed)
        // Rows 0-4, 6-8 should NOT have factories called (key unchanged)
        Assert.Equal(0, newFactoryCalls[0]);
        Assert.Equal(0, newFactoryCalls[1]);
        Assert.Equal(0, newFactoryCalls[2]);
        Assert.Equal(0, newFactoryCalls[3]);
        Assert.Equal(0, newFactoryCalls[4]);
        Assert.Equal(1, newFactoryCalls[5]); // Selected changed
        Assert.Equal(0, newFactoryCalls[6]);
        Assert.Equal(0, newFactoryCalls[7]);
        Assert.Equal(0, newFactoryCalls[8]);
        Assert.Equal(1, newFactoryCalls[9]); // Unselected changed

        // Should have 8 hits (unchanged rows) and 2 misses (changed rows)
        Assert.Equal(8, Operations.MemoHits);
        Assert.Equal(2, Operations.MemoMisses);

        // Should have patches for the 2 changed rows (class attribute change)
        Assert.NotEmpty(patches);
    }

    #endregion

    #region LIS Algorithm Tests (Swap Benchmark Scenario)

    [Fact]
    public void KeyedChildren_SwapTwoElements_ShouldOnlyMoveTwoElements()
    {
        // This test validates the LIS algorithm fix for the js-framework-benchmark swap scenario.
        // When swapping 2 elements in a 1000-element list, only 2 MoveChild patches should be generated.
        // Before the fix, the buggy LIS algorithm produced 999 MoveChild patches!

        const int listSize = 1000;

        // Create a list of 1000 elements with unique IDs
        var oldChildren = new Node[listSize];
        for (int i = 0; i < listSize; i++)
        {
            oldChildren[i] = new Element($"row-{i}", "tr", [], new Text($"text-{i}", $"Row {i}"));
        }
        var oldDom = new Element("tbody", "tbody", [], oldChildren);

        // Swap positions 1 and 998 (same as js-framework-benchmark)
        var newChildren = new Node[listSize];
        Array.Copy(oldChildren, newChildren, listSize);
        (newChildren[1], newChildren[998]) = (newChildren[998], newChildren[1]);
        var newDom = new Element("tbody", "tbody", [], newChildren);

        var patches = Operations.Diff(oldDom, newDom);

        // Count MoveChild patches
        var moveCount = patches.Count(p => p is MoveChild);

        // With correct LIS algorithm: only 2 elements need to move
        // LIS should be [0, 2, 3, 4, ..., 997, 999] (length 998)
        // Non-LIS elements: positions 1 and 998
        Assert.Equal(2, moveCount);

        // Should NOT have any Remove/Add patches for a pure reorder
        Assert.DoesNotContain(patches, p => p is RemoveChild);
        Assert.DoesNotContain(patches, p => p is AddChild);
    }

    [Fact]
    public void KeyedChildren_SwapAdjacentElements_ShouldOnlyMoveOneElement()
    {
        // Swapping adjacent elements should only require 1 move
        // Original: A, B, C, D
        // After swap B,C: A, C, B, D
        // LIS: [A, C, D] (length 3) or [A, B, D]
        // Either B or C needs to move, but not both

        var oldDom = new Element("root", "div", [],
            new Element("a", "div", [], new Text("ta", "A")),
            new Element("b", "div", [], new Text("tb", "B")),
            new Element("c", "div", [], new Text("tc", "C")),
            new Element("d", "div", [], new Text("td", "D")));

        var newDom = new Element("root", "div", [],
            new Element("a", "div", [], new Text("ta", "A")),
            new Element("c", "div", [], new Text("tc", "C")),
            new Element("b", "div", [], new Text("tb", "B")),
            new Element("d", "div", [], new Text("td", "D")));

        var patches = Operations.Diff(oldDom, newDom);
        var moveCount = patches.Count(p => p is MoveChild);

        // Should be exactly 1 move (either B moves after C, or C moves before B)
        Assert.Equal(1, moveCount);
        Assert.DoesNotContain(patches, p => p is RemoveChild);
        Assert.DoesNotContain(patches, p => p is AddChild);
    }

    [Fact]
    public void KeyedChildren_ReverseOrder_ShouldMinimizeMoves()
    {
        // Reversing a list: A, B, C, D -> D, C, B, A
        // LIS of [3, 2, 1, 0] is just [3] or any single element (length 1)
        // So 3 moves are needed (all except one element)

        var oldDom = new Element("root", "div", [],
            new Element("a", "div", [], new Text("ta", "A")),
            new Element("b", "div", [], new Text("tb", "B")),
            new Element("c", "div", [], new Text("tc", "C")),
            new Element("d", "div", [], new Text("td", "D")));

        var newDom = new Element("root", "div", [],
            new Element("d", "div", [], new Text("td", "D")),
            new Element("c", "div", [], new Text("tc", "C")),
            new Element("b", "div", [], new Text("tb", "B")),
            new Element("a", "div", [], new Text("ta", "A")));

        var patches = Operations.Diff(oldDom, newDom);
        var moveCount = patches.Count(p => p is MoveChild);

        // Reversing 4 elements: LIS length is 1, so 3 moves needed
        Assert.Equal(3, moveCount);
        Assert.DoesNotContain(patches, p => p is RemoveChild);
        Assert.DoesNotContain(patches, p => p is AddChild);
    }

    #endregion
}
