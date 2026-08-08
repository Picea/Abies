# WinUI Native Renderer — State of the Spike & Production-Readiness Plan

> Assessed 2026-08-07 against `origin/spike/winui-native-renderer` @ `094c797`
> (1 commit, +2,893 lines / 33 files, 4 commits behind `main`).
> Companion documents: [ADR-027](../adr/ADR-027-native-winui-renderer.md),
> [research](../research/native-winui-mvu.md).

## 1. State

### What the spike actually is

Three new projects plus a sample, layered so that the UI framework is confined to one of them:

| Project | TFM / SDK | Role |
| --- | --- | --- |
| `Picea.Abies.Native` | `net10.0`, Microsoft.NET.Sdk | Control vocabulary (`Elements`/`Properties`/`Events`) + `PatchInterpreter<T>` over `INativeBackend<T>`. **No UI dependency.** |
| `Picea.Abies.Native.Tests` | `net10.0`, TUnit | 15 tests; `FakeBackend` records every operation. |
| `Picea.Abies.WinUI` | `net10.0-desktop`, Uno.Sdk | `WinUIBackend` (control factory, string→native parsing, event wiring) + `Runtime.Run` bootstrap. |
| `Picea.Abies.Counter.Native` | `net10.0-desktop`, Uno.Sdk | Sample: shared `CounterProgram`, native `View` only. |

The central architectural claim holds up: **the core was not touched.** The only
repo-level changes are the `Uno.Sdk` pin in `global.json` and four solution entries.
The renderer plugs into the existing `public delegate void Apply(IReadOnlyList<Patch>)`
seam, exactly as the browser and server renderers do.

The design decision that makes this work is worth restating: **native trees contain
only `Element` nodes**, with text carried as a `Text`/`Content` *attribute* rather
than a `Text` child. That structurally prevents the diff's HTML fallback paths
(`ReplaceRaw` / `Render.Html`) from ever firing. The interpreter throws on the
Text/Raw patch families as a tripwire, and two tests cover it.

### Verified locally (Linux/Fedora, .NET SDK 10.0.110)

| Check | Result |
| --- | --- |
| `Picea.Abies.Native` build | ✅ 0 warnings, 0 errors |
| `Picea.Abies.Native.Tests` | ✅ **15/15 passed** (506 ms) |
| `Uno.Sdk` restore on Linux | ✅ 44 s cold, no workload install needed |
| `Picea.Abies.WinUI` build | ✅ 0 warnings, 0 errors |
| `Picea.Abies.Counter.Native` build | ✅ 0 warnings, 0 errors (57 s) |
| Vulnerable transitive packages (Uno tree) | ✅ none |
| Untracked TODO/FIXME | ✅ none |

The claims in the research document are accurate. This is an unusually clean spike:
correct layering, real tests that exercise the *actual* `Operations.Diff` rather
than hand-built patch lists, and honest self-reported limitations.

### The gap between "spike" and "production"

**It is a spike in one important sense the name obscures: the WinUI 3 head has
never been built or run.** Only the Uno Skia `net10.0-desktop` head exists. A
project called `Picea.Abies.WinUI` that has never been compiled against Windows
App SDK is the single largest risk in the branch.

---

## 2. Findings

### Blockers

**B1 — Event-handler leak in `WinUIBackend`.**
`_detachers` is a `Dictionary<(FrameworkElement, string), Action>`; entries are
removed only in `DetachEvent`, which `PatchInterpreter` calls only from
`RemoveHandlerFromLiveControl`. The teardown paths — `UnregisterSubtree`
(`RemoveChild`, `ReplaceChild`, `ClearChildren`), `UnregisterChildrenOf`, and
`Reset` (on every `AddRoot`) — drop interpreter bookkeeping but **never detach**.
Every removed control with a handler leaves a permanent entry whose *key* holds a
strong reference to the dead `FrameworkElement`. Any app with a dynamic list leaks
controls and their closures for the process lifetime.
*Fix:* have the interpreter call `DetachEvent` for each handler it unregisters, and
add a `FakeBackend` assertion that detach count matches attach count after teardown.
The existing `RemovingAllChildren_EmptiesParentAndTracking` test asserts only
`ControlCount`, which is why this slipped through.

**B2 — No Windows App SDK / real WinUI 3 head.**
`TargetFrameworks` is `net10.0-desktop` only. Nothing has validated the actual
WinUI 3 target the ADR is named for.

**B3 — `dotnet format` violations fail the required `lint-check` job.**
`Picea.Abies.WinUI/WinUIBackend.cs` has two hard errors — `WHITESPACE` at line 287
and `IMPORTS` (ordering) at line 1 — plus `IDE0005` unnecessary-using warnings in
`PatchInterpreter.cs:19`, `Runtime.cs:13`, `WinUIBackend.cs:14,17`,
`PatchInterpreterTests.cs:10`, `RuntimeIntegrationTests.cs:13`.

**B4 — CI does not know about any of this.**
Every workflow runs a bare `dotnet restore` + `dotnet build --no-restore` at the
repo root, so merging silently adds an Uno.Sdk restore (~1 min cold) and a Uno
desktop-head compile to *every* PR and push job. Separately, `cd.yml` enumerates
test projects explicitly — `Picea.Abies.Native.Tests` would build but **never run**.
Restore does work on `ubuntu-latest` without a workload install (verified), but it
must be made deliberate: SDK pin, NuGet caching, and an explicit test step.

### Design debt to settle before packaging (API-breaking later)

**D1 — `Properties` method names shadow the identically-named enums** under
`using static`, forcing `NativeOrientation` / `NativeFontWeight` / `NativeAlignment`
aliases at every use site.

**Fixed.** The enums are now `StackOrientation` and `TextWeight`, so the factories
(`Orientation(...)`, `FontWeight(...)`) keep their WinUI-faithful names and nothing is
hidden. Only those two ever collided — `Alignment` is fine, because the factories are
`HorizontalAlignment`/`VerticalAlignment`, which is why the sample's `NativeAlignment`
alias was never actually needed. The sample now reads
`Orientation(StackOrientation.Horizontal)` with no aliases at all, which is the proof
the friction is gone.

**D2 — `Program` contract is not split into Core + View.** Every native program
must hand-delegate `Initialize`/`Transition`/`Decide`/`IsTerminal`/`Subscriptions`
to a shared class. Fine for a spike, unacceptable as the documented pattern.

**Fixed** — see [ADR-028](../adr/ADR-028-program-core-view-split.md). `Program` now
composes `ProgramCore` + `ProgramView`, and `WithView<TCore, TView, ...>` does the
forwarding once in the framework. The sample is a view and nothing else; the native
host uses `CounterProgram` directly. Fully backward compatible — 496 tests and a full
solution build pass with no source changes outside the native sample.

**D3 — `INativeBackend.InsertBefore` is dead API.** Declared, implemented twice,
never called — the interpreter always appends on `AddChild` and relies on
subsequent `MoveChild` patches for ordering. Either wire it up or delete it before
the interface is public and frozen.

### Correctness nits

**N1 — Attribute removal does not reset brushes.**
`tb.Foreground = ParseBrushOrNull(value) ?? tb.Foreground` keeps the old brush when
`value` is null, unlike every other property path which clears. Affects
`TextBlock.Foreground`, `CheckBox.Foreground`, `Button.Foreground`/`Background`.

**N2 — `Slider.Value` has no identical-value guard**, though `TextBox.Text` does.
Same echo/jitter class of problem.

**N3 — No error boundary.** `dispatcherQueue.TryEnqueue` return value is ignored
(patches silently dropped on a shut-down queue); an exception inside
`PatchInterpreter.Apply` propagates onto the UI thread and crashes the app;
`_ = dispatch(message)` swallows dispatch failures.

**N4 — Unresolved TextBox controlled-input strategy.** Caret can jump if a patch
rewrites `Text` mid-typing.

**Resolved.** The strategy is deliberately narrow and now documented on
`Properties.Text`: identical writes are skipped, so ordinary typing never touches the
caret; when the model genuinely transforms the input the write happens and the
selection is captured and restored around it, clamped to the new length. It does not
try to map a caret through an arbitrary transformation — a model that rewrites the
whole string still moves the cursor, which is the honest outcome, because there is no
general answer to where the caret belongs after an arbitrary rewrite.

**N5 — Reserved-tag and element-only-tree rules are runtime tripwires only.** The
ADR-021 analyzer that would make them compile-time errors is unwritten.

**Resolved.** `NativeElementAnalyzer` adds two error-severity rules: **ABIES008** for a
native tag colliding with an HTML void element name (the diff would silently skip child
diffing, so the subtree renders once and never updates), and **ABIES009** for a
`text()`/`raw()` node inside a native tree. ABIES009 reports at the offending node
rather than at each enclosing element, so nesting yields exactly one diagnostic, and it
walks out to the nearest enclosing element factory so `text()` in an ordinary HTML tree
is untouched.

### Scope gaps

10 controls; no virtualized lists (`ItemsRepeater` escape hatch); no styling/theming
story; no accessibility/automation-peer pass; no NuGet packaging; no `abies-native`
template; no subscription-driven or keyed-list sample (the "TaskTimer" stretch goal
was not built).

---

## 3. Plan

Sequenced so each phase ends somewhere shippable. Repo conventions apply: PRs under
1,500 changed lines, Conventional Commit titles with a capitalized subject, and
What/Why/Testing sections in the description.

### Phase 0 — Land the spike safely ✅ *done*

Landed as four stacked PRs: **#321** (vocabulary + interpreter, with the detach fix and the
`InsertBefore` removal), **#322** (WinUI backend, Uno pin, brush/slider fixes, NuGet caching),
**#323** (Counter sample), **#324** (this document + ADR-027 + research doc).

Original scope, retained for reference:

#### Phase 0 — Land the spike safely (1 PR chain, ~1 day)

Goal: get the branch onto `main` behind CI, with no regression to existing jobs.
The 2,893-line branch exceeds the PR size limit, so split it:

1. `feat(native): Add platform-neutral native vocabulary and patch interpreter`
   — `Picea.Abies.Native` + `Picea.Abies.Native.Tests` only. No Uno, no
   `global.json` change. Self-contained and CI-safe.
   - Fix B1 (detach on teardown) and add the attach/detach-parity test here.
   - Fix B3 lint issues in these files.
   - Resolve D3 (wire or delete `InsertBefore`).
   - Add `Picea.Abies.Native.Tests` to the `cd.yml` test list.
2. `feat(winui): Add WinUI backend and Uno desktop head`
   — `Picea.Abies.WinUI` + `global.json` pin + solution entries.
   - Fix B3 lint errors in `WinUIBackend.cs`.
   - Fix N1, N2.
   - **CI work (B4):** add NuGet package caching keyed on `global.json`; confirm
     restore time on `ubuntu-latest`; if the added minute per job is unacceptable,
     move the Uno projects behind a solution filter and give them a dedicated job.
3. `feat(samples): Add native Counter sample` — `Picea.Abies.Counter.Native`.
4. `docs(adr): Add ADR-027 native WinUI renderer` — ADR + research doc, status
   flipped from Proposed to Accepted.

**Exit:** `main` builds and tests green; native tests run in CI; no new required-check
failures.

### Phase 1 — Make the name true

**Compilation: done (#326).** `Picea.Abies.WinUI` and the sample now resolve
`net10.0-windows10.0.26100` under an `IsOSPlatform('Windows')` guard, and a
`winui-windows` job on `windows-latest` builds both heads plus the native tests
(~5 min). B2 is closed: the WinUI 3 target has been compiled and is now covered on
every PR.

That job earned its place immediately by catching a real divergence — see
"Tooling divergence" below.

**Rendering: done (#327).** `ABIES_SNAPSHOT` now fails on an empty render target and
on a uniform image, so "the window opened but nothing drew" is a failure rather than a
pass. CI runs it on both heads and uploads the PNG: the Windows App SDK head inside
`winui-windows` (187x134 — WinUI sizes the render target to the content), and the Skia
head under Xvfb in `native-render-smoke` (1024x640, full window). Both produce the
Counter UI — title, −/0/+ row, Reset — from the same `NativeCounterProgram`.

`native-render-smoke` sits outside the required `build` job on purpose: a
GUI-under-virtual-display check is the kind that flakes, and it should not be able to
block merges on its own.

**N3: done (#327).** Three silent failure modes now report. An exception inside a patch
batch is caught rather than killing the UI thread; a batch dropped because `TryEnqueue`
returned false is reported instead of vanishing; and dispatch, which was
fire-and-forget, is observed so a failing update surfaces instead of appearing as a
window that stopped responding. `PatchInterpreter.Faulted` and `Runtime.Run`'s
`renderFaulted` carry these, defaulting to standard error so nothing is silent by
omission. The reporter is itself guarded — in the async observer a throwing handler
would be an unhandled exception on the UI thread.

**Phase 1 exit criterion met:** the sample is shown *running* on real WinUI 3, not
merely compiling.

**Interaction is now covered too** (#337): the TaskTimer sample's `ABIES_INTERACT` mode
drives real native input on both heads and asserts the model round-trips back to the
controls, including interval ticks arriving from a threadpool thread.

#### Tooling divergence (new finding)

`dotnet format` runs against whichever head the tooling loads — on Linux always Uno —
so it can emit code that does not compile for the other head. IDE0002 rewrote
`Panel.BackgroundProperty` to `FrameworkElement.BackgroundProperty`: correct for Uno,
which declares Background on `FrameworkElement`, and nonexistent in WinUI 3, which
declares it on `Panel`/`Border`/`Control`. It did this *before the code was first
committed*, so the defect shipped in #322 and sat undetected until Windows CI existed.

Access through the derived type is valid on both heads and is therefore the portable
spelling; `Picea.Abies.WinUI/.editorconfig` disables IDE0002 to stop the rewrite.
Only Background diverges today — BorderBrush and Foreground agree — but the general
lesson stands: **a Linux-only lint cannot be trusted to keep multi-head code portable,
and the Windows job is the thing that makes it visible.**

### Phase 2 — API shape freeze (~1 week)

Everything here is breaking, so it must precede packaging.

- **D1 ✅ done**: enums renamed to `StackOrientation` / `TextWeight`; all three alias
  workarounds deleted from the sample.
- **D2 ✅ done** (ADR-028): `ProgramCore` + `ProgramView` + `WithView`. Backward
  compatible; browser/server/WASM/Conduit programs unchanged.
- **N5 ✅ done**: `NativeElementAnalyzer` (ABIES008, ABIES009), 11 tests.
- **N4 ✅ done**: caret-preserving controlled-input strategy, documented on
  `Properties.Text`.

**Exit:** public API reviewed and frozen; no known breaking changes pending.
**Reached** — D1, D2, N4 and N5 are all done. The two breaking changes (D1, D2) landed
before packaging, which was the whole point of sequencing the freeze ahead of Phase 4.

### Phase 3 — Breadth and scale

**Vocabulary ✅ done.** Added `ToggleSwitch`, `ComboBox`/`ComboBoxItem`, `ProgressRing`,
`ProgressBar`, `ContentControl`, and element-content `Button`. ComboBox is the one with a
trap in it: it derives from both `ItemsControl` and `ContentControl`, so the backend's
child operations must match `ItemsControl` first or every item lands in the content slot
and only the last one shows — no exception, just a wrong dropdown. `MoveChild` gained the
same branch, so keyed reordering works for items as it does for panels.

**Still open, deliberately.** Each of the remaining items is genuinely multi-week, and two
of them should not be rushed:

- **Virtualized lists — windowing done, recycling open.** `Virtualization.Window` bounds
  the control count by the viewport rather than the data: the model tracks the scroll
  offset, the view emits only visible rows plus spacers that keep the scrollbar
  proportional. That is the difference between a 50,000-row list being impossible and
  being usable, and it needed no change to `INativeBackend` — which matters now that the
  interface is published.

  Container *recycling* (a WinUI `ItemsRepeater` reusing row controls) is still open, and
  it is the part that is a project rather than a patch. The obstacle is not the control:
  it is that the diff produces patches for the whole list while only some rows exist, so
  a patch aimed at an off-screen row would be dropped and the row would be stale when it
  scrolled back in. Doing it correctly means the interpreter keeping a shadow of
  unrealized rows and replaying it on realization — a real design change to the core, and
  one that deserves its own ADR. Reactor's element pooling remains the benchmark.
- **Styling/theming.** This would *add public API*, immediately after Phase 2 froze it.
  Designing a theming surface under time pressure is the fastest way to acquire the kind
  of API debt Phase 2 just paid off. It deserves its own ADR: mapping onto XAML theme
  resources versus a framework-level abstraction is a real decision, not an
  implementation detail.
- **Accessibility / automation-peer pass.** Needs a real audit against screen readers,
  not a plausible-looking API.
- **TaskTimer sample ✅ done.** The verification gap is closed. `Picea.Abies.TaskTimer.Native`
  exercises an interval subscription, subscriptions starting and stopping with the model,
  a keyed list that genuinely re-sorts, and two-way text input. `ABIES_INTERACT` drives it
  through real native input — text written to the actual `TextBox`, buttons invoked
  through their automation peers — and asserts the row appears, the toggle re-renders,
  and elapsed has advanced past zero. Both CI heads run it; both report
  *"text input, click dispatch and 3 interval tick(s) all round-tripped"*.

  It earned its keep immediately: the Windows head exposed a deadlock the Skia head
  tolerated. The script runs on the dispatcher thread, and the snapshot blocked on
  `RenderAsync` via `GetAwaiter().GetResult()` — but `RenderAsync` needs that same thread,
  so it never completed. Same class of finding as the `dotnet format` divergence in Phase
  1: **the two heads are not interchangeable, and only running both catches it.**
- **Benchmarks against `microsoft-ui-reactor`.** Meaningful only once virtualization
  exists; benchmarking a non-virtualized list against a virtualized one measures nothing.

### Phase 4 — Ship

**Packaging: done.** `Picea.Abies.WinUI` gained package metadata and both projects
are packed in `release.yml`.

`Picea.Abies.WinUI` is packed on **`windows-latest`**, not with the rest. Its
`net10.0-windows10.0.26100` TFM is guarded by `IsOSPlatform('Windows')`, so packing on
the Linux release runner would silently produce a package containing only the Uno Skia
head — it would install fine and then fail to resolve for every Windows consumer. The
Windows job packs it, asserts `lib/net10.0-windows10.0.26100` is actually present in
the `.nupkg`, and hands it to the release job as an artifact.

**No `Abies.Native` metapackage.** The existing `Abies` / `Abies.Browser` /
`Abies.Server` metapackages are deprecation shims from the ADR-023 rename, redirecting
consumers of the old names. There is no historic `Abies.Native` to redirect, so a
metapackage would create a deprecated package that never had users. Dropped from the
plan deliberately rather than followed by analogy.

- **Template ✅ done**: `dotnet new abies-native` scaffolds an Uno single-project app
  with a `ProgramCore` + `ProgramView` counter, its own `global.json` pinning
  `Uno.Sdk`, and a `windows10.0.26100` head guarded by `IsOSPlatform`. A smoke-test
  matrix entry scaffolds and builds it, then greps the generated source to confirm the
  core/view split survived scaffolding.

  Two things this surfaced. `Uno.Sdk` sets `IsPackable=false` whenever
  `UnoSingleProject` is on — it assumes single-project means *app* — so
  `Picea.Abies.WinUI` produced **no package at all**, silently, and had to opt back in
  explicitly. And a scaffolded project has none of the repo's global usings, so the
  template ships its own `global using Picea;` for `Unit`. Both were found by building
  a scaffolded app rather than by reading the template.
- **Docs ✅ done**: [native apps guide](../guides/native-apps.md) covering the
  vocabulary, sharing a core across web and native, two-way input, fault handling,
  testing and platform support; linked from the README's render-mode section and the
  docs index.

---

## 3a. Status summary

| Phase | Status |
| --- | --- |
| 0 — Land the spike | ✅ #321–#324 |
| 1 — Make the name true | ✅ #326, #327 (compiles, runs and renders on both heads) |
| 2 — API freeze | ✅ #329–#331 (D1, D2, N4, N5; both breaking changes before packaging) |
| 3 — Breadth and scale | ◐ vocabulary (#332) and TaskTimer (#337) done; virtualization, theming, accessibility, benchmarks open |
| 4 — Ship | ✅ #333–#335 (packages, docs, `dotnet new abies-native`) |

The renderer is usable end-to-end today: `dotnet new abies-native`, `dotnet build`, and a
window with real WinUI controls driven by a program shared with the web host. Every layer
is now verified on both heads in CI — it compiles, it renders, and it responds to real
input including threadpool-originated subscription ticks. What remains is depth — scale,
polish and a real accessibility story — not viability.

## 4. Recommendation

Phase 0 is done: B1, B3, D3, N1 and N2 are fixed and covered by tests, and the CI
cost of the Uno toolchain is measured and cached rather than assumed.

**B2 is closed (#326):** the Windows App SDK head compiles and is covered by CI on
every PR, so ADR-027's headline claim now holds for compilation. The implicit-usings
worry flagged here earlier turned out to be a non-issue — Uno's
`Uno.Implicit.Namespaces.targets` declares `Microsoft.UI.Xaml` and friends with no TFM
guard, so both heads agree. A different tooling divergence did bite, and is documented
under Phase 1.

**Phase 1 is complete (#326, #327).** The Windows App SDK head compiles, runs, and
renders the Counter UI in CI, and the silent-failure paths now report. Phase 2 — the
API freeze — is the critical path, and it is the last chance to make breaking changes
cheaply: D1 (enum shadowing) and D2 (`Program` Core/View split) both alter the public
surface, and shipping them after packaging costs far more than the week they take now.

Do **not** ship packages before Phase 2: D1 (enum shadowing) and D2 (`Program`
Core/View split) are both breaking, and shipping them post-1.0 costs far more than
the week they take now.

The one thing to be clear-eyed about: until Phase 1, this is an *Uno Skia* renderer
with a WinUI-shaped API, not a WinUI renderer. That distinction should stay visible
in the ADR status and any interim docs.
