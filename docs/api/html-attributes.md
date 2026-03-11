# HTML Attributes API Reference

The `Picea.Abies.Html.Attributes` static class provides functions for creating HTML attributes.

## Usage

```csharp
using Picea.Abies.Html;
using static Picea.Abies.Html.Attributes;
```

## Core Attribute Factory

### attribute

Create any attribute by name and value:

```csharp
public static DOM.Attribute attribute(
    string name,
    string value,
    [UniqueId] string? id = null)
```

Example:

```csharp
attribute("data-custom", "value")
attribute("aria-label", "Description")
attribute("x-data", "{ open: false }")
```

All named attribute functions below are convenience wrappers around `attribute`.

## Global Attributes

```csharp
id("main-content")                  // id="main-content"
class_("btn btn-primary")           // class="btn btn-primary" — underscore avoids C# keyword
style("color: red; font-size: 16px") // style="..."
title("Tooltip text")               // title="Tooltip text"
lang("en")                          // lang="en"
dir("ltr")                          // dir="ltr" | "rtl"
tabindex("0")                       // tabindex="0"
accesskey("s")                      // accesskey="s"
role("button")                      // role="button"
slot("header")                      // slot="header"
```

## Boolean Attributes

Boolean attributes default to `"true"` when called with no arguments:

```csharp
public static DOM.Attribute disabled(
    string value = "true",
    [UniqueId] string? id = null)
```

All boolean attributes follow this pattern:

```csharp
// Common boolean attributes
hidden()                            // hidden="true"
disabled()                          // disabled="true"
checked_()                          // checked="true" — underscore avoids C# keyword
selected()                          // selected="true"
readonly_()                         // readonly="true" — underscore avoids C# keyword
required()                          // required="true"
multiple()                          // multiple="true"
autofocus()                         // autofocus="true"
autoplay()                          // autoplay="true"
controls()                          // controls="true"
loop()                              // loop="true"
muted()                             // muted="true"
defer()                             // defer="true"
async_()                            // async="true" — underscore avoids C# keyword
novalidate()                        // novalidate="true"
formnovalidate()                    // formnovalidate="true"
open_()                             // open="true" — underscore avoids C# keyword
inert()                             // inert="true"
reversed()                          // reversed="true"
allowfullscreen()                   // allowfullscreen="true"
default_()                          // default="true" — underscore avoids C# keyword
ismap()                             // ismap="true"
nomodule()                          // nomodule="true"
playsinline()                       // playsinline="true"
contenteditable()                   // contenteditable="true"
draggable()                         // draggable="true"
spellcheck()                        // spellcheck="true"
```

## Editability & Interaction

```csharp
translate("yes")                    // translate="yes" | "no"
inputmode("numeric")                // inputmode="numeric" | "text" | "email" | ...
enterkeyhint("send")                // enterkeyhint="send" | "search" | "go" | ...
```

## Form Attributes

### Input Attributes

```csharp
type("text")                        // type="text"
name("username")                    // name="username"
value(model.Username)               // value="..."
placeholder("Enter email")          // placeholder="Enter email"
pattern("[0-9]{5}")                 // pattern="[0-9]{5}"
autocomplete("email")               // autocomplete="email"
list("suggestions")                 // list="suggestions" — associates with datalist
size("20")                          // size="20"
accept(".jpg,.png")                 // accept=".jpg,.png" — for file inputs
```

### Number & Range

```csharp
min("0")
max("100")
step("5")
```

### Text Length

```csharp
maxlength("100")
minlength("3")
```

### Textarea

```csharp
rows("10")
cols("50")
wrap("soft")                        // wrap="soft" | "hard"
```

### Label & Form Association

```csharp
for_("email-input")                 // for="email-input" — underscore avoids C# keyword
form("login-form")                  // form="login-form"
```

### Form Submission

```csharp
action("/api/submit")               // action="/api/submit"
method("post")                      // method="post"
enctype("multipart/form-data")      // enctype="multipart/form-data"
formaction("/api/alt-submit")       // formaction="..." — overrides form action
formmethod("post")                  // formmethod="..." — overrides form method
formenctype("multipart/form-data")  // formenctype="..." — overrides form enctype
formtarget("_blank")                // formtarget="..." — overrides form target
```

## Link & Navigation Attributes

```csharp
href("/profile")                    // href="/profile"
src("/images/photo.jpg")            // src="/images/photo.jpg"
target("_blank")                    // target="_blank" | "_self" | "_parent" | "_top"
rel("noopener noreferrer")          // rel="noopener noreferrer"
download("report.pdf")              // download="report.pdf" — requires a value
ping("https://analytics.example.com") // ping="..."
referrerpolicy("no-referrer")       // referrerpolicy="no-referrer"
hreflang("en")                      // hreflang="en"
```

## Image & Media Attributes

```csharp
alt("Company logo")                 // alt="Company logo"
width("200")                        // width="200"
height("150")                       // height="150"
loading("lazy")                     // loading="lazy" | "eager"
decoding("async")                   // decoding="async" | "sync" | "auto"
srcset("/images/large.jpg 2x")      // srcset="..."
sizes("(max-width: 600px) 100vw")   // sizes="..."
poster("/images/poster.jpg")        // poster="..."
preload("metadata")                 // preload="auto" | "metadata" | "none"
crossorigin("anonymous")            // crossorigin="anonymous" | "use-credentials"
usemap("#imagemap")                 // usemap="#imagemap"
```

## Table Attributes

```csharp
colspan("2")                        // colspan="2"
rowspan("3")                        // rowspan="3"
scope("col")                        // scope="col" | "row"
headers("header-id")                // headers="header-id"
span_("3")                          // span="3" — underscore avoids C# keyword
```

## Meta & Script Attributes

```csharp
charset("utf-8")                    // charset="utf-8"
content("width=device-width")       // content="width=device-width"
http_equiv("refresh")               // http-equiv="refresh"
integrity("sha384-...")             // integrity="sha384-..."
nonce("abc123")                     // nonce="abc123"
media("(min-width: 800px)")          // media="(min-width: 800px)"
```

## Iframe & Embed Attributes

```csharp
sandbox("allow-scripts")            // sandbox="allow-scripts"
allow("autoplay; fullscreen")       // allow="autoplay; fullscreen"
```

## ARIA Attributes

```csharp
ariaLabel("Close dialog")           // aria-label="Close dialog"
ariaLabelledby("title-id")          // aria-labelledby="title-id"
ariaDescribedby("help-text")        // aria-describedby="help-text"
ariaHidden("true")                  // aria-hidden="true" — requires a value
ariaExpanded("false")               // aria-expanded="false"
ariaControls("dropdown-menu")       // aria-controls="dropdown-menu"
ariaLive("polite")                  // aria-live="polite" | "assertive" | "off"
ariaAtomic("true")                  // aria-atomic="true"
ariaCurrent("page")                 // aria-current="page" | "step" | "location" | ...
ariaDisabled("true")                // aria-disabled="true"
ariaSelected("true")                // aria-selected="true"
ariaChecked("true")                 // aria-checked="true" | "false" | "mixed"
ariaValuenow("50")                  // aria-valuenow="50"
ariaValuemin("0")                   // aria-valuemin="0"
ariaValuemax("100")                 // aria-valuemax="100"
```

## Data Attributes & Keyed Diffing

### data

Creates custom `data-*` attributes:

```csharp
data("id", "123")                   // data-id="123"
data("action", "delete")            // data-action="delete"
data("user-id", "456")              // data-user-id="456"
```

### key

Creates a `data-key` attribute for explicit keyed DOM diffing. When present, the diff algorithm uses this as the element's key instead of its compile-time ID:

```csharp
key("user-42")                      // data-key="user-42"
```

Useful for dynamic lists where elements can be reordered:

```csharp
ul([], [
    ..model.Items.Select(item =>
        li([key($"item-{item.Id}")], [text(item.Name)])
    )
])
```

## SVG Attributes

### Geometry

```csharp
viewBox("0 0 100 100")
cx("50")                            // Circle center x
cy("50")                            // Circle center y
r("25")                             // Circle radius
rx("10")                            // Ellipse x radius
ry("5")                             // Ellipse y radius
x("10")                             // Rectangle x / text x
y("10")                             // Rectangle y / text y
x1("0")                             // Line start x
y1("0")                             // Line start y
x2("100")                           // Line end x
y2("100")                           // Line end y
dx("5")                             // Text offset x
dy("5")                             // Text offset y
d("M10 10 L90 90")                  // Path data
points("0,0 50,50 100,0")           // Polyline/polygon points
```

### Presentation

```csharp
fill("red")                         // fill="red"
fill("none")                        // fill="none"
stroke("black")                     // stroke="black"
strokeWidth("2")                    // stroke-width="2"
strokeLinecap("round")              // stroke-linecap="round"
strokeLinejoin("miter")             // stroke-linejoin="miter"
strokeDasharray("5,10")             // stroke-dasharray="5,10"
strokeDashoffset("0")               // stroke-dashoffset="0"
strokeOpacity("0.5")                // stroke-opacity="0.5"
fillOpacity("0.8")                  // fill-opacity="0.8"
fillRule("evenodd")                  // fill-rule="evenodd"
clipRule("nonzero")                  // clip-rule="nonzero"
opacity("0.5")                      // opacity="0.5"
transform("rotate(45)")             // transform="rotate(45)"
```

### Text

```csharp
textAnchor("middle")                // text-anchor="middle" | "start" | "end"
dominantBaseline("middle")          // dominant-baseline="middle"
```

### Gradients & Patterns

```csharp
gradientUnits("userSpaceOnUse")     // gradientUnits="userSpaceOnUse"
patternUnits("userSpaceOnUse")      // patternUnits="userSpaceOnUse"
spreadMethod("pad")                 // spreadMethod="pad" | "reflect" | "repeat"
preserveAspectRatio("xMidYMid meet") // preserveAspectRatio="..."
xlinkHref("#icon")                  // xlink:href="#icon"
xmlns("http://www.w3.org/2000/svg") // xmlns="..."
```

### Markers

```csharp
markerStart("url(#arrow)")          // marker-start="url(#arrow)"
markerMid("url(#dot)")              // marker-mid="url(#dot)"
markerEnd("url(#arrow)")            // marker-end="url(#arrow)"
```

## Miscellaneous

```csharp
start("5")                          // start="5" — ordered list start number
color("red")                        // color="red"
cite_("https://source.com")         // cite="..." — underscore avoids C# keyword
datetime("2025-01-01")              // datetime="2025-01-01"
coords("0,0,50,50")                 // coords="0,0,50,50" — for area elements
shape("rect")                       // shape="rect" | "circle" | "poly"
is_("custom-element")               // is="custom-element"
as_("script")                       // as="script" — underscore avoids C# keyword
fetchpriority("high")               // fetchpriority="high" | "low" | "auto"
importance("high")                  // importance="high"
fallback("loading...")              // fallback="loading..."
challenge("challenge-string")       // challenge="..."
```

## C# Keyword Avoidance

Attributes that conflict with C# keywords or contextual keywords use trailing underscores:

| HTML Attribute | C# Function | Reason |
| -------------- | ----------- | ------ |
| `class` | `class_(...)` | C# keyword |
| `for` | `for_(...)` | C# keyword |
| `checked` | `checked_(...)` | C# keyword |
| `readonly` | `readonly_(...)` | C# keyword |
| `async` | `async_(...)` | Contextual keyword |
| `default` | `default_(...)` | C# keyword |
| `open` | `open_(...)` | Contextual keyword |
| `is` | `is_(...)` | C# keyword |
| `as` | `as_(...)` | C# keyword |
| `span` | `span_(...)` | Conflicts with element |
| `cite` | `cite_(...)` | Conflicts with element |

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
        disabled(model.IsSubmitting ? "true" : "false")
    ], [
        text(model.IsSubmitting ? "Logging in..." : "Login")
    ])
])
```

## See Also

- [Elements API](./html-elements.md) — HTML elements
- [Events API](./html-events.md) — Event handlers
- [DOM Types](./dom-types.md) — Attribute type definition
