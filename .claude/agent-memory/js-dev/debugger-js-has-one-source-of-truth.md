---
name: debugger-js-has-one-source-of-truth
description: Only Picea.Abies.Browser/wwwroot/debugger.js is editable; every other debugger.js in the repo is an MSBuild-generated copy that will be overwritten
metadata:
  type: feedback
---

`Picea.Abies.Browser/wwwroot/debugger.js` is the single editable copy. Every
other `debugger.js` under a source tree is generated:

- `Picea.Abies.Server.Kestrel/wwwroot/_abies/debugger.js` — produced by a `Copy`
  target in `Picea.Abies.Server.Kestrel.csproj`, and explicitly `Remove`d from
  `Content`/`None`/`EmbeddedResource` so it is never treated as source.
- `Picea.Abies.Conduit.Wasm` and `Picea.Abies.Counter.Wasm` copy it into
  `AppBundle/` for Debug builds only.

**Why:** The original squad note said the Kestrel file was a "copy — always keep
identical", implying manual synchronisation. That is now wrong and actively
dangerous: an edit made there is silently discarded on the next build. Editing
the canonical file and rebuilding is the only correct workflow.

**How to apply:** Before editing any `debugger.js`, check the path. If it is not
`Picea.Abies.Browser/wwwroot/debugger.js`, stop and edit the canonical file
instead. The same applies when reviewing a diff — a change to a generated copy
is a red flag, not a thoroughness signal. Note also that `debugger.js` is
excluded from Release builds by `Picea.Abies.Browser.csproj`, which is what
makes the release strip contract hold.

Corrected and verified 2026-09-02 against the csproj `Copy` targets.
