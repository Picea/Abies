using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;

namespace Abies.DOM
{
    public record Document(string Title, Node Body);
    
    public record Node(string Id);
    public record RawHtml(string Id, string Html) : Node(Id);
    public record Attribute(string Id, string Name, string Value);

    public record Element(string Id, string Tag, Attribute[] Attributes, params Node[] Children) : Node(Id);

    public record Handler(string Name, string CommandId, Message Command, string Id) : Attribute(Id, $"data-event-{Name}", CommandId);

    public record Text(string Id, string Value) : Node(Id)
    {
        public static implicit operator string(Text text) => text.Value;
        public static implicit operator Text(string text) => new(text, text);
    };

    public record Empty() : Node("");

    public interface Patch { }

    public readonly struct AddRoot(Element element) : Patch
    {
        public readonly Element Element = element;
    }

    public readonly struct ReplaceChild(Element oldElement, Element newElement) : Patch
    {
        public readonly Element OldElement = oldElement;
        public readonly Element NewElement = newElement;
    }

    public readonly struct AddChild(Element parent, Element child) : Patch
    {
        public readonly Element Parent = parent;
        public readonly Element Child = child;
    }

    public readonly struct RemoveChild(Element parent, Element child) : Patch
    {
        public readonly Element Parent = parent;
        public readonly Element Child = child;
    }

    public readonly struct UpdateAttribute(Element element, Attribute attribute, string value) : Patch
    {
        public readonly Element Element = element;
        public readonly Attribute Attribute = attribute;
        public readonly string Value = value;
    }

    public readonly struct AddAttribute(Element element, Attribute attribute) : Patch
    {
        public readonly Element Element = element;
        public readonly Attribute Attribute = attribute;
    }

    public readonly struct RemoveAttribute(Element element, Attribute attribute) : Patch
    {
        public readonly Element Element = element;
        public readonly Attribute Attribute = attribute;
    }

    public readonly struct AddHandler(Element element, Handler handler) : Patch
    {
        public readonly Element Element = element;
        public readonly Handler Handler = handler;
    }

    public readonly struct RemoveHandler(Element element, Handler handler) : Patch
    {
        public readonly Element Element = element;
        public readonly Handler Handler = handler;
    }

    public readonly struct UpdateText(Text node, string text, string newId) : Patch
    {
        public readonly Text Node = node;
        public readonly string Text = text;
        public readonly string NewId = newId;
    }

    public readonly struct AddRaw(Element parent, RawHtml child) : Patch
    {
        public readonly Element Parent = parent;
        public readonly RawHtml Child = child;
    }

    public readonly struct RemoveRaw(Element parent, RawHtml child) : Patch
    {
        public readonly Element Parent = parent;
        public readonly RawHtml Child = child;
    }

    public readonly struct ReplaceRaw(RawHtml oldNode, RawHtml newNode) : Patch
    {
        public readonly RawHtml OldNode = oldNode;
        public readonly RawHtml NewNode = newNode;
    }

    public readonly struct UpdateRaw(RawHtml node, string html, string newId) : Patch
    {
        public readonly RawHtml Node = node;
        public readonly string Html = html;
        public readonly string NewId = newId;
    }



    public static class Render
    {
        public static string Html(Node node)
        {
            var sb = new System.Text.StringBuilder();
            RenderNode(node, sb);
            return sb.ToString();
        }

        private static void RenderNode(Node node, System.Text.StringBuilder sb)
        {
            switch (node)
            {
                case Element element:
                    sb.Append($"<{element.Tag} id=\"{element.Id}\"");
                    foreach (var attr in element.Attributes)
                    {
                        if (attr is Handler handler)
                        {
                            sb.Append($" {handler.Name}=\"{handler.Value}\"");
                        }

                        sb.Append($" {attr.Name}=\"{System.Web.HttpUtility.HtmlEncode(attr.Value)}\"");
                    }
                    sb.Append('>');
                    foreach (var child in element.Children)
                    {
                        RenderNode(child, sb);
                    }
                    sb.Append($"</{element.Tag}>");
                    break;
                case Text text:
                    sb.Append($"<span id=\"{text.Id}\">{System.Web.HttpUtility.HtmlEncode(text.Value)}</span>");
                    break;
                case RawHtml raw:
                    sb.Append($"<span id=\"{raw.Id}\">{raw.Html}</span>");
                    break;
                // Handle other node types if necessary
                default:
                    break;
            }
        }
    }
    
    /// <summary>
    /// Provides diffing and patching utilities for the virtual DOM.
    /// The implementation is inspired by Elm's VirtualDom diff algorithm
    /// and is written with performance in mind.
    /// </summary>
    public static class Operations
    {
        // Object pools to reduce allocations
        private static readonly ConcurrentQueue<List<Patch>> _patchListPool = new();
        private static readonly ConcurrentQueue<Dictionary<string, Attribute>> _attributeMapPool = new();

        private static List<Patch> RentPatchList()
        {
            if (_patchListPool.TryDequeue(out var list))
            {
                list.Clear();
                return list;
            }
            return new List<Patch>();
        }

        private static void ReturnPatchList(List<Patch> list)
        {
            if (list.Count < 1000) // Prevent memory bloat
                _patchListPool.Enqueue(list);
        }

        private static Dictionary<string, Attribute> RentAttributeMap()
        {
            if (_attributeMapPool.TryDequeue(out var map))
            {
                map.Clear();
                return map;
            }
            return new Dictionary<string, Attribute>();
        }

        private static void ReturnAttributeMap(Dictionary<string, Attribute> map)
        {
            if (map.Count < 100) // Prevent memory bloat
                _attributeMapPool.Enqueue(map);
        }

        /// <summary>
        /// Apply a patch to the real DOM by invoking JavaScript interop.
        /// </summary>
        public static async Task Apply(Patch patch)
        {
            switch (patch)
            {
                case AddRoot addRoot:
                    await Interop.SetAppContent(Render.Html(addRoot.Element));
                    break;
                case ReplaceChild replaceChild:
                    await Interop.ReplaceChildHtml(replaceChild.OldElement.Id, Render.Html(replaceChild.NewElement));
                    break;
                case AddChild addChild:
                    await Interop.AddChildHtml(addChild.Parent.Id, Render.Html(addChild.Child));
                    break;
                case RemoveChild removeChild:
                    await Interop.RemoveChild(removeChild.Parent.Id, removeChild.Child.Id);
                    break;
                case UpdateAttribute updateAttribute:
                    await Interop.UpdateAttribute(updateAttribute.Element.Id, updateAttribute.Attribute.Name, updateAttribute.Value);
                    break;
                case AddAttribute addAttribute:
                    await Interop.AddAttribute(addAttribute.Element.Id, addAttribute.Attribute.Name, addAttribute.Attribute.Value);
                    break;
                case RemoveAttribute removeAttribute:
                    await Interop.RemoveAttribute(removeAttribute.Element.Id, removeAttribute.Attribute.Name);
                    break;
                case AddHandler addHandler:
                    await Interop.AddAttribute(addHandler.Element.Id, addHandler.Handler.Name, addHandler.Handler.Value);
                    break;
                case RemoveHandler removeHandler:
                    await Interop.RemoveAttribute(removeHandler.Element.Id, removeHandler.Handler.Name);
                    break;
                case UpdateText updateText:
                    await Interop.UpdateTextContent(updateText.Node.Id, updateText.Text);
                    break;
                case AddRaw addRaw:
                    await Interop.AddChildHtml(addRaw.Parent.Id, Render.Html(addRaw.Child));
                    break;
                case RemoveRaw removeRaw:
                    await Interop.RemoveChild(removeRaw.Parent.Id, removeRaw.Child.Id);
                    break;
                case ReplaceRaw replaceRaw:
                    await Interop.ReplaceChildHtml(replaceRaw.OldNode.Id, Render.Html(replaceRaw.NewNode));
                    break;
                case UpdateRaw updateRaw:
                    await Interop.ReplaceChildHtml(updateRaw.Node.Id, Render.Html(new RawHtml(updateRaw.NewId, updateRaw.Html)));
                    break;
                default:
                    throw new InvalidOperationException("Unknown patch type");
            }
        }        /// <summary>
        /// Compute the list of patches that transform <paramref name="oldNode"/> into <paramref name="newNode"/>.
        /// </summary>
        /// <param name="oldNode">The previous virtual DOM node. Can be <c>null</c> when rendering for the first time.</param>
        /// <param name="newNode">The new virtual DOM node.</param>
        public static List<Patch> Diff(Node? oldNode, Node newNode)
        {
            var patches = RentPatchList();
            try
            {
                if (oldNode is null)
                {
                    patches.Add(new AddRoot((Element)newNode));
                    var result = new List<Patch>(patches);
                    return result;
                }

                DiffInternal(oldNode, newNode, null, patches);
                var finalResult = new List<Patch>(patches);
                return finalResult;
            }
            finally
            {
                ReturnPatchList(patches);
            }
        }        private static void DiffInternal(Node oldNode, Node newNode, Element? parent, List<Patch> patches)
        {
            // Text nodes only need an update when the value changes
            if (oldNode is Text oldText && newNode is Text newText)
            {
                if (!string.Equals(oldText.Value, newText.Value, StringComparison.Ordinal) || !string.Equals(oldText.Id, newText.Id, StringComparison.Ordinal))
                    patches.Add(new UpdateText(oldText, newText.Value, newText.Id));
                return;
            }

            if (oldNode is RawHtml oldRaw && newNode is RawHtml newRaw)
            {
                if (!string.Equals(oldRaw.Html, newRaw.Html, StringComparison.Ordinal) || !string.Equals(oldRaw.Id, newRaw.Id, StringComparison.Ordinal))
                    patches.Add(new UpdateRaw(oldRaw, newRaw.Html, newRaw.Id));
                return;
            }

            // Elements may need to be replaced when the tag differs or the node type changed
            if (oldNode is Element oldElement && newNode is Element newElement)
            {
                // Early exit for reference equality only for elements with same tag
                if (ReferenceEquals(oldElement, newElement))
                    return;

                if (!string.Equals(oldElement.Tag, newElement.Tag, StringComparison.Ordinal))
                {
                    if (parent == null)
                        patches.Add(new AddRoot(newElement));
                    else
                        patches.Add(new ReplaceChild(oldElement, newElement));
                    return;
                }

                DiffAttributes(oldElement, newElement, patches);
                DiffChildren(oldElement, newElement, patches);
                return;
            }

            // Fallback for node type mismatch
            if (parent != null)
            {
                if (oldNode is Element oe && newNode is Element ne)
                {
                    patches.Add(new ReplaceChild(oe, ne));
                }
                else if (oldNode is RawHtml oldRaw && newNode is RawHtml newRaw)
                {
                    patches.Add(new ReplaceRaw(oldRaw, newRaw));
                }
                else if (oldNode is RawHtml r && newNode is Element ne2)
                {
                    patches.Add(new ReplaceRaw(r, new RawHtml(ne2.Id, Render.Html(ne2))));
                }
                else if (oldNode is Element oe2 && newNode is RawHtml r2)
                {
                    patches.Add(new ReplaceRaw(new RawHtml(oe2.Id, Render.Html(oe2)), r2));
                }
            }
        }// Diff attribute collections using dictionaries for O(n) lookup
        private static void DiffAttributes(Element oldElement, Element newElement, List<Patch> patches)
        {
            var oldAttrs = oldElement.Attributes;
            var newAttrs = newElement.Attributes;

            // Early exit for identical attribute arrays
            if (ReferenceEquals(oldAttrs, newAttrs))
                return;

            // Early exit for both empty
            if (oldAttrs.Length == 0 && newAttrs.Length == 0)
                return;

            // If old is empty, just add all new attributes
            if (oldAttrs.Length == 0)
            {
                foreach (var newAttr in newAttrs)
                {
                    if (newAttr is Handler handler)
                        patches.Add(new AddHandler(newElement, handler));
                    else
                        patches.Add(new AddAttribute(newElement, newAttr));
                }
                return;
            }

            // If new is empty, remove all old attributes
            if (newAttrs.Length == 0)
            {
                foreach (var oldAttr in oldAttrs)
                {
                    if (oldAttr is Handler handler)
                        patches.Add(new RemoveHandler(oldElement, handler));
                    else
                        patches.Add(new RemoveAttribute(oldElement, oldAttr));
                }
                return;
            }

            var oldMap = RentAttributeMap();
            try
            {
                // Use initial capacity hint
                if (oldMap.Count == 0 && oldAttrs.Length > 0)
                    oldMap.EnsureCapacity(oldAttrs.Length);

                foreach (var attr in oldAttrs)
                    oldMap[attr.Id] = attr;

                foreach (var newAttr in newAttrs)
                {
                    if (oldMap.TryGetValue(newAttr.Id, out var oldAttr))
                    {
                        oldMap.Remove(newAttr.Id);
                        if (!newAttr.Equals(oldAttr))
                        {
                            if (oldAttr is Handler oldHandler)
                                patches.Add(new RemoveHandler(oldElement, oldHandler));
                            else if (newAttr is Handler)
                                patches.Add(new RemoveAttribute(oldElement, oldAttr));

                            if (newAttr is Handler newHandler)
                                patches.Add(new AddHandler(newElement, newHandler));
                            else
                                patches.Add(new UpdateAttribute(oldElement, newAttr, newAttr.Value));
                        }
                    }
                    else
                    {
                        if (newAttr is Handler handler)
                            patches.Add(new AddHandler(newElement, handler));
                        else
                            patches.Add(new AddAttribute(newElement, newAttr));
                    }
                }

                // Any remaining old attributes must be removed
                foreach (var remaining in oldMap.Values)
                {
                    if (remaining is Handler handler)
                        patches.Add(new RemoveHandler(oldElement, handler));
                    else
                        patches.Add(new RemoveAttribute(oldElement, remaining));
                }
            }
            finally
            {
                ReturnAttributeMap(oldMap);
            }
        }        private static void DiffChildren(Element oldParent, Element newParent, List<Patch> patches)
        {
            var oldChildren = oldParent.Children;
            var newChildren = newParent.Children;

            // Early exit for identical child arrays
            if (ReferenceEquals(oldChildren, newChildren))
                return;

            var oldLength = oldChildren.Length;
            var newLength = newChildren.Length;
            var shared = Math.Min(oldLength, newLength);

            // Diff children that exist in both trees
            for (int i = 0; i < shared; i++)
                DiffInternal(oldChildren[i], newChildren[i], oldParent, patches);

            // Remove extra old children (iterate backwards to maintain DOM order)
            for (int i = oldLength - 1; i >= shared; i--)
            {
                if (oldChildren[i] is Element oldChild)
                    patches.Add(new RemoveChild(oldParent, oldChild));
                else if (oldChildren[i] is RawHtml oldRaw)
                    patches.Add(new RemoveRaw(oldParent, oldRaw));
            }

            // Add additional new children
            for (int i = shared; i < newLength; i++)
            {
                if (newChildren[i] is Element newChild)
                    patches.Add(new AddChild(newParent, newChild));
                else if (newChildren[i] is RawHtml newRaw)
                    patches.Add(new AddRaw(newParent, newRaw));
            }
        }
    }
}
