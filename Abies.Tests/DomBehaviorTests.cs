using Xunit;
using Abies.DOM;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Versioning;
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
    public void DiffWithBatching_ShouldGroupAddChildPatches()
    {
        var oldDom = new Element("root", "ul", [],
            new Element("item-1", "li", [], new Text("t1", "1")),
            new Element("item-2", "li", [], new Text("t2", "2")));

        var newDom = new Element("root", "ul", [],
            new Element("item-1", "li", [], new Text("t1", "1")),
            new Element("item-2", "li", [], new Text("t2", "2")),
            new Element("item-3", "li", [], new Text("t3", "3")),
            new Element("item-4", "li", [], new Text("t4", "4")));

        var patches = Operations.Diff(oldDom, newDom, batchPatches: true);

        var addBatch = Assert.Single(patches.OfType<AddChildrenBatch>());
        Assert.Equal(2, addBatch.Children.Length);
        Assert.Contains(addBatch.Children, c => c.Id == "item-3");
        Assert.Contains(addBatch.Children, c => c.Id == "item-4");
    }

    [Fact]
    public void DiffWithBatching_ShouldGroupRemoveChildPatches()
    {
        var oldDom = new Element("root", "ul", [],
            new Element("item-1", "li", [], new Text("t1", "1")),
            new Element("item-2", "li", [], new Text("t2", "2")),
            new Element("item-3", "li", [], new Text("t3", "3")),
            new Element("item-4", "li", [], new Text("t4", "4")));

        var newDom = new Element("root", "ul", [],
            new Element("item-1", "li", [], new Text("t1", "1")),
            new Element("item-2", "li", [], new Text("t2", "2")));

        var patches = Operations.Diff(oldDom, newDom, batchPatches: true);

        var removeBatch = Assert.Single(patches.OfType<RemoveChildrenBatch>());
        Assert.Equal(2, removeBatch.Children.Length);
        Assert.Contains(removeBatch.Children, c => c.Id == "item-3");
        Assert.Contains(removeBatch.Children, c => c.Id == "item-4");
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
    public void KeyedChildren_Reorder_ShouldReplaceList()
    {
        // Per ADR-016: Element Id is used for keyed diffing, not data-key attribute.
        // When children have different IDs in different order, the algorithm detects reordering.
        var oldDom = new Element("root", "div", [],
            new Element("item-a", "div", new DOMAttribute[]
            {
                new DOMAttribute("ca", "class", "item")
            }, new Text("ta", "A")),
            new Element("item-b", "div", new DOMAttribute[]
            {
                new DOMAttribute("cb", "class", "item")
            }, new Text("tb", "B")));

        var newDom = new Element("root", "div", [],
            new Element("item-b", "div", new DOMAttribute[]
            {
                new DOMAttribute("cb2", "class", "item")
            }, new Text("tb2", "B")),
            new Element("item-a", "div", new DOMAttribute[]
            {
                new DOMAttribute("ca2", "class", "item")
            }, new Text("ta2", "A")));

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(newDom), Render.Html(result));
        // When IDs are reordered, the algorithm removes and re-adds to preserve order
        Assert.Contains(patches, p => p is RemoveChild);
        Assert.Contains(patches, p => p is AddChild);
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
        
        // Keys are reordered, so should remove and add
        Assert.Contains(patches, p => p is RemoveChild);
        Assert.Contains(patches, p => p is AddChild);
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
            AddChildrenBatch acb => UpdateElement(root!, acb.Parent.Id, e => e with { Children = e.Children.Concat(acb.Children).ToArray() }),
            RemoveChild rc => UpdateElement(root!, rc.Parent.Id, e => e with { Children = e.Children.Where(c => c.Id != rc.Child.Id).ToArray() }),
            RemoveChildrenBatch rcb => UpdateElement(root!, rcb.Parent.Id, e =>
            {
                var removeIds = rcb.Children.Select(c => c.Id).ToHashSet();
                return e with { Children = e.Children.Where(c => !removeIds.Contains(c.Id)).ToArray() };
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
                    attrs = attrs.Append(ua.Attribute with { Value = ua.Value }).ToArray();
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
            AddTextsBatch atb => UpdateElement(root!, atb.Parent.Id, e => e with { Children = e.Children.Concat(atb.Children).ToArray() }),
            RemoveTextsBatch rtb => UpdateElement(root!, rtb.Parent.Id, e =>
            {
                var removeIds = rtb.Children.Select(c => c.Id).ToHashSet();
                return e with { Children = e.Children.Where(c => !removeIds.Contains(c.Id)).ToArray() };
            }),
            AddRawBatch arb => UpdateElement(root!, arb.Parent.Id, e => e with { Children = e.Children.Concat(arb.Children).ToArray() }),
            RemoveRawBatch rrb => UpdateElement(root!, rrb.Parent.Id, e =>
            {
                var removeIds = rrb.Children.Select(c => c.Id).ToHashSet();
                return e with { Children = e.Children.Where(c => !removeIds.Contains(c.Id)).ToArray() };
            }),
            _ => root
        };
    }

    private static Node ReplaceNode(Node node, Node target, Node newNode)
    {
        if (ReferenceEquals(node, target) || node.Id == target.Id)
            return newNode;

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
                    attrs[i] = attr with { Id = attrId, Value = oldElement.Id };
                else
                    attrs[i] = attr with { Id = attrId };
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
            return new Text(oldText.Id, newText.Value);

        if (newNode is Element newElem)
        {
            var children = new Node[newElem.Children.Length];
            for (int i = 0; i < newElem.Children.Length; i++)
                children[i] = PreserveIdsForTest(null, newElem.Children[i]);
            return new Element(newElem.Id, newElem.Tag, newElem.Attributes, children);
        }

        return newNode;
    }

    private static Node UpdateElement(Node node, string targetId, System.Func<Element, Element> update)
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
}
