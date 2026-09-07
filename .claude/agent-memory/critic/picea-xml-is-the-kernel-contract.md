---
name: picea-xml-is-the-kernel-contract
description: Where to verify Abies kernel dispatch claims — the Picea NuGet package's shipped XML doc file, which settles which messages reach Transition without passing Decide
metadata:
  type: reference
---

`Picea` is an external NuGet package, so `AutomatonRuntime`, `Decider` and `Automaton` are not
source in this checkout. The package ships full XML documentation:

`~/.nuget/packages/picea/<version>/lib/net10.0/Picea.xml`

Check `Picea.Abies/Picea.Abies.csproj` for the referenced version (1.0.0 as of 2026-09).

**Why this matters to the Critic specifically.** Design plans keep making claims of the form *"a
message that arrives this way is classified as X"*. Whether a message reaches `Program.Transition`
without passing `Program.Decide` is decided by the kernel, not by Abies, and the XML answers it
outright: `AutomatonRuntime.Dispatch` *"dispatches an event through transition, observer, and
interpreter pipelines"* (`:1229-1239`) — `Decide` is not in that list — and `InterpretEffect`
*"interprets an effect and dispatches any produced feedback events"* (`:1240-1247`), i.e. through
that same `Dispatch`. `README.md` adds the bound: feedback depth is capped at 64.

Read together with `Picea.Abies/Runtime.cs`, the enumeration of the bare path is therefore closed:
**interpreter feedback** (`_core.Dispatch` from `InterpretEffect`) and **`Decide` errors**
(`Runtime.cs:460,484` dispatches the `Err` channel's message directly). Everything else — DOM
handler events *and* subscription deliveries alike, since `Runtime.cs:332` assigns
`_handlerRegistry.Dispatch = DispatchFromSubscription` — enters through `Runtime.Dispatch` and is
decided.

**How to apply:** any plan that classifies messages by provenance gets checked against this before
its findings are ranked. The XML establishes *contract*, not accessibility — csc emits entries for
internal members too — so if a plan depends on *calling* something, still require a compile probe.

See [[abies-recurring-risk-patterns]], [[undo-redo-pass-critic-verdict]].
