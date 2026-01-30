# HTML Elements API Reference

The `Abies.Html.Elements` static class provides functions for creating virtual DOM elements.

## Usage

```csharp
using Abies.Html;
using static Abies.Html.Elements;
```

## Element Function Signature

Most element functions follow this pattern:

```csharp
public static Node element(
    DOM.Attribute[] attributes, 
    Node[] children, 
    [UniqueId] string? id = null)
```

- `attributes` — Array of attributes for the element
- `children` — Array of child nodes
- `id` — Auto-generated compile-time unique ID

## Document Elements

### text

Creates a text node:

```csharp
text("Hello, World!")
text(model.Username)
text($"Count: {model.Count}")
```

### raw

Creates raw HTML (use with caution):

```csharp
raw("<strong>Bold text</strong>")
raw(model.MarkdownHtml)  // Pre-rendered markdown
```

## Structural Elements

### div

Generic container:

```csharp
div([class_("container")], [
    text("Content here")
])
```

### span

Inline container:

```csharp
span([class_("highlight")], [text("Important")])
```

### section

Document section:

```csharp
section([class_("hero")], [
    h1([], [text("Welcome")])
])
```

### article

Self-contained content:

```csharp
article([class_("blog-post")], [
    header([], [h2([], [text(post.Title)])]),
    p([], [text(post.Body)])
])
```

### aside

Sidebar content:

```csharp
aside([class_("sidebar")], [
    nav([], [...])
])
```

### header

Introductory content:

```csharp
header([class_("page-header")], [
    h1([], [text("My App")]),
    nav([], [...])
])
```

### footer

Footer content:

```csharp
footer([class_("page-footer")], [
    text("© 2024 My Company")
])
```

### main

Main content area:

```csharp
main([class_("content")], [
    // Primary content
])
```

### nav

Navigation links:

```csharp
nav([class_("navbar")], [
    a([href("/")], [text("Home")]),
    a([href("/about")], [text("About")])
])
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

### p

Paragraph:

```csharp
p([class_("intro")], [text("This is a paragraph.")])
```

### blockquote

Quotation:

```csharp
blockquote([cite("source-url")], [
    p([], [text("A famous quote.")])
])
```

### pre

Preformatted text:

```csharp
pre([], [
    code([], [text("const x = 42;")])
])
```

### code

Inline code:

```csharp
code([class_("inline-code")], [text("npm install")])
```

## Text Formatting

```csharp
strong([], [text("Bold text")])
em([], [text("Italic text")])
small([], [text("Small text")])
mark([], [text("Highlighted")])
del([], [text("Deleted")])
ins([], [text("Inserted")])
sub([], [text("Subscript")])
sup([], [text("Superscript")])
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

### Dynamic Lists

```csharp
ul([], [
    ..model.Items.Select(item => 
        li([key(item.Id)], [text(item.Name)])
    )
])
```

## Links and Navigation

### a

Anchor link:

```csharp
a([href("/profile"), class_("link")], [text("View Profile")])

// External link
a([href("https://example.com"), target("_blank"), rel("noopener")], [
    text("External Site")
])
```

## Images and Media

### img

Image:

```csharp
img([src("/images/logo.png"), alt("Company Logo"), width("200")])
```

### picture

Responsive images:

```csharp
picture([], [
    source([srcset("/images/large.jpg"), media("(min-width: 800px)")]),
    source([srcset("/images/small.jpg")]),
    img([src("/images/fallback.jpg"), alt("Photo")])
])
```

### video

Video player:

```csharp
video([controls(), width("640"), height("360")], [
    source([src("/video.mp4"), type("video/mp4")]),
    text("Your browser doesn't support video.")
])
```

### audio

Audio player:

```csharp
audio([controls()], [
    source([src("/audio.mp3"), type("audio/mp3")])
])
```

### iframe

Embedded content:

```csharp
iframe([
    src("https://www.youtube.com/embed/xyz"),
    width("560"),
    height("315"),
    allowfullscreen()
], [])
```

## Forms

### form

Form container:

```csharp
form([onsubmit(new SubmitForm())], [
    // Form fields
])
```

### input

Text input:

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
input([type("checkbox"), checked_(model.IsChecked ? "true" : null), ...])
input([type("radio"), name("choice"), value("option1"), ...])
input([type("file"), accept(".jpg,.png"), ...])
input([type("hidden"), name("token"), value(model.Token)])
```

### textarea

Multi-line text:

```csharp
textarea([
    name("body"),
    rows("10"),
    placeholder("Write your article..."),
    oninput(e => new BodyChanged(e?.Value ?? ""))
], [text(model.Body)])
```

### select

Dropdown:

```csharp
select([
    name("country"),
    onchange(e => new CountryChanged(e?.Value ?? ""))
], [
    option([value("")], [text("Select country...")]),
    option([value("us"), selected(model.Country == "us")], [text("United States")]),
    option([value("uk")], [text("United Kingdom")])
])
```

### button

Button:

```csharp
button([type("submit"), class_("btn")], [text("Submit")])
button([type("button"), onclick(new Cancel())], [text("Cancel")])
button([disabled(model.IsLoading ? "true" : null)], [text("Save")])
```

### label

Form label:

```csharp
label([for_("email")], [text("Email Address")])
input([id("email"), type("email"), name("email")])
```

### fieldset / legend

Field grouping:

```csharp
fieldset([], [
    legend([], [text("Personal Information")]),
    // Fields here
])
```

## Tables

```csharp
table([class_("data-table")], [
    thead([], [
        tr([], [
            th([], [text("Name")]),
            th([], [text("Email")]),
            th([], [text("Actions")])
        ])
    ]),
    tbody([], [
        ..model.Users.Select(user => 
            tr([key(user.Id)], [
                td([], [text(user.Name)]),
                td([], [text(user.Email)]),
                td([], [
                    button([onclick(new EditUser(user.Id))], [text("Edit")])
                ])
            ])
        )
    ])
])
```

## Interactive Elements

### details / summary

Collapsible content:

```csharp
details([open(model.IsExpanded ? "true" : null)], [
    summary([], [text("Click to expand")]),
    p([], [text("Hidden content here")])
])
```

### dialog

Modal dialog:

```csharp
dialog([open(model.ShowModal ? "true" : null)], [
    h2([], [text("Confirm Action")]),
    p([], [text("Are you sure?")]),
    button([onclick(new CloseModal())], [text("Close")])
])
```

### progress

Progress bar:

```csharp
progress([value(model.Progress.ToString()), max("100")], [])
```

### meter

Gauge:

```csharp
meter([value("75"), min("0"), max("100"), low("25"), high("75")], [])
```

## SVG Elements

```csharp
svg([viewBox("0 0 100 100"), width("100"), height("100")], [
    circle([cx("50"), cy("50"), r("40"), fill("red")]),
    rect([x("10"), y("10"), width("30"), height("30"), fill("blue")]),
    path([d("M10 80 L50 10 L90 80 Z"), fill("green")]),
    line([x1("0"), y1("0"), x2("100"), y2("100"), stroke("black")]),
    text([x("50"), y("50")], [text("SVG")])
])
```

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
                        ..model.Articles.Select(article =>
                            article_([key(article.Slug), class_("article-card")], [
                                h2([], [text(article.Title)]),
                                p([], [text(article.Description)]),
                                a([href($"/article/{article.Slug}")], [text("Read more")])
                            ])
                        )
                    ])
            ]),
            footer([class_("footer")], [
                text("© 2024 My Company")
            ])
        ]));

// Note: article_() for the <article> element to avoid C# keyword conflict
```

## See Also

- [Attributes API](./html-attributes.md) — Element attributes
- [Events API](./html-events.md) — Event handlers
- [Concepts: Virtual DOM](../concepts/virtual-dom.md) — How elements work
