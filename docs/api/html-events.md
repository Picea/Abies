# HTML Events API Reference

The `Abies.Html.Events` static class provides functions for creating event handlers.

## Usage

```csharp
using Abies.Html;
using static Abies.Html.Events;
```

## Event Handler Patterns

### Simple Message

Dispatch a message when the event fires:

```csharp
button([onclick(new ButtonClicked())], [text("Click me")])
```

### Message with Event Data

Access event data using a factory function:

```csharp
input([
    oninput(e => new TextChanged(e?.Value ?? ""))
])
```

## Mouse Events

### onclick

Click event:

```csharp
// Simple
onclick(new ItemClicked())

// With data
onclick(e => new ClickedAt(e?.ClientX ?? 0, e?.ClientY ?? 0))
```

### ondblclick

Double-click event:

```csharp
ondblclick(new DoubleClicked())
```

### onmousedown / onmouseup

Mouse button events:

```csharp
onmousedown(new MousePressed())
onmouseup(new MouseReleased())

// With button info
onmousedown(e => new MouseDown(e?.Button ?? 0))
```

### onmouseenter / onmouseleave

Mouse enter/leave (no bubbling):

```csharp
onmouseenter(new MouseEntered())
onmouseleave(new MouseLeft())
```

### onmouseover / onmouseout

Mouse over/out (bubbles):

```csharp
onmouseover(new MouseOver())
onmouseout(new MouseOut())
```

### onmousemove

Mouse movement:

```csharp
onmousemove(e => new MouseMoved(e?.ClientX ?? 0, e?.ClientY ?? 0))
```

### onwheel

Mouse wheel:

```csharp
onwheel(e => new Scrolled(e?.DeltaY ?? 0))
```

### oncontextmenu

Right-click menu:

```csharp
oncontextmenu(new RightClicked())
```

## Keyboard Events

### onkeydown

Key pressed:

```csharp
// Simple
onkeydown(new KeyPressed())

// With key data
onkeydown(e => new KeyDown(e?.Key ?? ""))
```

### onkeyup

Key released:

```csharp
onkeyup(e => new KeyUp(e?.Key ?? ""))
```

### onkeypress

Key pressed (deprecated, use onkeydown):

```csharp
onkeypress(e => new KeyTyped(e?.Key ?? ""))
```

## Form Events

### oninput

Input value changed (fires on each keystroke):

```csharp
input([
    type("text"),
    oninput(e => new TextChanged(e?.Value ?? ""))
])
```

### onchange

Value changed (fires on blur for text, immediately for select/checkbox):

```csharp
select([
    onchange(e => new SelectionChanged(e?.Value ?? ""))
], [...])

input([
    type("checkbox"),
    onchange(new CheckboxToggled())
])
```

### onsubmit

Form submission:

```csharp
form([onsubmit(new FormSubmitted())], [
    // form fields
    button([type("submit")], [text("Submit")])
])
```

### onreset

Form reset:

```csharp
form([onreset(new FormReset())], [...])
```

### onfocus

Element focused:

```csharp
onfocus(new InputFocused())
```

### onblur

Element lost focus:

```csharp
onblur(new InputBlurred())
onblur(new ValidateField())
```

### oninvalid

Invalid form field:

```csharp
oninvalid(e => new FieldInvalid(e?.Message ?? ""))
```

## Touch Events

### ontouchstart

Touch began:

```csharp
ontouchstart(new TouchStarted())
ontouchstart(e => new TouchStart(e?.ClientX ?? 0, e?.ClientY ?? 0))
```

### ontouchend

Touch ended:

```csharp
ontouchend(new TouchEnded())
```

### ontouchmove

Touch moved:

```csharp
ontouchmove(e => new TouchMove(e?.ClientX ?? 0, e?.ClientY ?? 0))
```

### ontouchcancel

Touch cancelled:

```csharp
ontouchcancel(new TouchCancelled())
```

## Pointer Events

Unified mouse/touch/pen events:

```csharp
onpointerdown(new PointerDown())
onpointerup(new PointerUp())
onpointermove(e => new PointerMove(e?.ClientX ?? 0, e?.ClientY ?? 0))
onpointerenter(new PointerEntered())
onpointerleave(new PointerLeft())
onpointerover(new PointerOver())
onpointerout(new PointerOut())
onpointercancel(new PointerCancelled())
```

## Drag and Drop Events

### ondrag

Dragging:

```csharp
ondrag(new Dragging())
```

### ondragstart

Drag started:

```csharp
ondragstart(new DragStarted())
```

### ondragend

Drag ended:

```csharp
ondragend(new DragEnded())
```

### ondragenter / ondragleave

Drag enter/leave target:

```csharp
ondragenter(new DragEntered())
ondragleave(new DragLeft())
```

### ondragover

Dragging over target:

```csharp
ondragover(new DragOver())
```

### ondrop

Dropped:

```csharp
ondrop(new Dropped())
```

## Scroll Events

### onscroll

Element scrolled:

```csharp
onscroll(new Scrolled())
```

## Media Events

### onplay / onpause

Media play/pause:

```csharp
onplay(new MediaPlaying())
onpause(new MediaPaused())
```

### onended

Media ended:

```csharp
onended(new MediaEnded())
```

### ontimeupdate

Playback position changed:

```csharp
ontimeupdate(e => new TimeUpdated(e?.CurrentTime ?? 0))
```

### onvolumechange

Volume changed:

```csharp
onvolumechange(new VolumeChanged())
```

### onloadeddata / onloadedmetadata

Media loaded:

```csharp
onloadeddata(new MediaDataLoaded())
onloadedmetadata(new MediaMetadataLoaded())
```

### oncanplay / oncanplaythrough

Media ready:

```csharp
oncanplay(new CanPlay())
oncanplaythrough(new CanPlayThrough())
```

### onseeking / onseeked

Seeking:

```csharp
onseeking(new Seeking())
onseeked(new Seeked())
```

### onwaiting

Waiting for data:

```csharp
onwaiting(new MediaWaiting())
```

## Animation Events

### onanimationstart / onanimationend / onanimationiteration

CSS animation events:

```csharp
onanimationstart(new AnimationStarted())
onanimationend(new AnimationEnded())
onanimationiteration(new AnimationIteration())
```

### ontransitionstart / ontransitionend

CSS transition events:

```csharp
ontransitionstart(new TransitionStarted())
ontransitionend(new TransitionEnded())
```

## Loading Events

### onload

Resource loaded:

```csharp
onload(new ImageLoaded())
```

### onerror

Load error:

```csharp
onerror(new LoadFailed())
```

## Clipboard Events

### oncopy / oncut / onpaste

Clipboard operations:

```csharp
oncopy(new Copied())
oncut(new Cut())
onpaste(new Pasted())
```

## Page Lifecycle Events

### onbeforeunload

Before page unload:

```csharp
onbeforeunload(new BeforeUnload())
```

### onvisibilitychange

Page visibility:

```csharp
onvisibilitychange(new VisibilityChanged())
```

## Dialog Events

### onclose

Dialog closed:

```csharp
onclose(new DialogClosed())
```

### oncancel

Dialog cancelled (Escape key):

```csharp
oncancel(new DialogCancelled())
```

### ontoggle

Details/dialog toggled:

```csharp
ontoggle(new Toggled())
```

## Network Events

### ononline / onoffline

Network status:

```csharp
ononline(new BackOnline())
onoffline(new WentOffline())
```

## Custom Events

### on

Create any event handler:

```csharp
// Simple
on("customevent", new CustomEventReceived())

// With data
on<CustomEventData>("customevent", e => new CustomEvent(e?.Detail))
```

## Event Data Types

### PointerEventData

For mouse/pointer events:

```csharp
public record PointerEventData(
    int ClientX,
    int ClientY,
    int ScreenX,
    int ScreenY,
    int Button,
    bool CtrlKey,
    bool ShiftKey,
    bool AltKey,
    bool MetaKey
);
```

### KeyEventData

For keyboard events:

```csharp
public record KeyEventData(
    string Key,
    string Code,
    bool CtrlKey,
    bool ShiftKey,
    bool AltKey,
    bool MetaKey,
    bool Repeat
);
```

### InputEventData

For input/change events:

```csharp
public record InputEventData(
    string Value,
    bool Checked
);
```

### GenericEventData

For events without specific data:

```csharp
public record GenericEventData(
    string Type,
    string TargetId
);
```

## Event Handling Patterns

### Conditional Dispatch

```csharp
onkeydown(e => e?.Key switch
{
    "Enter" => new Submit(),
    "Escape" => new Cancel(),
    _ => null  // Don't dispatch for other keys
})
```

### Combining with Model Data

```csharp
div([], [
    ..model.Items.Select((item, index) =>
        button([
            onclick(new ItemClicked(item.Id, index))
        ], [text(item.Name)])
    )
])
```

### Preventing Default Behavior

Note: Abies handles event prevention at the framework level. For custom behavior, you may need to use JavaScript interop.

## Complete Example

```csharp
form([
    class_("search-form"),
    onsubmit(new SearchSubmitted())
], [
    input([
        type("text"),
        class_("search-input"),
        placeholder("Search articles..."),
        value(model.SearchText),
        oninput(e => new SearchTextChanged(e?.Value ?? "")),
        onkeydown(e => e?.Key == "Escape" ? new ClearSearch() : null),
        onfocus(new SearchFocused()),
        onblur(new SearchBlurred())
    ]),
    button([
        type("submit"),
        class_("search-button"),
        disabled(model.IsSearching ? "true" : null)
    ], [
        text(model.IsSearching ? "Searching..." : "Search")
    ])
])
```

## See Also

- [Elements API](./html-elements.md) — HTML elements
- [Attributes API](./html-attributes.md) — HTML attributes
- [Concepts: MVU Architecture](../concepts/mvu-architecture.md) — Event flow
