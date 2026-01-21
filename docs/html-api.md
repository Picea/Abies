# HTML API (Elements, Attributes, Events)

Abies exposes HTML and SVG helpers under `Abies.Html`.

- `Abies.Html.Elements` contains functions like `div`, `button`, `svg`, and more.
- `Abies.Html.Attributes` contains functions like `class_`, `href`, `type`, etc.
- `Abies.Html.Events` contains event handlers like `onclick` and `oninput`.

## Example

```csharp
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

public static Node View(Model model)
    => div([class_("toolbar")], [
        input([
            type("text"),
            value(model.Query),
            oninput(data => new QueryChanged(data?.Value ?? ""))
        ], []),
        button([onclick(new Submit())], [text("Search")])
    ]);
```

## Elements

Elements are pure functions that return a `Node`. Each helper accepts
attributes and children. Some elements are empty (`img`, `input`, `br`) and take
an empty children array.

You can always fall back to `Elements.element("tag", attributes, children)` when
an element helper is missing.

## Attributes

Attributes are strongly-typed helpers that produce `Abies.DOM.Attribute` values.
They are rendered as HTML attributes on the resulting element.

## Events

Event helpers return `Abies.DOM.Handler`, which is a special attribute. The
runtime registers handlers and dispatches messages when events occur.

You can either attach a fixed message:

```csharp
button([onclick(new Increment())], [text("+")]);
```

Or map event data to a message:

```csharp
input([oninput(data => new QueryChanged(data?.Value ?? ""))], []);
```

## Event data

Event helpers can deserialize common payloads:
- `InputEventData` (value)
- `KeyEventData` (key + modifier keys)
- `PointerEventData` (clientX/clientY/button)
- `GenericEventData` (union of common fields)

Use the appropriate event overload to receive a typed payload.
