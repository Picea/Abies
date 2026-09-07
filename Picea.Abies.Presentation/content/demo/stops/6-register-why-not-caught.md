## What was wrong

Only navigation *before* anything was recorded had a rule (origin re-basing). After `Origin` becomes `Established`, a `UrlChanged` delivered by the browser on a **back or forward press** was enveloped like any other message and took the `record` row. Undo across it restored a model carrying the previous page's `Route` while the browser stayed where the user put it, and the browser's own back/forward stack walked away from the application's history. Raised by the user after close-out; no phase caught it.

The reason no phase caught it is worth more than the bug: `00-scope.md`'s INV-2 said *"application state"* and its falsifier said **model**, so every downstream property quantified over the model alone and was green for this whole class of violation. Track A had already named the shape — `model ⊗ world` — in `01-track-a.md`; the scope had not.
