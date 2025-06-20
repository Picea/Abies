using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;

namespace Abies.DOM
{
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
                if (!string.Equals(oldText.Value, newText.Value, StringComparison.Ordinal))
                    patches.Add(new UpdateText(oldText, newText.Value));
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
            if (oldNode is Element oe && newNode is Element ne && parent != null)
            {
                patches.Add(new ReplaceChild(oe, ne));
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
            }

            // Add additional new children
            for (int i = shared; i < newLength; i++)
            {
                if (newChildren[i] is Element newChild)
                    patches.Add(new AddChild(newParent, newChild));
            }
        }
    }
}
