using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Abies.DOM
{
    /// <summary>
    /// Provides diffing and patching utilities for the virtual DOM.
    /// The implementation is inspired by Elm's VirtualDom diff algorithm
    /// and is written with performance in mind.
    /// </summary>
    public static class Operations
    {
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
        }

        /// <summary>
        /// Compute the list of patches that transform <paramref name="oldNode"/> into <paramref name="newNode"/>.
        /// </summary>
        /// <param name="oldNode">The previous virtual DOM node. Can be <c>null</c> when rendering for the first time.</param>
        /// <param name="newNode">The new virtual DOM node.</param>
        public static List<Patch> Diff(Node? oldNode, Node newNode)
        {
            var patches = new List<Patch>();
            if (oldNode is null)
            {
                patches.Add(new AddRoot((Element)newNode));
                return patches;
            }

            DiffInternal(oldNode, newNode, null, patches);
            return patches;
        }

        private static void DiffInternal(Node oldNode, Node newNode, Element? parent, List<Patch> patches)
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
        }

        // Diff attribute collections using dictionaries for O(n) lookup
        private static void DiffAttributes(Element oldElement, Element newElement, List<Patch> patches)
        {
            var oldMap = new Dictionary<string, Attribute>(oldElement.Attributes.Length);
            foreach (var attr in oldElement.Attributes)
                oldMap[attr.Id] = attr;

            foreach (var newAttr in newElement.Attributes)
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

        private static void DiffChildren(Element oldParent, Element newParent, List<Patch> patches)
        {
            var oldChildren = oldParent.Children;
            var newChildren = newParent.Children;
            int shared = Math.Min(oldChildren.Length, newChildren.Length);

            // Diff children that exist in both trees
            for (int i = 0; i < shared; i++)
                DiffInternal(oldChildren[i], newChildren[i], oldParent, patches);

            // Remove extra old children
            for (int i = shared; i < oldChildren.Length; i++)
            {
                if (oldChildren[i] is Element oldChild)
                    patches.Add(new RemoveChild(oldParent, oldChild));
            }

            // Add additional new children
            for (int i = shared; i < newChildren.Length; i++)
            {
                if (newChildren[i] is Element newChild)
                    patches.Add(new AddChild(newParent, newChild));
            }
        }
    }
}
