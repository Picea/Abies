# Head Management Guide

Manage `<head>` elements — meta tags, Open Graph, Twitter Cards, canonical URLs, stylesheets, and structured data — declaratively from your View function.

## Overview

In Abies, `<head>` content is part of the `Document` returned by your `View` function, just like the page body. The runtime diffs head content between renders and applies only the changes — stale elements are automatically removed on navigation.

```csharp
using static Abies.Head;

public static Document View(Model model)
    => new Document(
        "My App - Home",
        body,
        meta("description", "Welcome to my app"),
        og("title", "My App"),
        canonical("/")
    );
```

## Use Cases

### 1. SEO Meta Tags

Every page should have a unique `description` meta tag for search engine results. You can also control indexing behavior with `robots`.

```csharp
// Public page — indexable
Page.Home => new Document("Home", body,
    meta("description", "A social blogging platform built with Abies."),
    meta("keywords", "blog, articles, social")),

// Private page — hidden from crawlers
Page.Settings => new Document("Settings", body,
    meta("robots", "noindex")),

// Login/Register — specific descriptions
Page.Login => new Document("Sign In", body,
    meta("description", "Sign in to your account")),
```

### 2. Open Graph Tags (Facebook, LinkedIn, Slack)

Open Graph tags control how your pages appear when shared on social media. They update automatically as the user navigates between pages.

```csharp
Page.Home => new Document("Home", body,
    og("title", "Conduit"),
    og("type", "website"),
    og("description", "A place to share your knowledge."),
    og("image", "https://example.com/og-home.png"),
    og("url", "https://example.com/")),

Page.Article article => new Document("Article", body,
    og("title", article.Title),
    og("type", "article"),
    og("description", article.Description),
    og("image", article.CoverImage),
    og("url", $"https://example.com/article/{article.Slug}")),
```

**How diffing works:** When navigating from Home → Article, the runtime updates `og:title`, `og:description`, and `og:url` in place, adds `og:image` if it wasn't there before, and changes `og:type` from `"website"` to `"article"`.

### 3. Twitter Cards

Twitter Cards use their own meta tags (via the `name` attribute). Use `twitter()` to prefix automatically.

```csharp
new Document("Article", body,
    twitter("card", "summary_large_image"),
    twitter("title", article.Title),
    twitter("description", article.Description),
    twitter("image", article.CoverImage)),
```

### 4. Canonical URLs

Canonical URLs tell search engines which version of a page is the "primary" one. This is critical for avoiding duplicate content issues in SPAs.

```csharp
Page.Home => new Document("Home", body,
    canonical("/")),

Page.Article article => new Document("Article", body,
    canonical($"/article/{article.Slug}")),

// Paginated content — canonical points to page 1
Page.Home home => new Document("Home", body,
    canonical(home.CurrentPage == 1 ? "/" : $"/?page={home.CurrentPage}")),
```

**Key behavior:** Since the canonical URL's key includes the href (`link:canonical:/article/cats`), navigating to a different article **removes** the old canonical and **adds** the new one — they're different elements, not updates.

### 5. Structured Data (JSON-LD)

Structured data helps search engines understand your content. Use `jsonLd()` to embed Schema.org markup.

```csharp
// Article page
Page.Article article => new Document("Article", body,
    jsonLd(new
    {
        @context = "https://schema.org",
        @type = "Article",
        headline = article.Title,
        description = article.Description,
        datePublished = article.CreatedAt,
        author = new
        {
            @type = "Person",
            name = article.Author.Username
        }
    })),

// Profile page
Page.Profile profile => new Document("Profile", body,
    jsonLd(new
    {
        @context = "https://schema.org",
        @type = "Person",
        name = profile.Username,
        description = profile.Bio,
        image = profile.Image
    })),

// Home page
Page.Home => new Document("Home", body,
    jsonLd(new
    {
        @context = "https://schema.org",
        @type = "WebSite",
        name = "Conduit",
        url = "https://example.com"
    })),
```

**Diffing behavior:** All JSON-LD scripts share the key `script:application/ld+json`, so navigating between pages **updates** the existing script element rather than adding multiple ones.

### 6. Dynamic Stylesheets (Theming)

Swap stylesheets based on model state — useful for dark/light mode or per-page styles.

```csharp
public static Document View(Model model)
{
    var themeSheet = model.Theme switch
    {
        Theme.Light => stylesheet("/css/theme-light.css"),
        Theme.Dark => stylesheet("/css/theme-dark.css"),
        _ => stylesheet("/css/theme-light.css")
    };

    return new Document("My App", body,
        stylesheet("/css/base.css"),
        themeSheet);
}
```

**How diffing works:** When switching from light to dark, the runtime **removes** `link:stylesheet:/css/theme-light.css` and **adds** `link:stylesheet:/css/theme-dark.css`. The base stylesheet is unchanged and untouched.

### 7. Resource Preloading

Preload critical resources for pages that need them.

```csharp
Page.Article => new Document("Article", body,
    preload("/fonts/serif.woff2", "font"),
    preload("/images/article-header.jpg", "image")),

Page.Home => new Document("Home", body,
    preload("/fonts/sans.woff2", "font")),
```

### 8. Article Metadata with Open Graph Protocol Properties

For content-heavy pages, use the `property()` helper to add protocol-specific properties like `article:published_time` and `article:author`.

```csharp
Page.Article article => new Document("Article", body,
    meta("description", article.Description),
    og("title", article.Title),
    og("type", "article"),
    property("article:published_time", article.CreatedAt),
    property("article:author", article.Author.Username),
    property("article:section", article.Category),
    property("article:tag", article.PrimaryTag)),
```

### 9. Favicons and App Icons

```csharp
new Document("My App", body,
    link("icon", "/favicon.ico", "image/x-icon"),
    link("icon", "/icon-192.png", "image/png"),
    link("apple-touch-icon", "/apple-touch-icon.png")),
```

### 10. Conditional Head Content Based on Data Loading

A common pattern: show minimal head content while data is loading, then enrich it once data arrives.

```csharp
private static Document ViewArticle(Page.Article article, Model model)
{
    var body = WithLayout(ArticlePage(article.Model), model);

    // Loading state — minimal head
    if (article.Model.Article is not { } art)
        return new Document("Loading...", body,
            meta("description", "Loading article..."));

    // Loaded — full SEO head
    return new Document($"Conduit - {art.Title}", body,
        meta("description", art.Description),
        og("title", art.Title),
        og("type", "article"),
        og("description", art.Description),
        canonical($"/article/{art.Slug}"),
        jsonLd(new
        {
            @context = "https://schema.org",
            @type = "Article",
            headline = art.Title,
            description = art.Description,
            datePublished = art.CreatedAt
        }));
}
```

**How it works:** The first render emits a single `meta:description` tag. When the article loads (Update → View cycle), the runtime diffs the old single-element head against the new 6-element head: it **updates** the description and **adds** the 5 new elements.

## API Reference

### Document Record

```csharp
// The third parameter is params — backward compatible
public record Document(string Title, Node Body, params HeadContent[] Head);

// These are equivalent:
new Document("Title", body)                    // Empty head
new Document("Title", body, meta("x", "y"))    // Single element
new Document("Title", body, [..headArray])      // From array
```

### HeadContent Types

| Type | Factory | Key Pattern | HTML Output |
| ---- | ------- | ----------- | ----------- |
| `Meta` | `meta(name, content)` | `meta:{name}` | `<meta name="..." content="...">` |
| `MetaProperty` | `og(prop, content)`, `property(prop, content)` | `property:{property}` | `<meta property="..." content="...">` |
| `Link` | `canonical(href)`, `stylesheet(href)`, `link(rel, href)` | `link:{rel}:{href}` | `<link rel="..." href="...">` |
| `Script` | `jsonLd(data)` | `script:{type}` | `<script type="...">...</script>` |
| `Base` | `@base(href)` | `base` | `<base href="...">` |

### Head Helper Functions

| Function | Description |
| -------- | ----------- |
| `meta(name, content)` | Standard meta tag |
| `og(property, content)` | Open Graph tag (auto-prefixes `og:`) |
| `twitter(name, content)` | Twitter Card tag (auto-prefixes `twitter:`) |
| `canonical(href)` | Canonical URL |
| `stylesheet(href)` | CSS stylesheet link |
| `preload(href, asType)` | Resource preload hint |
| `jsonLd(object)` | JSON-LD structured data (auto-serializes) |
| `link(rel, href, type?)` | Custom link tag |
| `property(prop, content)` | Custom property meta tag |
| `@base(href)` | Base URL |

## How Diffing Works

The runtime tracks head content between renders and computes the minimal set of DOM operations:

| Scenario | Old Head | New Head | Patch |
| -------- | -------- | -------- | ----- |
| **Same key, same content** | `meta:description = "Hello"` | `meta:description = "Hello"` | *No-op* |
| **Same key, different content** | `meta:description = "Hello"` | `meta:description = "World"` | **Update** |
| **Key only in new** | — | `og:title = "Home"` | **Add** |
| **Key only in old** | `og:title = "Home"` | — | **Remove** |

Managed elements are tagged with `data-abies-head` in the real DOM, so Abies never touches user-defined `<head>` elements from `index.html`.

## Best Practices

1. **Always include `meta("description", ...)` on every page** — it's the most important SEO tag and appears in search results.

2. **Use `meta("robots", "noindex")` on private pages** — settings, login, register, and any page that shouldn't be indexed.

3. **Add Open Graph tags for shareable pages** — articles, profiles, and the home page benefit most from social sharing metadata.

4. **Keep JSON-LD structured data accurate** — search engines use it for rich results. Only include data you actually have.

5. **Handle loading states** — return minimal head content while data is loading, then enrich it once data arrives. The runtime diffs efficiently.

6. **Import `using static Abies.Head;`** — the lowercase factory functions (`meta`, `og`, `canonical`) read like DSL when used with static imports.
