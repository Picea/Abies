// =============================================================================
// HTML Elements
// =============================================================================
// Provides functions for creating virtual DOM elements. Each function returns
// an Element node that can be composed into a virtual DOM tree.
//
// Uses Praefixum source generator for compile-time unique IDs, ensuring
// stable DOM element identification for efficient diffing.
//
// Architecture Decision Records:
// - ADR-003: Virtual DOM
// - ADR-014: Compile-Time Unique IDs
// - ADR-002: Pure Functional Programming
// =============================================================================

using Picea.Abies.DOM;
using Praefixum;
using System.Collections.Concurrent;
using System.Threading;

namespace Picea.Abies.Html;

/// <summary>
/// Provides factory functions for creating HTML elements as virtual DOM nodes.
/// </summary>
/// <remarks>
/// All element functions are pure: they take data and return a Node.
/// Element IDs are generated at compile-time using the Praefixum source generator.
/// </remarks>
public static class Elements
{
    // =========================================================================
    // View Cache — Reference Equality Optimization
    // =========================================================================
    // When lazy() is called with the same compile-time ID and a matching key,
    // we return the EXACT SAME object reference from the previous render. This
    // enables ReferenceEquals bailout at the top of DiffInternal — an O(1) skip
    // that avoids all key comparison, dictionary building, and subtree diffing.
    //
    // This is inspired by Elm's lazy optimization where reference equality (===)
    // enables skipping VDOM construction entirely.
    //
    // IMPORTANT: Cache eviction is required to prevent memory leaks!
    // Call ClearViewCache() when navigating or when the view fundamentally changes.
    // The cache is also automatically trimmed when it exceeds MaxViewCacheSize.
    // =========================================================================

    private static readonly ConcurrentDictionary<string, Node> _lazyCache = new();
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Node>> _scopedLazyCaches = new();
    private static readonly AsyncLocal<string?> _currentViewCacheScope = new();
    private const int MaxViewCacheSize = 2000;

    /// <summary>
    /// Clears the view cache to free memory and prevent stale references.
    /// Call this when navigating to a new page or when the view structure changes significantly.
    /// </summary>
    public static void ClearViewCache()
    {
        _lazyCache.Clear();

        foreach (var scopedCache in _scopedLazyCaches.Values)
        {
            scopedCache.Clear();
        }
    }

    /// <summary>
    /// Gets the current size of the view cache for diagnostics.
    /// </summary>
    public static int ViewCacheCount =>
        _lazyCache.Count + _scopedLazyCaches.Values.Sum(cache => cache.Count);

    internal static IDisposable EnterViewCacheScope(string scopeId)
    {
        var previousScope = _currentViewCacheScope.Value;
        _currentViewCacheScope.Value = scopeId;
        return new ViewCacheScope(previousScope);
    }

    internal static void RemoveViewCacheScope(string scopeId)
    {
        _scopedLazyCaches.TryRemove(scopeId, out _);
    }

    private static ConcurrentDictionary<string, Node> GetActiveLazyCache()
    {
        var scopeId = _currentViewCacheScope.Value;
        return scopeId is null
            ? _lazyCache
            : _scopedLazyCaches.GetOrAdd(scopeId, _ => new ConcurrentDictionary<string, Node>());
    }

    private sealed class ViewCacheScope(string? previousScope) : IDisposable
    {
        public void Dispose() => _currentViewCacheScope.Value = previousScope;
    }

    // =========================================================================
    // Core Element Factory
    // =========================================================================

    /// <summary>
    /// Creates an HTML element node with the given tag, attributes, and children.
    /// If the user provides an explicit <c>id</c> attribute, that value is used
    /// as the element's ID; otherwise the Praefixum-generated ID is used.
    /// </summary>
    public static Element element(string tag, DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
    {
        // Check if user provided an explicit id attribute — if so, use that value
        var explicitId = Array.Find(attributes, a => a.Name == "id");
        var elementId = explicitId?.Value ?? id!;
        // Filter out any user-provided id attributes to avoid duplicates
        var filteredAttributes = explicitId != null
            ? Array.FindAll(attributes, a => a.Name != "id")
            : attributes;
        return new(elementId, tag, [Attributes.id(elementId), .. filteredAttributes], children);
    }

    // =========================================================================
    // Text & Raw HTML
    // =========================================================================

    /// <summary>
    /// Creates a text node with the given string value.
    /// Text nodes are HTML-encoded during rendering.
    /// </summary>
    public static Node text(string value, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new Text(id!, value);

    /// <summary>
    /// Creates a raw HTML node. Content is inserted without encoding.
    /// ⚠️ Use with caution — raw HTML is not sanitized.
    /// </summary>
    public static Node raw(string html, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => new RawHtml(id!, html);

    // =========================================================================
    // Memoization
    // =========================================================================

    /// <summary>
    /// Creates a lazily memoized node. The factory function is only called when
    /// the key changes — when the key matches the previous render, the entire
    /// subtree is skipped during diffing.
    /// </summary>
    /// <typeparam name="TKey">The type of the memoization key.</typeparam>
    /// <param name="key">The key to compare for memoization.</param>
    /// <param name="factory">A function that produces the node content — only called if key differs.</param>
    /// <param name="id">Compile-time unique identifier for the lazy memo node.</param>
    /// <returns>A lazily memoized node that defers evaluation until needed.</returns>
    /// <remarks>
    /// <b>View cache optimization:</b> If the same compile-time ID is called with a
    /// matching key, the exact same object reference is returned. This enables
    /// <c>ReferenceEquals</c> bailout in <c>DiffInternal</c> — an O(1) skip that
    /// avoids all key comparison, dictionary building, and subtree diffing.
    /// Inspired by Elm's <c>lazy</c> where JavaScript <c>===</c> skips VDOM construction.
    /// </remarks>
    public static Node lazy<TKey>(TKey key, Func<Node> factory, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null) where TKey : notnull
    {
        var cache = GetActiveLazyCache();

        // View cache optimization: return same reference if ID and key match.
        // This enables ReferenceEquals bailout in DiffInternal.
        if (id is not null && cache.TryGetValue(id, out var cached) &&
            cached is LazyMemo<TKey> lazyCached &&
            EqualityComparer<TKey>.Default.Equals(lazyCached.Key, key))
        {
            return cached; // Same reference — ReferenceEquals will work!
        }

        var node = new LazyMemo<TKey>(id!, key, factory);
        if (id is not null)
        {
            // Auto-trim cache if it gets too large to prevent memory leaks.
            if (cache.Count >= MaxViewCacheSize)
            {
                cache.Clear();
            }

            cache[id] = node;
        }

        return node;
    }

    /// <summary>
    /// Creates an eagerly memoized node. The node content is provided directly
    /// and the key is compared during diffing to skip unchanged subtrees.
    /// </summary>
    /// <typeparam name="TKey">The type of the memoization key.</typeparam>
    /// <param name="key">The key to compare for memoization.</param>
    /// <param name="node">The pre-computed node content.</param>
    /// <param name="id">Compile-time unique identifier for the memo node.</param>
    public static Node memo<TKey>(TKey key, Node node, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null) where TKey : notnull
    {
        return new Memo<TKey>(id!, key, node);
    }

    // =========================================================================
    // Document Root & Metadata
    // =========================================================================

    public static Node html(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("html", attributes, children, id);

    public static Node head(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("head", attributes, children, id);

    public static Node body(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("body", attributes, children, id);

    public static Node title(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("title", attributes, children, id);

    public static Node meta(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("meta", attributes, [], id);

    public static Node @base(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("base", attributes, [], id);

    public static Node link(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("link", attributes, children, id);

    public static Node style(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("style", attributes, children, id);

    public static Node script(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("script", attributes, children, id);

    public static Node noscript(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("noscript", attributes, children, id);

    // =========================================================================
    // Content Sectioning
    // =========================================================================

    public static Node div(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("div", attributes, children, id);

    public static Node span(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("span", attributes, children, id);

    public static Node p(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("p", attributes, children, id);

    public static Node a(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("a", attributes, children, id);

    public static Node h1(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h1", attributes, children, id);

    public static Node h2(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h2", attributes, children, id);

    public static Node h3(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h3", attributes, children, id);

    public static Node h4(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h4", attributes, children, id);

    public static Node h5(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h5", attributes, children, id);

    public static Node h6(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("h6", attributes, children, id);

    public static Node header(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("header", attributes, children, id);

    public static Node footer(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("footer", attributes, children, id);

    public static Node nav(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("nav", attributes, children, id);

    public static Node main(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("main", attributes, children, id);

    public static Node section(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("section", attributes, children, id);

    public static Node article(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("article", attributes, children, id);

    public static Node aside(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("aside", attributes, children, id);

    public static Node hgroup(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("hgroup", attributes, children, id);

    public static Node address(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("address", attributes, children, id);

    // =========================================================================
    // Text-Level Semantics
    // =========================================================================

    public static Node strong(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("strong", attributes, children, id);

    public static Node em(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("em", attributes, children, id);

    public static Node small(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("small", attributes, children, id);

    public static Node code(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("code", attributes, children, id);

    public static Node pre(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("pre", attributes, children, id);

    public static Node blockquote(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("blockquote", attributes, children, id);

    public static Node b(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("b", attributes, children, id);

    public static Node i(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("i", attributes, children, id);

    public static Node u(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("u", attributes, children, id);

    public static Node s(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("s", attributes, children, id);

    public static Node del(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("del", attributes, children, id);

    public static Node ins(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ins", attributes, children, id);

    public static Node abbr(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("abbr", attributes, children, id);

    public static Node cite(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("cite", attributes, children, id);

    public static Node dfn(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("dfn", attributes, children, id);

    public static Node kbd(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("kbd", attributes, children, id);

    public static Node samp(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("samp", attributes, children, id);

    public static Node sup(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("sup", attributes, children, id);

    public static Node sub(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("sub", attributes, children, id);

    public static Node mark(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("mark", attributes, children, id);

    public static Node q(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("q", attributes, children, id);

    public static Node time(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("time", attributes, children, id);

    public static Node @var(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("var", attributes, children, id);

    public static Node bdi(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("bdi", attributes, children, id);

    public static Node bdo(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("bdo", attributes, children, id);

    public static Node ruby(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ruby", attributes, children, id);

    public static Node rt(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("rt", attributes, children, id);

    public static Node rp(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("rp", attributes, children, id);

    public static Node rb(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("rb", attributes, children, id);

    public static Node rtc(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("rtc", attributes, children, id);

    // =========================================================================
    // Void Elements (no children parameter — HTML Living Standard §13.1.2)
    // =========================================================================

    public static Node br(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("br", attributes, [], id);

    public static Node hr(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("hr", attributes, [], id);

    public static Node img(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("img", attributes, [], id);

    public static Node input(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("input", attributes, [], id);

    public static Node area(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("area", attributes, [], id);

    public static Node col(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("col", attributes, [], id);

    public static Node embed(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("embed", attributes, [], id);

    public static Node param(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("param", attributes, [], id);

    public static Node source(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("source", attributes, [], id);

    public static Node track(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("track", attributes, [], id);

    public static Node wbr(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("wbr", attributes, [], id);

    // =========================================================================
    // Forms
    // =========================================================================

    public static Node form(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("form", attributes, children, id);

    public static Node button(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("button", attributes, children, id);

    public static Node select(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("select", attributes, children, id);

    public static Node option(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("option", attributes, children, id);

    public static Node optgroup(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("optgroup", attributes, children, id);

    public static Node textarea(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("textarea", attributes, children, id);

    public static Node label(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("label", attributes, children, id);

    public static Node fieldset(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("fieldset", attributes, children, id);

    public static Node legend(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("legend", attributes, children, id);

    public static Node datalist(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("datalist", attributes, children, id);

    public static Node output(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("output", attributes, children, id);

    public static Node progress(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("progress", attributes, children, id);

    public static Node meter(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("meter", attributes, children, id);

    // =========================================================================
    // Lists
    // =========================================================================

    public static Node ul(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ul", attributes, children, id);

    public static Node ol(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ol", attributes, children, id);

    public static Node li(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("li", attributes, children, id);

    public static Node dl(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("dl", attributes, children, id);

    public static Node dt(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("dt", attributes, children, id);

    public static Node dd(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("dd", attributes, children, id);

    // =========================================================================
    // Tables
    // =========================================================================

    public static Node table(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("table", attributes, children, id);

    public static Node thead(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("thead", attributes, children, id);

    public static Node tbody(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("tbody", attributes, children, id);

    public static Node tfoot(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("tfoot", attributes, children, id);

    public static Node tr(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("tr", attributes, children, id);

    public static Node td(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("td", attributes, children, id);

    public static Node th(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("th", attributes, children, id);

    public static Node caption(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("caption", attributes, children, id);

    public static Node colgroup(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("colgroup", attributes, children, id);

    // =========================================================================
    // Media & Embedded Content
    // =========================================================================

    public static Node video(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("video", attributes, children, id);

    public static Node audio(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("audio", attributes, children, id);

    public static Node canvas(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("canvas", attributes, children, id);

    public static Node iframe(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("iframe", attributes, children, id);

    public static Node picture(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("picture", attributes, children, id);

    public static Node object_(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("object", attributes, children, id);

    public static Node map(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("map", attributes, children, id);

    public static Node figure(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("figure", attributes, children, id);

    public static Node figcaption(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("figcaption", attributes, children, id);

    // =========================================================================
    // Interactive & Misc
    // =========================================================================

    public static Node details(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("details", attributes, children, id);

    public static Node summary(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("summary", attributes, children, id);

    public static Node dialog(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("dialog", attributes, children, id);

    public static Node template(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("template", attributes, children, id);

    public static Node slot(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("slot", attributes, children, id);

    public static Node portal(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("portal", attributes, children, id);

    public static Node menu(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("menu", attributes, children, id);

    public static Node menuitem(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("menuitem", attributes, children, id);

    public static Node data(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("data", attributes, children, id);

    public static Node math(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("math", attributes, children, id);

    // =========================================================================
    // SVG Elements
    // =========================================================================

    public static Node svg(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("svg", attributes, children, id);

    public static Node g(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("g", attributes, children, id);

    public static Node defs(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("defs", attributes, children, id);

    public static Node symbol(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("symbol", attributes, children, id);

    public static Node use(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("use", attributes, [], id);

    public static Node path(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("path", attributes, [], id);

    public static Node circle(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("circle", attributes, [], id);

    public static Node rect(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("rect", attributes, [], id);

    public static Node ellipse(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("ellipse", attributes, [], id);

    public static Node line(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("line", attributes, [], id);

    public static Node polyline(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("polyline", attributes, [], id);

    public static Node polygon(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("polygon", attributes, [], id);

    public static Node stop(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("stop", attributes, [], id);

    public static Node linearGradient(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("linearGradient", attributes, children, id);

    public static Node radialGradient(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("radialGradient", attributes, children, id);

    public static Node mask(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("mask", attributes, children, id);

    public static Node clipPath(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("clipPath", attributes, children, id);

    public static Node pattern(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("pattern", attributes, children, id);

    public static Node text(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("text", attributes, children, id);

    public static Node tspan(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("tspan", attributes, children, id);

    public static Node desc(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("desc", attributes, children, id);

    public static Node foreignObject(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("foreignObject", attributes, children, id);

    // SVG Filter Elements

    public static Node filter(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("filter", attributes, children, id);

    public static Node feBlend(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feBlend", attributes, [], id);

    public static Node feColorMatrix(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feColorMatrix", attributes, [], id);

    public static Node feComponentTransfer(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feComponentTransfer", attributes, children, id);

    public static Node feComposite(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feComposite", attributes, [], id);

    public static Node feConvolveMatrix(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feConvolveMatrix", attributes, [], id);

    public static Node feDiffuseLighting(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feDiffuseLighting", attributes, children, id);

    public static Node feDisplacementMap(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feDisplacementMap", attributes, [], id);

    public static Node feFlood(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feFlood", attributes, [], id);

    public static Node feGaussianBlur(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feGaussianBlur", attributes, [], id);

    public static Node feImage(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feImage", attributes, [], id);

    public static Node feMerge(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feMerge", attributes, children, id);

    public static Node feMergeNode(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feMergeNode", attributes, [], id);

    public static Node feMorphology(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feMorphology", attributes, [], id);

    public static Node feOffset(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feOffset", attributes, [], id);

    public static Node feSpecularLighting(DOM.Attribute[] attributes, Node[] children, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feSpecularLighting", attributes, children, id);

    public static Node feTile(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feTile", attributes, [], id);

    public static Node feTurbulence(DOM.Attribute[] attributes, [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
        => element("feTurbulence", attributes, [], id);
}
