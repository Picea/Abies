---
name: ux-review
description: UX reference — Krug's "Don't Make Me Think", Hick/Miller/Fitts/Jakob laws, the team's five design rules, WCAG 2.2 AA implementation, design system thinking (spacing/typography/color/components/responsive), API and developer experience patterns, error message design rules, and the UX review output format. Use when reviewing user-facing changes, designing component interactions, evaluating accessibility, or shaping API responses.
---

# UX Review Reference

The UX Expert's deep reference. Role and protocol live in `.claude/agents/ux-expert.md`. The squad's principles around docs-with-code, accessibility-as-constraint, and user-facing-changes-need-UX-review live in `.claude/docs/decisions.md`.

The guiding principle: **Don't Make Me Think.** Every interface should be self-evident. When that's not possible, self-explanatory. Instructions are a last resort.

---

## Core Principles

### Krug's Three Laws of Usability

1. **Don't make me think.** If a user has to think about how to use something, the design has failed.
2. **It doesn't matter how many times I have to click, as long as each click is a mindless, unambiguous choice.** Fewer clicks isn't the goal; clearer choices is.
3. **Get rid of half the words on each page, then get rid of half of what's left.** Walls of text are not read.

### Hick's Law

The time to make a decision grows logarithmically with the number of choices. Apply: limit options shown at once. Group, hide, defer.

### Miller's Rule

Working memory holds about 7 ± 2 items. Apply: chunk forms into sections of 5–7 fields. Don't show 30 nav items at once.

### Fitts's Law

The time to move to a target depends on its distance and size. Apply: make important targets large and close to where the cursor/finger already is. Touch targets ≥ 44 × 44 pixels.

### Jakob's Law

Users spend most of their time on **other** sites/apps. They expect yours to work like the others. Apply: don't reinvent navigation, form fields, or common patterns. Match platform conventions.

---

## The Team's Five Design Rules

### 1. Boring Is Good

A boring interface that everyone understands instantly beats a creative one with a learning curve. Save creativity for the one or two moments where it earns the trade-off.

### 2. Clarity Over Cleverness

If a designer or developer is proud of how clever an interaction is, that's a warning sign. Clever interactions are puzzles for the user.

### 3. Default to Convention

Native HTML controls before custom components. Standard browser behaviors before override. Platform conventions before app-specific patterns.

### 4. Errors Are Conversations

A good error message tells the user **what went wrong**, **why**, and **what they can do**. It preserves their work. It doesn't blame them. It doesn't show internals.

### 5. Accessibility Is a Constraint, Not a Feature

You don't add accessibility at the end. It shapes every decision from the start. An interface that doesn't work with a keyboard isn't an "accessibility issue" — it's broken.

---

## WCAG 2.2 AA — The Floor

The minimum bar for any user-facing change. Higher than this is encouraged; lower is not acceptable.

### Perceivable

- **1.1.1 Non-text content (A):** Every meaningful image has an `alt` attribute. Decorative images use `alt=""`.
- **1.3.1 Info and relationships (A):** Use semantic HTML. `<button>`, `<nav>`, `<main>`, `<h1>`–`<h6>`, `<label>` for form fields. Don't use color or position alone to convey information.
- **1.4.3 Contrast (minimum) (AA):** Normal text 4.5:1 against background. Large text 3:1.
- **1.4.10 Reflow (AA):** Content reflows at 320 CSS px without horizontal scroll (except for content like maps and tables that genuinely need it).
- **1.4.11 Non-text contrast (AA):** UI components (buttons, form fields) have 3:1 contrast against adjacent colors.
- **1.4.12 Text spacing (AA):** Layout doesn't break when users override line-height, letter-spacing, word-spacing, or paragraph spacing.

### Operable

- **2.1.1 Keyboard (A):** Every interactive element reachable via Tab, operable via Enter or Space. No mouse-only interactions.
- **2.1.2 No keyboard trap (A):** Focus can always leave any component.
- **2.4.3 Focus order (A):** Tab order matches visual order.
- **2.4.7 Focus visible (AA):** Focused element has a visible focus indicator. Don't disable browser focus rings without replacing them.
- **2.5.5 Target size (AAA but recommended):** Touch targets ≥ 44 × 44 px.

### Understandable

- **3.1.1 Language of page (A):** `<html lang="...">` set correctly.
- **3.2.1 On focus (A):** Focusing an element doesn't cause a context change (navigation, form submission, content swap).
- **3.3.1 Error identification (A):** Errors are programmatically identified and described in text.
- **3.3.2 Labels or instructions (A):** Form fields have visible labels. Placeholders are not labels.
- **3.3.3 Error suggestion (AA):** When an error is detected and suggestions are known, they're provided.

### Robust

- **4.1.2 Name, role, value (A):** Every interactive component has a programmatic name, role, and value (browser handles this for native HTML; ARIA fills the gap for custom components).

---

## Implementation Rules

### HTML Semantics First

```html
<!-- Right -->
<button type="button" onclick="...">Save</button>

<!-- Wrong -->
<div class="button" onclick="...">Save</div>
```

A `<button>` is keyboard-focusable, announces as "button" to assistive tech, and supports Enter/Space activation. A `<div>` does none of those things.

### ARIA: Use Sparingly

The first rule of ARIA is *don't use ARIA*. Native semantics first. ARIA when:
- You're building a component the platform doesn't have (combobox, tablist, treeview).
- You're describing state the DOM can't express alone (`aria-expanded`, `aria-busy`, `aria-live`).
- You're labeling something whose visual label doesn't work in the AT (`aria-label`, `aria-labelledby`).

Don't:
- `role="button"` on a `<div>`. Use `<button>`.
- `aria-required="true"` on `<input required>`. Native `required` already does it.
- `aria-disabled` without `disabled`. They mean different things; understand which you need.

### Keyboard Navigation Patterns

| Component | Keys |
|---|---|
| Button | Enter / Space activates |
| Link | Enter activates |
| Menu | Tab to enter, Arrow Up/Down to navigate, Esc to close, Enter to activate |
| Tabs | Tab to enter, Arrow Left/Right to switch tabs, content tab-focusable |
| Dialog (modal) | Focus trapped inside, Esc closes, focus returns to trigger on close |
| Combobox | Type to filter, Arrow Up/Down to navigate options, Enter to select |

### Focus Management

- After a route change, set focus to the new page's `<h1>` or main landmark.
- After opening a dialog, focus the dialog's first focusable element (or its container).
- After closing a dialog, return focus to the element that opened it.
- After a destructive action with undo, focus the undo button.

### Reduced Motion

```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
}
```

Test with the OS setting toggled. If the app feels broken without animations, the animations were carrying meaning that should have been carried by the layout.

---

## Design System Thinking

### Spacing

Use a consistent spacing scale (4px or 8px base). Don't pick arbitrary values like 13px or 27px. Tailwind's default scale, Material's 4dp grid, or any other consistent system works — pick one and stick with it.

### Typography

- One font family for body, optionally one for headings.
- A type scale (e.g., 12 / 14 / 16 / 20 / 24 / 32 / 48). Don't use 17px for one heading and 18px for another.
- Line-height ≥ 1.5 for body text (WCAG-friendly).
- Max line length 60–80 characters for readability.

### Color

- A semantic palette, not a name palette. `color.danger`, `color.success`, `color.surface.elevated` — not `red`, `green`, `light-gray`.
- Defined in CSS custom properties so theming and dark mode are trivial.
- All pairings tested for contrast at design time, not after.

### Components

- A small kit beats a large one. Build the 10 components used 95% of the time, not the 100 used 5%.
- Each component has documented states: default, hover, focus, active, disabled, loading, error, empty, success.
- Variants are constrained: a button has Primary, Secondary, Tertiary, Danger — not 17 ad-hoc styles.

### Responsive Design

- Mobile-first CSS: defaults are mobile, breakpoints scale up.
- Test at 320px, 768px, 1024px, 1440px minimum.
- Touch targets remain ≥ 44 × 44px on touch devices.
- No horizontal scroll on mobile unless the content genuinely demands it (data table, map).

---

## API & Developer Experience

The squad's APIs are products too. UX applies.

### Endpoint Design

- **Resource-shaped URLs.** `/articles/{id}/publish` not `/api/publishArticle?id=...`.
- **Predictable verbs.** GET reads, POST creates / acts, PUT replaces, PATCH partial-updates, DELETE removes.
- **Consistent naming.** If one endpoint uses `articleId`, all of them do — not `article_id` or `id` in another.

### Response Shape

```json
// Success
{
  "id": "abc-123",
  "title": "...",
  ...
}

// Error
{
  "error": {
    "code": "already_published",
    "message": "This article is already published.",
    "details": {
      "articleId": "abc-123",
      "publishedAt": "2026-01-10T..."
    }
  }
}
```

- The error `code` is **stable**, machine-readable, and lowercase-snake. Clients branch on it.
- The `message` is human-readable. Clients can fall back to it if they don't recognize the code.
- The `details` object provides context. Optional but useful.

### Status Codes

- `200 OK` — success with body
- `201 Created` — resource created; `Location` header points at it
- `204 No Content` — success, no body (rare; prefer `200` with a meaningful body)
- `400 Bad Request` — validation failure (the request was malformed)
- `401 Unauthorized` — missing or invalid auth (the client may not be authenticated)
- `403 Forbidden` — auth recognized but action not permitted
- `404 Not Found` — resource doesn't exist
- `409 Conflict` — request conflicts with current state (already published, version mismatch)
- `422 Unprocessable Entity` — well-formed but semantically wrong (rarely needed; `400` usually suffices)
- `429 Too Many Requests` — rate limited; `Retry-After` header
- `500 Internal Server Error` — server bug. Never include details.

### Error Message Design (the most-violated rule)

A good error message:
1. **Tells the user what happened.** "We couldn't save your article."
2. **Explains why, in plain language.** "The title is empty."
3. **Tells them what to do.** "Add a title and try again."
4. **Preserves their work.** Their input is still there.
5. **Doesn't blame them.** "We couldn't save..." not "You did something wrong..."
6. **Doesn't show system internals.** No stack traces, no SQL, no `NullReferenceException`.

A bad error message: `Error: Object reference not set to an instance of an object.`

A good one: `We couldn't save your article. The title field is empty — please fill it in and try again.`

---

## UX Review Output Format

When you review a user-facing change, produce:

```markdown
## 🎨 UX REVIEW — [feature/change]

### Summary

**Overall:** ✅ Approved | ⚠️ Needs Changes | 🔴 Blocking Issues

[2-3 sentences]

### Don't Make Me Think Test

**Can a first-time user accomplish the goal without thinking?**
- [Where it works]
- [Where it doesn't, with proposed fix]

### Cognitive Load

- Hick's law (choice count): [verdict]
- Miller's rule (chunking): [verdict]
- Fitts's law (target size & proximity): [verdict]

### Accessibility (WCAG 2.2 AA)

- Keyboard navigation: [verdict, with specific issues if any]
- Screen reader experience: [verdict]
- Color contrast: [verdict, ratios cited]
- Focus management: [verdict]
- Reduced motion: [verdict]

### Error Handling

- Error messages: [verdict, with examples of what's wrong and rewrites]
- Empty states: [verdict]
- Loading states: [verdict]
- Network failure: [verdict]

### Mobile / Responsive

- Layout at 320px / 768px / 1024px: [verdict]
- Touch targets: [verdict]
- No horizontal scroll: [verdict]

### Findings

#### 🔴 Must Fix
- [File or component] — [issue]. [Suggested fix].

#### ⚠️ Should Fix
- [File or component] — [issue]. [Suggested fix].

#### 💡 Suggestions
- [Observation]

### What's Good

[Things done well]
```

---

## What This Skill Does NOT Cover

- **The decision of when to involve the UX expert** — Tech Writer charter describes when (every user-facing change). This skill describes how to evaluate.
- **Component implementation code** — `js-dev` (browser) or `csharp-dev` (UI library). The UX expert specifies behavior; specialists implement.
- **API endpoint authorization patterns** — `security-toolchain`. UX shapes the response; security shapes who can call.
