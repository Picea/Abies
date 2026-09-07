---
name: prior-art-undo-redo
description: Reliable external sources and dead ends for undo/redo design in the Abies MVU kernel — surveyed 2026-09-06 for the `undo-redo` pass
metadata:
  type: reference
---

Prior-art index for undo/redo in an MVU/state-container architecture. Compiled during the
`undo-redo` design pass (2026-09-06).

**Sources that paid off**

- `github.com/omnidan/redux-undo` — self-describes as a *higher order reducer*; state shape
  `{past, present, future, _latestUnfiltered, group, index, limit}`; `filter`, `groupBy`,
  `limit` are the three configuration axes that map onto "what counts as a step / what is
  exempt / how much is retained".
- `github.com/elm-community/undo-redo` — `UndoList` past/present/future; the closest
  structural match to an Abies `Program` wrapper.
- Apple's Undo Manager docs + `blog.mbcharbonneau.com/2006/10/07/automatic-grouping-in-nsundomanager/`
  — `groupsByEvent` defaults true, grouping per run-loop pass. **The single strongest
  citation for "one user event = one undo entry"** and it maps directly onto one
  `Runtime.Dispatch` call.
- `martinfowler.com/eaaDev/EventSourcing.html` — the gateway-checks-replay-mode prescription
  is almost line-for-line what `Runtime.InterpretCommand(replay: true)` already does. Also
  the external-*query* problem, which is the part people forget.
- `raw.githubusercontent.com/ProseMirror/prosemirror-history/master/src/history.ts` — read
  the source, not the docs site: `depth: 100`, `newGroupDelay: 500`. The docs site fetch
  truncates before the history module.
- `doc.qt.io/qt-6/qundostack.html` — `undoLimit` (default 0 = unbounded), `mergeWith`,
  `setClean`. Good for the inverse-command family.
- `docs.yjs.dev/api/undo-manager` — `captureTimeout` 500 ms, `trackedOrigins`. The origin
  filter is the cleanest published statement of "exempt by provenance".
- `dev.to/chromiumdev/-native-undo--redo-for-the-web-3fl3` and `w3c/editing` issues — the
  app-undo-vs-native-text-undo collision, with the mechanism explained.

**Dead ends / low yield**

- Searching for the W3C `UndoManager and DOM Transaction` spec as "abandoned" — it was
  superseded by an Undo API draft that also never shipped. Useful as a *failure-mode*
  citation, useless as a design source.
- `prosemirror.net/docs/ref/` via WebFetch — truncates before prosemirror-history.
- `github.com/ProseMirror/prosemirror-history` landing page via WebFetch — archived redirect,
  no README content returned.

**Structural framing that landed**

Past/present/future is a list zipper (Huet, *The Zipper*, JFP 1997). Snapshot retention cost
over immutable records is the persistent-data-structure cost model (Driscoll/Sarnak/Sleator/
Tarjan 1989): per-entry cost is the changed path, not the whole model — but a retained
snapshot pins the whole graph it references, which is the real long-session hazard.

See [[abies-kernel-constraints]].
