using Picea.Abies.DOM;

namespace Picea.Abies;

/// <summary>
/// Marker interface for all patch operations.
/// </summary>
public interface Patch;

/// <summary>Set the root element (initial render).</summary>
public readonly struct AddRoot(Element element) : Patch
{
    public readonly Element Element = element;
}

/// <summary>Replace an element with another.</summary>
public readonly struct ReplaceChild(Element oldElement, Element newElement) : Patch
{
    public readonly Element OldElement = oldElement;
    public readonly Element NewElement = newElement;
}

/// <summary>Append a child element to a parent.</summary>
public readonly struct AddChild(Element parent, Element child) : Patch
{
    public readonly Element Parent = parent;
    public readonly Element Child = child;
}

/// <summary>Remove a child element from a parent.</summary>
public readonly struct RemoveChild(Element parent, Element child) : Patch
{
    public readonly Element Parent = parent;
    public readonly Element Child = child;
}

/// <summary>
/// Remove all children from an element.
/// More efficient than multiple RemoveChild operations.
/// </summary>
public readonly struct ClearChildren(Element parent, Node[] oldChildren) : Patch
{
    public readonly Element Parent = parent;
    public readonly Node[] OldChildren = oldChildren;
}

/// <summary>
/// Set all children via a single innerHTML assignment.
/// Dramatically faster than N individual AddChild patches — eliminates
/// N parseHtmlFragment + appendChild calls in the browser.
/// Used for the 0→N children fast path (initial render of a list).
/// </summary>
public readonly struct SetChildrenHtml(Element parent, Node[] children) : Patch
{
    public readonly Element Parent = parent;
    public readonly Node[] Children = children;
}

/// <summary>
/// Append children via a single insertAdjacentHTML('beforeend', html) call.
/// Used for the N→N+M append fast path (e.g., "Add 1000 rows" benchmark).
/// Unlike SetChildrenHtml, this preserves existing children and appends new ones.
/// </summary>
public readonly struct AppendChildrenHtml(Element parent, Node[] children) : Patch
{
    public readonly Element Parent = parent;
    public readonly Node[] Children = children;
}

/// <summary>
/// Move a child element to a new position within its parent.
/// Uses insertBefore semantics: insert before the element with BeforeId, or append if null.
/// </summary>
public readonly struct MoveChild(Element parent, Element child, string? beforeId) : Patch
{
    public readonly Element Parent = parent;
    public readonly Element Child = child;
    public readonly string? BeforeId = beforeId;
}

/// <summary>Update an existing attribute's value.</summary>
public readonly struct UpdateAttribute(Element element, DOM.Attribute attribute, string value) : Patch
{
    public readonly Element Element = element;
    public readonly DOM.Attribute Attribute = attribute;
    public readonly string Value = value;
}

/// <summary>Add a new attribute to an element.</summary>
public readonly struct AddAttribute(Element element, DOM.Attribute attribute) : Patch
{
    public readonly Element Element = element;
    public readonly DOM.Attribute Attribute = attribute;
}

/// <summary>Remove an attribute from an element.</summary>
public readonly struct RemoveAttribute(Element element, DOM.Attribute attribute) : Patch
{
    public readonly Element Element = element;
    public readonly DOM.Attribute Attribute = attribute;
}

/// <summary>Add a new event handler to an element.</summary>
public readonly struct AddHandler(Element element, Handler handler) : Patch
{
    public readonly Element Element = element;
    public readonly Handler Handler = handler;
}

/// <summary>Remove an event handler from an element.</summary>
public readonly struct RemoveHandler(Element element, Handler handler) : Patch
{
    public readonly Element Element = element;
    public readonly Handler Handler = handler;
}

/// <summary>Update an event handler on an element (replace old handler with new one).</summary>
public readonly struct UpdateHandler(Element element, Handler oldHandler, Handler newHandler) : Patch
{
    public readonly Element Element = element;
    public readonly Handler OldHandler = oldHandler;
    public readonly Handler NewHandler = newHandler;
}

/// <summary>Update the text content of a text node.</summary>
public readonly struct UpdateText(Element parent, Text node, string text, string newId) : Patch
{
    public readonly Element Parent = parent;
    public readonly Text Node = node;
    public readonly string Text = text;
    public readonly string NewId = newId;
}

/// <summary>Add a text node to a parent element.</summary>
public readonly struct AddText(Element parent, Text child) : Patch
{
    public readonly Element Parent = parent;
    public readonly Text Child = child;
}

/// <summary>Remove a text node from a parent element.</summary>
public readonly struct RemoveText(Element parent, Text child) : Patch
{
    public readonly Element Parent = parent;
    public readonly Text Child = child;
}

/// <summary>Add raw HTML to a parent element.</summary>
public readonly struct AddRaw(Element parent, RawHtml child) : Patch
{
    public readonly Element Parent = parent;
    public readonly RawHtml Child = child;
}

/// <summary>Remove raw HTML from a parent element.</summary>
public readonly struct RemoveRaw(Element parent, RawHtml child) : Patch
{
    public readonly Element Parent = parent;
    public readonly RawHtml Child = child;
}

/// <summary>Replace one raw HTML node with another.</summary>
public readonly struct ReplaceRaw(RawHtml oldNode, RawHtml newNode) : Patch
{
    public readonly RawHtml OldNode = oldNode;
    public readonly RawHtml NewNode = newNode;
}

/// <summary>Update the content of an existing raw HTML node.</summary>
public readonly struct UpdateRaw(RawHtml node, string html, string newId) : Patch
{
    public readonly RawHtml Node = node;
    public readonly string Html = html;
    public readonly string NewId = newId;
}

/// <summary>Add a new managed element to the document head.</summary>
public readonly struct AddHeadElement(HeadContent content) : Patch
{
    public readonly HeadContent Content = content;
}

/// <summary>Update an existing managed head element whose key matches but content changed.</summary>
public readonly struct UpdateHeadElement(HeadContent content) : Patch
{
    public readonly HeadContent Content = content;
}

/// <summary>Remove a managed head element that is no longer declared.</summary>
public readonly struct RemoveHeadElement(string key) : Patch
{
    public readonly string Key = key;
}
