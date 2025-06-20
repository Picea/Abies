using Xunit;
using Abies.DOM;
using System.Linq;
using System.Collections.Generic;
using DOMAttribute = Abies.DOM.Attribute;

namespace Abies.Tests;

public class DomBehaviorTests
{
    private record DummyMessage() : Message;

    [Fact]
    public void AddRoot_ShouldRenderCorrectly()
    {
        var newDom = new Element("1", "div", System.Array.Empty<DOMAttribute>(),
            new Text("2", "Hello"));

        var patches = Operations.Diff(null, newDom);
        var result = ApplyPatches(null, patches, null);

        Assert.Equal(Render.Html(newDom), Render.Html(result!));
    }

    [Fact]
    public void ReplaceChild_ShouldUpdateTree()
    {
        var oldDom = new Element("1", "div", System.Array.Empty<DOMAttribute>(),
            new Element("2", "span", System.Array.Empty<DOMAttribute>(), new Text("3", "Old")));

        var newDom = new Element("1", "div", System.Array.Empty<DOMAttribute>(),
            new Element("4", "p", System.Array.Empty<DOMAttribute>(), new Text("5", "New")));

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.Equal(Render.Html(newDom), Render.Html(result));
    }

    [Fact]
    public void AttributeChanges_ShouldReflectInResult()
    {
        var oldDom = new Element("1", "button",
            new DOMAttribute[] { new DOMAttribute("a1", "class", "btn") },
            System.Array.Empty<Node>());

        var newDom = new Element("1", "button",
            new DOMAttribute[]
            {
                new DOMAttribute("a1", "class", "btn-primary"),
                new Handler("click", "cmd1", new DummyMessage(), "h1", null)
            },
            System.Array.Empty<Node>());

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.Equal(Render.Html(newDom), Render.Html(result));
    }

    [Fact]
    public void AttributeIdChange_ShouldNotRemoveAttribute()
    {
        var oldDom = new Element("1", "div",
            new DOMAttribute[] { new DOMAttribute("a1", "class", "foo") },
            System.Array.Empty<Node>());

        var newDom = new Element("1", "div",
            new DOMAttribute[] { new DOMAttribute("a2", "class", "foo") },
            System.Array.Empty<Node>());

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.Equal(Render.Html(newDom), Render.Html(result));
    }

    [Fact]
    public void Render_ShouldIncludeElementIds()
    {
        var dom = new Element("el1", "div", System.Array.Empty<DOMAttribute>(),
            new Element("child", "span", System.Array.Empty<DOMAttribute>(), new Text("t", "hi")));

        var html = Render.Html(dom);

        Assert.Contains("id=\"el1\"", html);
        Assert.Contains("id=\"child\"", html);
    }

    [Fact]
    public void TextUpdate_ShouldUpdateTextContent()
    {
        var oldDom = new Element("1", "h1", System.Array.Empty<DOMAttribute>(),
            new Text("2", "Sign up"));

        var newDom = new Element("1", "h1", System.Array.Empty<DOMAttribute>(),
            new Text("3", "Sign in"));

        var patches = Operations.Diff(oldDom, newDom);
        var result = ApplyPatches(oldDom, patches, oldDom);

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
        var oldDom = new Element("1", "h1", System.Array.Empty<DOMAttribute>(),
            new Text("2", "Sign up"));

        var newDomBeforePreservation = new Element("1", "h1", System.Array.Empty<DOMAttribute>(),
            new Text("3", "Sign in"));

        // Simulate what PreserveIds does - preserve the old text ID but use new text content
        var newDomAfterPreservation = new Element("1", "h1", System.Array.Empty<DOMAttribute>(),
            new Text("2", "Sign in")); // Same ID as old, new content

        var patches = Operations.Diff(oldDom, newDomAfterPreservation);
        var result = ApplyPatches(oldDom, patches, oldDom);

        Assert.Equal(Render.Html(newDomAfterPreservation), Render.Html(result));

        // Verify that an UpdateText patch was generated
        Assert.Contains(patches, p => p is UpdateText);
        var textPatch = patches.OfType<UpdateText>().First();
        Assert.Equal("Sign in", textPatch.Text);
        Assert.Equal("2", textPatch.Node.Id); // Should use the preserved ID
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
            UpdateAttribute ua => UpdateElement(root!, ua.Element.Id, e => e with { Attributes = e.Attributes.Select(a => a.Id == ua.Attribute.Id ? ua.Attribute with { Value = ua.Value } : a).ToArray() }),
            AddAttribute aa => UpdateElement(root!, aa.Element.Id, e => e with { Attributes = e.Attributes.Append(aa.Attribute).ToArray() }),
            RemoveAttribute ra => UpdateElement(root!, ra.Element.Id, e => e with { Attributes = e.Attributes.Where(a => a.Id != ra.Attribute.Id).ToArray() }),
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

