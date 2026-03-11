# HTML Elements API Reference

The `Picea.Abies.Html.Elements` static class provides functions for creating virtual DOM elements.

## Usage

```csharp
using Picea.Abies.Html;
using static Picea.Abies.Html.Elements;
```

## Element Function Signatures

Element functions come in two forms depending on whether the HTML element is a *normal* element (can contain children) or a *void* element (self-closing, no children).

### Normal Elements

Normal elements accept attributes and children:

```csharp
public static Node div(
    DOM.Attribute[] attributes,
    Node[] children,
    [UniqueId] string? id = null)
```

### Void Elements

Void elements accept only attributes — they cannot contain children:

```csharp
public static Node img(
    DOM.Attribute[] attributes,
    [UniqueId] string? id = null)
```

Void elements: `area`, `br`, `col`, `embed`, `hr`, `img`, `input`, `meta`, `@base`, `param`, `source`, `track`, `wbr`.

### Core Element Factory

All element functions delegate to the core factory:

```csharp
public static Element element(
    string tag,
    DOM.Attribute[] attributes,
    Node[] children,
    [UniqueId] string? id = null)
```

If any attribute has `Name == "id"`, that value overrides the Praefixum-generated ID for the element. This enables explicit DOM identification.

### Parameters

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `tag` | `string` | HTML tag name (only on the core `element` factory) |
| `attributes` | `DOM.Attribute[]` | Array of attributes — use collection expression syntax `[...]` |
| `children` | `Node[]` | Array of child nodes — use collection expression syntax `[...]` |
| `id` | `string?` | Auto-generated compile-time unique ID via Praefixum source generator |

## Text & Raw HTML

### text

Creates a text node. Content is HTML-encoded during rendering:

```csharp
text("Hello, World!")
text(model.Username)
text($"Count: {model.Count}")
```

### raw

Creates a raw HTML node. Content is inserted **without encoding** — use with caution:

```csharp
raw("<strong>Bold text</strong>")
raw(model.MarkdownHtml)  // Pre-rendered markdown
```

## Memoization

### lazy

Creates a lazily memoized node. The factory function is only called when the key changes — when the key matches the previous render, the entire subtree is skipped during diffing:

```csharp
public static Node lazy<TKey>(
    TKey key,
    Func<Node> factory,
    [UniqueId] string? id = null) where TKey : notnull
```

Example:

```csharp
// The expensive ArticleView is only rebuilt when article changes
lazy(article, () => ArticleView(article))

// Memoize by a specific key
lazy(article.UpdatedAt, () => ArticleView(article))
```

The view cache optimization returns the **exact same object reference** when the compile-time ID and key match. This enables `ReferenceEquals` bailout in `DiffInternal` — an O(1) skip that avoids all key comparison, dictionary building, and subtree diffing. Inspired by Elm's `lazy` function.

### memo

Creates an eagerly memoized node. The node content is provided directly and the key is compared during diffing to skip unchanged subtrees:

```csharp
public static Node memo<TKey>(
    TKey key,
    Node node,
    [UniqueId] string? id = null) where TKey : notnull
```

Example:

```csharp
// Skip diffing this subtree if model.Count hasn't changed
memo(model.Count, div([], [text($"Count: {model.Count}")]))
```

### View Cache Management

```csharp
// Clear the lazy view cache (call on navigation or major view changes)
Elements.ClearViewCache();

// Check current cache size for diagnostics
int size = Elements.ViewCacheCount;
```

The cache automatically trims when it exceeds 2000 entries.

## Document Root & Metadata

```csharp
html([lang("en")], [...])
head([], [...])
body([], [...])
title([], [text("Page Title")])
style([], [text("body { margin: 0; }")])
script([], [text("console.log('hello');")])
noscript([], [text("JavaScript required")])
link([rel("stylesheet"), href("/styles.css")])        // void
meta([name("viewport"), content("width=device-width")]) // void
@base([href("/")])                                      // void — @ prefix for C# keyword
```

## Content Sectioning

```csharp
div([class_("container")], [text("Content")])
span([class_("highlight")], [text("Important")])
section([class_("hero")], [h1([], [text("Welcome")])])
article([class_("blog-post")], [p([], [text("Body")])])
aside([class_("sidebar")], [nav([], [...])])
header([class_("page-header")], [h1([], [text("My App")])])
footer([class_("page-footer")], [text("© 2025 My Company")])
main([class_("content")], [...])
nav([class_("navbar")], [a([href("/")], [text("Home")])])
hgroup([], [h1([], [text("Title")]), p([], [text("Subtitle")])])
address([], [text("Contact info")])
```

## Headings

```csharp
h1([class_("title")], [text("Main Heading")])
h2([], [text("Section Heading")])
h3([], [text("Subsection")])
h4([], [text("Minor Heading")])
h5([], [text("Small Heading")])
h6([], [text("Smallest Heading")])
```

## Text Content

```csharp
p([class_("intro")], [text("Paragraph text.")])
blockquote([cite_("source-url")], [p([], [text("A quote.")])])
pre([], [code([], [text("const x = 42;")])])
code([class_("inline")], [text("npm install")])
```

## Text Formatting

```csharp
strong([], [text("Bold")])          // <strong>
em([], [text("Italic")])            // <em>
small([], [text("Small")])          // <small>
mark([], [text("Highlighted")])     // <mark>
del([], [text("Deleted")])          // <del>
ins([], [text("Inserted")])         // <ins>
sub([], [text("Subscript")])        // <sub>
sup([], [text("Superscript")])      // <sup>
b([], [text("Bold")])               // <b>
i([], [text("Italic")])             // <i>
u([], [text("Underline")])          // <u>
s([], [text("Strikethrough")])      // <s>
abbr([title("HyperText")], [text("HTML")])
cite([], [text("Source Title")])
dfn([], [text("Term")])
kbd([], [text("Ctrl+C")])
samp([], [text("Output")])
q([], [text("Inline quote")])
time([datetime("2025-01-01")], [text("New Year")])
@var([], [text("x")])               // @ prefix for C# keyword
bdi([], [text("Isolated")])         // bidirectional isolation
bdo([dir("rtl")], [text("Right to left")])
```

## Lists

### Unordered List

```csharp
ul([class_("menu")], [
    li([], [text("Item 1")]),
    li([], [text("Item 2")]),
    li([], [text("Item 3")])
])
```

### Ordered List

```csharp
ol([start("5")], [
    li([], [text("Fifth item")]),
    li([], [text("Sixth item")])
])
```

### Definition List

```csharp
dl([], [
    dt([], [text("Term")]),
    dd([], [text("Definition")])
])
```

### Dynamic Lists

For dynamic lists, use the `id:` parameter for stable element identity:

```csharp
ul([], [
    ..model.Items.Select(item =>
        li([], [text(item.Name)], id: $"item-{item.Id}")
    )
])
```

## Links and Navigation

```csharp
a([href("/profile"), class_("link")], [text("View Profile")])

// External link
a([href("https://example.com"), target("_blank"), rel("noopener")], [
    text("External Site")
])
```

## Images and Media

```csharp
// img is a void element — no children parameter
img([src("/images/logo.png"), alt("Company Logo"), width("200")])

// Responsive images
picture([], [
    source([srcset("/images/large.jpg"), media("(min-width: 800px)")]),
    source([srcset("/images/small.jpg")]),
    img([src("/images/fallback.jpg"), alt("Photo")])
])

// Video with controls
video([controls(), width("640"), height("360")], [
    source([src("/video.mp4"), type("video/mp4")]),
    text("Your browser doesn't support video.")
])

// Audio
audio([controls()], [
    source([src("/audio.mp3"), type("audio/mp3")])
])

// Embedded content
iframe([src("https://example.com"), width("560"), height("315"), allowfullscreen()], [])
canvas([width("800"), height("600")], [])
figure([], [img([src("/photo.jpg"), alt("Photo")]), figcaption([], [text("Caption")])])
object_([], [])  // _ suffix for C# keyword avoidance
```

## Forms

### form

```csharp
form([onsubmit(new SubmitForm())], [
    // Form fields
])
```

### input (void element)

```csharp
input([
    type("text"),
    name("username"),
    placeholder("Enter username"),
    value(model.Username),
    oninput(e => new UsernameChanged(e?.Value ?? ""))
])
```

Common input types:

```csharp
input([type("text"), ...])
input([type("password"), ...])
input([type("email"), ...])
input([type("number"), ...])
input([type("date"), ...])
input([type("checkbox"), checked_(), ...])
input([type("radio"), name("choice"), value("option1"), ...])
input([type("file"), accept(".jpg,.png"), ...])
input([type("hidden"), name("token"), value(model.Token)])
```

### textarea

```csharp
textarea([
    name("body"),
    rows("10"),
    placeholder("Write your article..."),
    oninput(e => new BodyChanged(e?.Value ?? ""))
], [text(model.Body)])
```

### select / option

```csharp
select([
    name("country"),
    onchange(e => new CountryChanged(e?.Value ?? ""))
], [
    option([value("")], [text("Select country...")]),
    option([value("us"), selected()], [text("United States")]),
    option([value("uk")], [text("United Kingdom")])
])
```

### button

```csharp
button([type("submit"), class_("btn")], [text("Submit")])
button([type("button"), onclick(new Cancel())], [text("Cancel")])
button([disabled(), class_("btn")], [text("Disabled")])
```

### label

```csharp
label([for_("email")], [text("Email Address")])
input([id("email"), type("email"), name("email")])
```

### fieldset / legend

```csharp
fieldset([], [
    legend([], [text("Personal Information")]),
    // Fields here
])
```

### Other Form Elements

```csharp
optgroup([label("Group")], [option([value("a")], [text("A")])])
datalist([id("suggestions")], [option([value("Suggestion 1")], [])])
output([], [text("Result")])
progress([value("75"), max("100")], [])
meter([value("75"), min("0"), max("100"), Attributes.low("25"), Attributes.high("75")], [])
```

## Tables

```csharp
table([class_("data-table")], [
    caption([], [text("User List")]),
    colgroup([], [col([width("200")]), col([width("300")])]),
    thead([], [
        tr([], [
            th([scope("col")], [text("Name")]),
            th([scope("col")], [text("Email")])
        ])
    ]),
    tbody([], [
        ..model.Users.Select(user =>
            tr([], [
                td([], [text(user.Name)]),
                td([], [text(user.Email)])
            ], id: $"user-row-{user.Id}")
        )
    ]),
    tfoot([], [
        tr([], [td([colspan("2")], [text($"Total: {model.Users.Count}")])])
    ])
])
```

## Interactive Elements

```csharp
// Collapsible content
details([open_()], [
    summary([], [text("Click to expand")]),
    p([], [text("Hidden content here")])
])

// Modal dialog
dialog([open_()], [
    h2([], [text("Confirm Action")]),
    p([], [text("Are you sure?")]),
    button([onclick(new CloseModal())], [text("Close")])
])

// Menu
menu([], [
    li([], [button([], [text("Cut")])]),
    li([], [button([], [text("Copy")])])
])
```

> **Note:** The `open` attribute function is `open_()` with a trailing underscore to avoid conflict with the C# contextual keyword.

## Miscellaneous Elements

```csharp
template([], [div([], [text("Template content")])])
slot([name("header")], [])
portal([], [])
data([value("42")], [text("Forty-two")])
math([], [text("x² + y² = z²")])
```

## SVG Elements

### Basic Shapes

SVG shape elements are void elements (no children parameter):

```csharp
svg([viewBox("0 0 100 100"), width("100"), height("100")], [
    circle([cx("50"), cy("50"), r("40"), fill("red")]),
    rect([x("10"), y("10"), width("30"), height("30"), fill("blue")]),
    path([d("M10 80 L50 10 L90 80 Z"), fill("green")]),
    line([x1("0"), y1("0"), x2("100"), y2("100"), stroke("black")]),
    ellipse([cx("50"), cy("50"), rx("40"), ry("20"), fill("orange")]),
    polyline([points("0,0 50,50 100,0"), stroke("black"), fill("none")]),
    polygon([points("50,0 100,100 0,100"), fill("purple")])
])
```

### SVG Container Elements

```csharp
g([transform("translate(10, 10)")], [...])
defs([], [...])
symbol([viewBox("0 0 100 100")], [...])
use([xlinkHref("#icon")])
foreignObject([width("100"), height("100")], [div([], [text("HTML in SVG")])])
```

### SVG Text

```csharp
// Note: SVG text() takes attributes + children (different from text(string))
Elements.text([x("50"), y("50"), textAnchor("middle")], [text("SVG Text")])
tspan([dx("10")], [text("Offset text")])
```

### Gradients & Patterns

```csharp
defs([], [
    linearGradient([id("grad")], [
        stop([Attributes.attribute("offset", "0%"), Attributes.attribute("stop-color", "red")]),
        stop([Attributes.attribute("offset", "100%"), Attributes.attribute("stop-color", "blue")])
    ]),
    radialGradient([id("rgrad")], [...]),
    pattern([id("pat"), patternUnits("userSpaceOnUse"), width("10"), height("10")], [...])
])
```

### Clipping & Masking

```csharp
defs([], [
    clipPath([id("clip")], [rect([x("0"), y("0"), width("50"), height("50")])]),
    mask([id("mask")], [rect([x("0"), y("0"), width("100"), height("100"), fill("white")])])
])
```

### SVG Filter Primitives

All filter primitives are void elements:

```csharp
filter([id("blur")], [
    feGaussianBlur([Attributes.attribute("stdDeviation", "5")])
])
```

Available filter primitives: `feBlend`, `feColorMatrix`, `feComposite`, `feConvolveMatrix`, `feDiffuseLighting`, `feDisplacementMap`, `feFlood`, `feGaussianBlur`, `feImage`, `feMerge` (container), `feMergeNode`, `feMorphology`, `feOffset`, `feSpecularLighting` (container), `feTile`, `feTurbulence`, `feComponentTransfer` (container).

## C# Keyword Avoidance

Some HTML elements and attributes conflict with C# keywords or contextual keywords. Abies uses trailing underscores or `@` prefixes:

| HTML | C# Function | Reason |
| ---- | ----------- | ------ |
| `<base>` | `@base(...)` | `base` is a C# keyword |
| `<var>` | `@var(...)` | `var` is a contextual keyword |
| `<object>` | `object_(...)` | `object` is a C# keyword |

## Complete Element Inventory

### Normal Elements (attributes + children)

**Document:** `html`, `head`, `body`, `title`, `style`, `script`, `noscript`, `link`

**Sectioning:** `div`, `span`, `p`, `a`, `header`, `footer`, `nav`, `main`, `section`, `article`, `aside`, `hgroup`, `address`

**Headings:** `h1`, `h2`, `h3`, `h4`, `h5`, `h6`

**Text:** `strong`, `em`, `small`, `code`, `pre`, `blockquote`, `b`, `i`, `u`, `s`, `del`, `ins`, `abbr`, `cite`, `dfn`, `kbd`, `samp`, `sup`, `sub`, `mark`, `q`, `time`, `@var`, `bdi`, `bdo`, `ruby`, `rt`, `rp`, `rb`, `rtc`

**Lists:** `ul`, `ol`, `li`, `dl`, `dt`, `dd`

**Tables:** `table`, `thead`, `tbody`, `tfoot`, `tr`, `td`, `th`, `caption`, `colgroup`

**Forms:** `form`, `button`, `select`, `option`, `optgroup`, `textarea`, `label`, `fieldset`, `legend`, `datalist`, `output`, `progress`, `meter`

**Media:** `video`, `audio`, `canvas`, `iframe`, `picture`, `object_`, `map`, `figure`, `figcaption`

**Interactive:** `details`, `summary`, `dialog`, `template`, `slot`, `portal`, `menu`, `menuitem`, `data`, `math`

**SVG containers:** `svg`, `g`, `defs`, `symbol`, `linearGradient`, `radialGradient`, `mask`, `clipPath`, `pattern`, `text` (SVG), `tspan`, `desc`, `foreignObject`, `filter`, `feComponentTransfer`, `feDiffuseLighting`, `feMerge`, `feSpecularLighting`

### Void Elements (attributes only)

**HTML:** `br`, `hr`, `img`, `input`, `area`, `col`, `embed`, `param`, `source`, `track`, `wbr`, `meta`, `@base`

**SVG:** `use`, `path`, `circle`, `rect`, `ellipse`, `line`, `polyline`, `polygon`, `stop`, `feBlend`, `feColorMatrix`, `feComposite`, `feConvolveMatrix`, `feDisplacementMap`, `feFlood`, `feGaussianBlur`, `feImage`, `feMergeNode`, `feMorphology`, `feOffset`, `feTile`, `feTurbulence`

## Complete Example

```csharp
public static Document View(Model model)
    => new("My App",
        div([class_("app")], [
            header([class_("header")], [
                h1([], [text("My Application")]),
                nav([], [
                    a([href("/"), class_("nav-link")], [text("Home")]),
                    a([href("/about"), class_("nav-link")], [text("About")])
                ])
            ]),
            main([class_("content")], [
                model.IsLoading
                    ? div([class_("loading")], [text("Loading...")])
                    : div([class_("articles")], [
                        ..model.Articles.Select(a =>
                            // Use lazy to skip diffing unchanged articles
                            lazy(a.UpdatedAt, () =>
                                article([class_("article-card")], [
                                    h2([], [text(a.Title)]),
                                    p([], [text(a.Description)]),
                                    a([href($"/article/{a.Slug}")], [text("Read more")])
                                ], id: $"article-{a.Slug}")
                            )
                        )
                    ])
            ]),
            footer([class_("footer")], [
                text("© 2025 My Company")
            ])
        ]));
```

## See Also

- [Attributes API](./html-attributes.md) — Element attributes
- [Events API](./html-events.md) — Event handlers
- [DOM Types](./dom-types.md) — Node, Element, and Document types
- [Concepts: Virtual DOM](../concepts/virtual-dom.md) — How elements work
