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
        var oldDom = new Element("root", "div", [],
            new Element("a", "div", new DOMAttribute[]
            {
                new DOMAttribute("ka", "data-key", "a"),
                new DOMAttribute("ca", "class", "item")
            }, new Text("ta", "A")),
            new Element("b", "div", new DOMAttribute[]
            {
                new DOMAttribute("kb", "data-key", "b"),
                new DOMAttribute("cb", "class", "item")
            }, new Text("tb", "B")));

        var newDom = new Element("root", "div", [],
            new Element("b2", "div", new DOMAttribute[]
            {
                new DOMAttribute("kb2", "data-key", "b"),
                new DOMAttribute("cb2", "class", "item")
            }, new Text("tb2", "B")),
            new Element("a2", "div", new DOMAttribute[]
            {
                new DOMAttribute("ka2", "data-key", "a"),
                new DOMAttribute("ca2", "class", "item")
            }, new Text("ta2", "A")));

        var alignedNew = PreserveIdsForTest(oldDom, newDom);
        var patches = Operations.Diff(oldDom, alignedNew);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(alignedNew), Render.Html(result));
        Assert.Contains(patches, p => p is RemoveChild);
        Assert.Contains(patches, p => p is AddChild);
    }

    [Fact]
    public void KeyedChildren_SameOrder_ShouldDiffInPlace()
    {
        var oldDom = new Element("root", "div", [],
            new Element("a", "div", new DOMAttribute[]
            {
                new DOMAttribute("ka", "data-key", "a"),
                new DOMAttribute("ca", "class", "item")
            }, new Text("ta", "Old")),
            new Element("b", "div", new DOMAttribute[]
            {
                new DOMAttribute("kb", "data-key", "b"),
                new DOMAttribute("cb", "class", "item")
            }, new Text("tb", "Old")));

        var newDom = new Element("root", "div", [],
            new Element("a2", "div", new DOMAttribute[]
            {
                new DOMAttribute("ka2", "data-key", "a"),
                new DOMAttribute("ca2", "class", "item")
            }, new Text("ta2", "New")),
            new Element("b2", "div", new DOMAttribute[]
            {
                new DOMAttribute("kb2", "data-key", "b"),
                new DOMAttribute("cb2", "class", "item")
            }, new Text("tb2", "New")));

        var alignedNew = PreserveIdsForTest(oldDom, newDom);
        var patches = Operations.Diff(oldDom, alignedNew);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.NotNull(result);
        Assert.Equal(Render.Html(alignedNew), Render.Html(result));
        Assert.DoesNotContain(patches, p => p is RemoveChild);
        Assert.DoesNotContain(patches, p => p is AddChild);
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
