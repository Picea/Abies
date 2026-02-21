namespace Abies.Analyzers.Tests;

/// <summary>
/// Minimal source-code stubs that mirror the public API surface of Abies.DOM,
/// Abies.Html.Elements, and Abies.Html.Attributes. These are compiled into the
/// Roslyn test harness's in-memory compilation so the semantic model can resolve
/// types without referencing the real Abies assembly (which targets net10.0 and
/// causes CS1705 version mismatches with the net8.0 reference assemblies used
/// by the test framework).
/// </summary>
internal static class AbiesStubs
{
    /// <summary>
    /// Source code containing minimal Abies type definitions.
    /// Add this to every analyzer test via <c>TestState.Sources.Add</c>.
    /// </summary>
    public const string Source = """
        // ── Abies.DOM stubs ──────────────────────────────────────────
        namespace Abies.DOM
        {
            public record Node(string Id);
            public record Element(string Id, string Tag, Attribute[] Attributes, params Node[] Children) : Node(Id);
            public record Text(string Id, string Value) : Node(Id);
            public record Attribute(string Id, string Name, string Value);
        }
        
        // ── Abies.Html.Elements stubs ────────────────────────────────
        namespace Abies.Html
        {
            using Abies.DOM;
        
            public static class Elements
            {
                // Non-void elements: (attributes, children, id?) → Node
                public static Node a(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "a", attributes, children);
        
                public static Node div(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "div", attributes, children);
        
                public static Node span(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "span", attributes, children);
        
                public static Node p(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "p", attributes, children);
        
                public static Node button(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "button", attributes, children);
        
                public static Node h1(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "h1", attributes, children);
        
                public static Node strong(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "strong", attributes, children);
        
                public static Node em(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "em", attributes, children);
        
                public static Node section(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "section", attributes, children);
        
                public static Node table(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "table", attributes, children);
        
                public static Node tr(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "tr", attributes, children);
        
                public static Node td(Attribute[] attributes, Node[] children, string? id = null)
                    => new Element(id ?? "", "td", attributes, children);
        
                // Void elements: (attributes, id?) → Node  (no children parameter)
                public static Node img(Attribute[] attributes, string? id = null)
                    => new Element(id ?? "", "img", attributes);
        
                public static Node input(Attribute[] attributes, string? id = null)
                    => new Element(id ?? "", "input", attributes);
        
                // Text factory: (value, id?) → Node
                public static Node text(string value, string? id = null)
                    => new Text(id ?? "", value);
            }
        }
        
        // ── Abies.Html.Attributes stubs ──────────────────────────────
        namespace Abies.Html
        {
            public static class Attributes
            {
                public static DOM.Attribute src(string value, string? id = null)
                    => new(id ?? "", "src", value);
        
                public static DOM.Attribute alt(string value, string? id = null)
                    => new(id ?? "", "alt", value);
        
                public static DOM.Attribute href(string value, string? id = null)
                    => new(id ?? "", "href", value);
        
                public static DOM.Attribute class_(string value, string? id = null)
                    => new(id ?? "", "class", value);
        
                public static DOM.Attribute type(string value, string? id = null)
                    => new(id ?? "", "type", value);
        
                public static DOM.Attribute placeholder(string value, string? id = null)
                    => new(id ?? "", "placeholder", value);
            }
        }
        """;
}
