# Building Native Apps with Abies

Abies renders to native WinUI 3 controls the same way it renders to the browser DOM: by
interpreting the patch stream that `Operations.Diff` already produces. Your Model, update
logic and subscriptions are shared verbatim; only `View` is per-platform.

- **`Picea.Abies.Native`** — the control vocabulary and a platform-neutral patch
  interpreter. No UI framework dependency, so it builds and tests anywhere.
- **`Picea.Abies.WinUI`** — the WinUI 3 backend and bootstrap. Built with the Uno.Sdk, so
  the same code targets Windows App SDK on Windows and Uno's Skia heads elsewhere.

Background: [ADR-027](../adr/ADR-027-native-winui-renderer.md) (why patch interpretation),
[ADR-028](../adr/ADR-028-program-core-view-split.md) (why `ProgramCore` / `ProgramView`).

## A first view

Native views are ordinary pure functions returning a `Document`, exactly like HTML views:

```csharp
using Picea.Abies.DOM;
using Picea.Abies.Native;
using static Picea.Abies.Native.Elements;
using static Picea.Abies.Native.Events;
using static Picea.Abies.Native.Properties;

public sealed class CounterView : ProgramView<CounterModel>
{
    public static Document View(CounterModel model) =>
        new("Counter",
            StackPanel([Spacing(16), HorizontalAlignment(Alignment.Center)],
            [
                TextBlock([FontSize(28), FontWeight(TextWeight.Bold)], "Counter"),
                StackPanel([Orientation(StackOrientation.Horizontal), Spacing(12)],
                [
                    Button([OnClick(new Decrement()), Width(48)], "−"),
                    TextBlock([FontSize(24), Width(64)], model.Count.ToString()),
                    Button([OnClick(new Increment()), Width(48)], "+"),
                ]),
            ]));
}
```

You need four `using`s: `Picea.Abies.Native` for the value enums, plus `using static` for
`Elements`, `Events` and `Properties`.

## Sharing a program across web and native

A native app supplies a `ProgramView` and pairs it with a core you already have:

```csharp
await Picea.Abies.WinUI.Runtime.RunWithView<CounterProgram, CounterView, CounterModel, Unit>(
    rootHost, window, Unit.Value);
```

`CounterProgram` here is the *same class* the WASM and server hosts use — not a copy or a
wrapper. If you are writing a native-only app, implement `Program<TModel, TArgument>`
directly instead and use `Runtime.Run`.

## The vocabulary

Factories mirror WinUI control and property names, so WinUI knowledge transfers directly.

| Group | Elements |
| --- | --- |
| Layout | `StackPanel`, `Grid`, `Border`, `ScrollViewer`, `ContentControl` |
| Text & media | `TextBlock`, `Image` |
| Input | `Button`, `TextBox`, `CheckBox`, `Slider`, `ToggleSwitch`, `ComboBox` / `ComboBoxItem` |
| Progress | `ProgressRing`, `ProgressBar` |

Events: `OnClick`, `OnTextChanged`, `OnToggled`, `OnValueChanged`, `OnSelectionChanged`.
`Events.On(name, …)` covers anything the backend supports but the vocabulary has not
named yet.

Unknown tags and properties **throw** rather than being ignored — in a renderer, silence
hides bugs.

### Two rules, enforced by analyzers

Native trees are element-only, and text is carried as a property:

```csharp
TextBlock([FontSize(14)], "hello")     // ✅ text is the Text attribute
StackPanel([], [text("hello")])        // ❌ ABIES009
```

A `Text` or `RawHtml` child would make the diff take an HTML fallback path that the native
interpreter rejects at runtime, so **ABIES009** catches it at compile time.

**ABIES008** rejects a custom tag colliding with an HTML void element name (`input`,
`link`, `source`, `base`, `track`, …). The diff skips child diffing for those names, so
such an element would render once and then never update its children — with no error at
all. This only applies if you call `Elements.element(...)` with your own tag; the named
factories are all safe.

### Elements created in loops need an explicit id

Ids are generated per call site, so a factory called in a loop would hand every iteration
the same id. Set `Properties.Id` (and `Key` for keyed reconciliation) yourself:

```csharp
StackPanel([], [.. items.Select(item =>
    TextBlock([Id($"item-{item.Id}"), Key(item.Id)], item.Name))])
```

This is the same rule as the HTML DSL.

## Two-way input

`TextBox` and friends are controlled: the model owns the value.

```csharp
TextBox([Text(model.Query), PlaceholderText("Search…"),
         OnTextChanged(d => new QueryChanged(d!.Text))])
```

The backend skips writes that would not change the text, so ordinary typing never
disturbs the caret. When your update function *transforms* the input — upper-casing,
trimming — the write does happen and the selection is restored around it. A model that
rewrites the whole string will still move the caret; there is no general answer for where
it should land after an arbitrary rewrite.

`Slider.Value` and `ComboBox.SelectedIndex` guard identical writes for the same reason:
re-setting them would re-raise their change events.

## Long lists

A row per item stops being viable somewhere in the low thousands: every row is a real
native control that must be created, measured and arranged. The fix is to let the model
track the scroll position and have the view emit only the rows on screen, so the control
count is bounded by the viewport rather than the data.

`Virtualization.Window` does the arithmetic — which slice is visible, and how much empty
space to leave above and below so the scrollbar still reflects the whole list:

```csharp
public record Model(IReadOnlyList<Row> Rows, double ScrollOffset, double ViewportHeight);
public record Scrolled(double Offset, double Viewport) : Message;

// Transition
Scrolled m => model with { ScrollOffset = m.Offset, ViewportHeight = m.Viewport },

// View
private const double RowHeight = 40;

public static Document View(Model model)
{
    var window = Virtualization.Window(
        itemCount: model.Rows.Count,
        rowHeight: RowHeight,
        scrollOffset: model.ScrollOffset,
        viewportHeight: model.ViewportHeight);

    return new("Big list",
        ScrollViewer([OnScrollChanged(d => new Scrolled(d!.VerticalOffset, d.ViewportHeight))],
            StackPanel([],
            [
                // Empty space standing in for the rows above the viewport.
                Border([Id("pad-top"), Height(window.TopPadding)], TextBlock([Id("pad-top-x")], "")),

                .. model.Rows
                    .Skip(window.StartIndex)
                    .Take(window.Count)
                    .Select((row, i) =>
                    {
                        var index = window.StartIndex + i;
                        return TextBlock([Id($"row-{index}"), Key(row.Id), Height(RowHeight)], row.Label);
                    }),

                Border([Id("pad-bottom"), Height(window.BottomPadding)], TextBlock([Id("pad-bottom-x")], "")),
            ])));
}
```

The two spacers are what keep the scrollbar honest — without them it would size itself to
the handful of rendered rows. Keys still matter: rows entering and leaving the window are
reconciled by key, so scrolling moves controls rather than rebuilding them.

**Keys do most of what recycling would.** Because rows are keyed, scrolling reuses the
overlap: scroll by one row and the diff creates exactly one control and destroys one,
moving the other nineteen. Churn is proportional to rows *scrolled*, not to window size —
there is a test asserting precisely that. A WinUI `ItemsRepeater` would go one step
further and reuse the container itself, saving that single construction, but the large win
— thousands of controls down to a viewport's worth — is already here.

`Properties.VerticalOffset` scrolls the control programmatically, for cases like jumping
to a search result. It skips identical writes so a scroll-driven model does not fight the
user mid-gesture.

## Theming

A literal colour is wrong in one of the two themes. WinUI already ships semantic brushes
that follow light and dark, so the theme story is "use the platform's":

```csharp
TextBlock([Foreground(ThemeColor.TextSecondary)], "Last updated 3m ago")
Border([Background(ThemeColor.SurfaceCard), BorderBrush(ThemeColor.Stroke)], content)
Button([Background(ThemeColor.Critical), OnClick(new Delete())], "Delete")
```

Roles: `TextPrimary`, `TextSecondary`, `TextDisabled`, `Accent`, `SurfaceBase`,
`SurfaceCard`, `Stroke`, `Success`, `Caution`, `Critical`. The string overloads still take
literal colours where you genuinely want one.

A theme key the platform does not define leaves the property at its default and reports
through `renderFaulted` rather than throwing — a missing brush is a styling problem, not a
reason to lose a window.

## Accessibility

Controls with visible text — `Button`, `CheckBox`, `TextBlock` — already expose it to
assistive technology. The ones that need help are those without: an icon-only button, a
`TextBox` with no adjacent label, a `Slider`.

```csharp
Button([AutomationName("Delete item"), AutomationHelpText("Removes this row"),
        OnClick(new Delete(id))], "🗑")

TextBox([AutomationName("Search"), PlaceholderText("Search…"),
         OnTextChanged(d => new QueryChanged(d!.Text))])
```

`AccessibilityHidden(true)` removes a control from the accessibility tree — for decoration
only, never for something the user must reach.

Automation peers work: the TaskTimer sample's CI interaction check drives buttons through
`IInvokeProvider`, which is the same tree a screen reader reads.

> **Not yet done:** there has been no audit against a real screen reader. These properties
> make correct names *possible*; they do not prove any particular view is usable. Treat
> that as open work.

## Handling failures

Rendering and dispatch failures are reported rather than silent. Pass `renderFaulted` to
see them; the default writes to standard error.

```csharp
await Runtime.RunWithView<CounterProgram, CounterView, CounterModel, Unit>(
    rootHost, window, Unit.Value,
    renderFaulted: ex => logger.LogError(ex, "Native render fault"));
```

This covers a patch batch that threw, a batch dropped because the dispatcher queue is
shutting down, and a message dispatch that faulted. Without it the symptom of any of
these is a window that quietly stops updating.

## Testing native programs

Because the interpreter is platform-neutral, the whole loop is testable with no UI
framework at all — implement `INativeBackend<T>` over a plain recording type and drive the
real diff. `Picea.Abies.Native.Tests` does exactly this, including keyed reordering and
event dispatch. `Picea.Abies.Testing` works on native programs unchanged, since it only
needs the core.

## Platform support

`Picea.Abies.WinUI` builds two heads:

| Head | TFM | Runs on |
| --- | --- | --- |
| Windows App SDK (real WinUI 3) | `net10.0-windows10.0.26100` | Windows |
| Uno Skia desktop | `net10.0-desktop` | Windows, macOS, Linux |

The Windows head is only resolved when building **on** Windows, so a Linux or macOS build
sees the desktop head alone. Both are built and render-smoke-tested in CI.

## Known limitations

- Long lists are handled by windowing (`Virtualization.Window`), not container recycling —
  rows are created and destroyed as they scroll rather than reused.
- Accessibility: automation names are supported, but no audit against a real screen
  reader has been done.
- `Document.Head` is a no-op; `Document.Title` maps to the window title.
