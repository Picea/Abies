# HTML Attributes API Reference

The `Abies.Html.Attributes` static class provides functions for creating HTML attributes. 

## Usage

```csharp
using Abies.Html;
using static Abies.Html.Attributes;
```

## Generic Attribute

### attribute

Create any attribute:

```csharp
attribute("data-custom", "value")
attribute("aria-label", "Description")
```

## Identification

### id

Element identifier:

```csharp
id("main-content")
```

### class_

CSS class (note underscore to avoid C# keyword):

```csharp
class_("btn btn-primary")
class_("container")

// Conditional classes
class_(model.IsActive ? "active" : "inactive")
class_($"card {(model.Selected ? "selected" : "")}")
```

### name

Form element name:

```csharp
name("username")
name("email")
```

## Links

### href

Link destination:

```csharp
href("/profile")
href("https://example.com")
href("#section")
href("mailto:contact@example.com")
```

### src

Media source:

```csharp
src("/images/photo.jpg")
src("https://cdn.example.com/video.mp4")
```

### target

Link target:

```csharp
target("_blank")   // New tab
target("_self")    // Same tab (default)
target("_parent")  // Parent frame
target("_top")     // Full window
```

### rel

Link relationship:

```csharp
rel("noopener noreferrer")  // For external links
rel("stylesheet")
rel("icon")
```

### download

Download link:

```csharp
download("report.pdf")
download()  // Use original filename
```

## Content

### alt

Image alt text:

```csharp
alt("Company logo")
alt("Photo of sunset over mountains")
```

### title

Tooltip text:

```csharp
title("Click to expand")
title("More information")
```

### placeholder

Input placeholder:

```csharp
placeholder("Enter your email")
placeholder("Search...")
```

### value

Input value:

```csharp
value(model.Username)
value(model.Count.ToString())
```

## Dimensions

### width / height

Element dimensions:

```csharp
width("100")
height("50")
width("100%")
height("auto")
```

## Form Attributes

### type

Input type:

```csharp
type("text")
type("password")
type("email")
type("number")
type("checkbox")
type("radio")
type("submit")
type("button")
type("hidden")
type("file")
type("date")
type("time")
type("range")
type("color")
```

### disabled

Disable element:

```csharp
disabled()
disabled(model.IsSubmitting ? "true" : null)  // Conditional
```

### readonly_

Read-only input (note underscore):

```csharp
readonly_()
```

### required

Required field:

```csharp
required()
```

### checked_

Checkbox/radio checked (note underscore):

```csharp
checked_()
checked_(model.IsChecked ? "true" : null)
```

### selected

Option selected:

```csharp
selected()
selected(model.Value == "option1" ? "true" : null)
```

### multiple

Allow multiple selections:

```csharp
multiple()
```

### autocomplete

Autocomplete behavior:

```csharp
autocomplete("off")
autocomplete("email")
autocomplete("current-password")
```

### autofocus

Auto-focus on load:

```csharp
autofocus()
```

### maxlength / minlength

Text length limits:

```csharp
maxlength("100")
minlength("3")
```

### pattern

Input pattern validation:

```csharp
pattern("[0-9]{5}")  // 5 digits
pattern("[A-Za-z]+")  // Letters only
```

### min / max / step

Number range:

```csharp
min("0")
max("100")
step("5")
```

### rows / cols

Textarea dimensions:

```csharp
rows("10")
cols("50")
```

### for_

Label association (note underscore):

```csharp
for_("email-input")
```

### form

Associate with form:

```csharp
form("login-form")
```

### action / method

Form submission:

```csharp
action("/api/submit")
method("post")
```

### enctype

Form encoding:

```csharp
enctype("multipart/form-data")  // For file uploads
enctype("application/json")
```

### novalidate

Disable validation:

```csharp
novalidate()
```

## Table Attributes

### colspan / rowspan

Cell spanning:

```csharp
colspan("2")
rowspan("3")
```

### scope

Header scope:

```csharp
scope("col")
scope("row")
```

## Media Attributes

### controls

Show media controls:

```csharp
controls()
```

### autoplay

Auto-play media:

```csharp
autoplay()
```

### loop

Loop media:

```csharp
loop()
```

### muted

Mute audio:

```csharp
muted()
```

### poster

Video poster image:

```csharp
poster("/images/video-poster.jpg")
```

### preload

Preload behavior:

```csharp
preload("auto")
preload("metadata")
preload("none")
```

## Styling

### style

Inline styles:

```csharp
style("color: red; font-size: 16px")
style($"width: {model.Width}px")
```

### hidden

Hide element:

```csharp
hidden()
hidden(model.IsHidden ? "true" : null)
```

## Accessibility

### role

ARIA role:

```csharp
role("button")
role("navigation")
role("alert")
role("dialog")
```

### ariaLabel

Accessible label:

```csharp
ariaLabel("Close dialog")
ariaLabel("Submit form")
```

### ariaHidden

Hide from assistive technology:

```csharp
ariaHidden()
ariaHidden("true")
```

### ariaExpanded

Expansion state:

```csharp
ariaExpanded(model.IsExpanded ? "true" : "false")
```

### ariaControls

Controlled element:

```csharp
ariaControls("dropdown-menu")
```

### ariaDescribedby

Description reference:

```csharp
ariaDescribedby("help-text")
```

### tabindex

Tab order:

```csharp
tabindex("0")   // In tab order
tabindex("-1")  // Focusable but not in tab order
tabindex("1")   // Specific order (avoid)
```

## Data Attributes

### data

Custom data attribute:

```csharp
data("id", "123")          // data-id="123"
data("action", "delete")   // data-action="delete"
data("user-id", "456")     // data-user-id="456"
```

## SVG Attributes

### viewBox

SVG viewBox:

```csharp
viewBox("0 0 100 100")
```

### fill / stroke

SVG colors:

```csharp
fill("red")
fill("#ff0000")
fill("none")
stroke("black")
strokeWidth("2")
```

### SVG Geometry

```csharp
cx("50")        // Circle center x
cy("50")        // Circle center y
r("25")         // Circle radius
x("10")         // Rectangle x
y("10")         // Rectangle y
x1("0")         // Line start x
y1("0")         // Line start y
x2("100")       // Line end x
y2("100")       // Line end y
d("M10 10 L90 90")  // Path data
transform("rotate(45)")
```

## Loading and Performance

### loading

Lazy loading:

```csharp
loading("lazy")   // Defer loading until visible
loading("eager")  // Load immediately
```

### async / defer

Script loading:

```csharp
async()
defer()
```

### crossorigin

Cross-origin requests:

```csharp
crossorigin("anonymous")
crossorigin("use-credentials")
```

### integrity

Subresource integrity:

```csharp
integrity("sha384-...")
```

## Miscellaneous

### lang

Language:

```csharp
lang("en")
lang("fr")
```

### dir

Text direction:

```csharp
dir("ltr")
dir("rtl")
```

### spellcheck

Spell checking:

```csharp
spellcheck("true")
spellcheck("false")
```

### contenteditable

Editable content:

```csharp
contenteditable("true")
```

### draggable

Drag and drop:

```csharp
draggable("true")
```

## Conditional Attributes

Handle nullable values:

```csharp
// Only add attribute if value is not null
...(model.CustomClass is not null 
    ? [class_(model.CustomClass)] 
    : [])

// Conditional disabled
button([
    onclick(new Submit()),
    ...(model.IsValid ? [] : [disabled()])
], [text("Submit")])
```

## Complete Example

```csharp
form([
    class_("login-form"),
    method("post"),
    action("/api/login"),
    onsubmit(new SubmitLogin())
], [
    div([class_("form-group")], [
        label([for_("email")], [text("Email")]),
        input([
            type("email"),
            id("email"),
            name("email"),
            class_("form-control"),
            placeholder("Enter your email"),
            required(),
            value(model.Email),
            ariaDescribedby("email-help"),
            oninput(e => new EmailChanged(e?.Value ?? ""))
        ]),
        small([id("email-help"), class_("form-text")], [
            text("We'll never share your email.")
        ])
    ]),
    div([class_("form-group")], [
        label([for_("password")], [text("Password")]),
        input([
            type("password"),
            id("password"),
            name("password"),
            class_("form-control"),
            placeholder("Enter password"),
            required(),
            minlength("8"),
            oninput(e => new PasswordChanged(e?.Value ?? ""))
        ])
    ]),
    button([
        type("submit"),
        class_("btn btn-primary"),
        disabled(model.IsSubmitting ? "true" : null)
    ], [
        text(model.IsSubmitting ? "Logging in..." : "Login")
    ])
])
```

## See Also

- [Elements API](./html-elements.md) — HTML elements
- [Events API](./html-events.md) — Event handlers
