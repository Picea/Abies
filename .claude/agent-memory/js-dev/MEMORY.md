# JS Dev memory

- [debugger.js has one source of truth](debugger-js-has-one-source-of-truth.md) — only the Browser copy is editable; the rest are MSBuild-generated and get overwritten.
- [Debugger has two navigation modes](debugger-has-two-navigation-modes.md) — bridge vs detached; a handler that forgets detached mode silently freezes the UI.
- [OTel CDN API drift needs fallbacks](otel-cdn-api-drift-needs-fallbacks.md) — CDN ESM builds change exports between versions; feature-detect and pin the whole set together.
- [Browser spans need explicit export-on-end](browser-spans-need-explicit-export-on-end.md) — `SimpleSpanProcessor` does not flush in the CDN build; the explicit export is not redundant.
- [WebSocket event envelope is additive](websocket-event-envelope-is-additive.md) — unknown top-level properties are ignored by design; keep new fields optional.
