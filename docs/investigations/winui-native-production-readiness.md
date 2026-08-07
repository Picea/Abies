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
aliases at every use site. This is visible in the sample and is the first thing a
user will hit. Rename one side (suggestion: enums move to a `Values` static class,
or become `OrientationValue`) **before** the package ships.

**D2 — `Program` contract is not split into Core + View.** Every native program
must hand-delegate `Initialize`/`Transition`/`Decide`/`IsTerminal`/`Subscriptions`
to a shared class. Fine for a spike, unacceptable as the documented pattern.

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
rewrites `Text` mid-typing. Documented, not solved.

**N5 — Reserved-tag and element-only-tree rules are runtime tripwires only.** The
ADR-021 analyzer that would make them compile-time errors is unwritten.

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

### Phase 1 — Make the name true (~1 week)

Goal: real WinUI 3 on Windows.

- Add `net10.0-windows10.0.26100` to `Picea.Abies.WinUI` and the sample (B2).
- Add a `windows-latest` CI job building both heads. This is the only new required
  platform in CI; keep it a separate job so Linux jobs stay fast.
- Run the sample on Windows; fix whatever the Windows App SDK surfaces (expect
  divergence in `RenderTargetBitmap`, `Colors`, and theme resources).
- Wire the `ABIES_SNAPSHOT` PNG path into CI as a smoke check on both heads.
- Address N3: error boundary around `Apply`, honour `TryEnqueue`'s return, surface
  dispatch failures through the existing `subscriptionFaulted`-style callback.

**Exit:** ADR-027's headline claim is demonstrably true on Windows and verified in CI.

### Phase 2 — API shape freeze (~1 week)

Everything here is breaking, so it must precede packaging.

- **D1**: resolve the `Properties`-vs-enum shadowing. Delete the alias workaround
  from the sample — the sample is the proof the fix worked.
- **D2**: split `Program` into Core + View interfaces; remove native delegation
  boilerplate. This touches the core, so it needs its own ADR and a compatibility
  review against browser/server programs.
- **N5**: ADR-021 Roslyn analyzer for reserved void-element tag names and the
  element-only-tree rule, promoting both runtime tripwires to compile-time errors.
- **N4**: pick and document a controlled-input strategy for `TextBox`.

**Exit:** public API reviewed and frozen; no known breaking changes pending.

### Phase 3 — Breadth and scale (~2–3 weeks)

- Vocabulary: `ItemsView`/`ItemsRepeater`, `NavigationView`, `ContentDialog`,
  `ComboBox`, `ProgressRing`; element-content `Button`/`ContentControl` children.
- Virtualized list recycling (Reactor's element pooling is the benchmark to beat).
- Styling/theming: map onto XAML theme resources rather than reinventing.
- Accessibility/automation-peer pass.
- The deferred TaskTimer sample: interval subscription (threadpool → dispatcher
  marshaling) plus keyed list reordering in a real app, not just the fake backend.
- Benchmarks against `microsoft-ui-reactor` for scroll-heavy scenarios.

### Phase 4 — Ship (~1 week)

- NuGet packages for `Picea.Abies.Native` and `Picea.Abies.WinUI`; add both to
  `release.yml`'s pack list (currently absent).
- An `Abies.Native` metapackage alongside `Abies.Browser` / `Abies.Server`.
- `abies-native` template + a smoke-test matrix entry in `pr-validation.yml`
  (note: this adds a Uno restore to the template job — budget for it).
- Docs: getting-started guide, vocabulary reference, "sharing a program across web
  and native" guide.
- README: add native to the render-mode story.

---

## 4. Recommendation

Phase 0 is done: B1, B3, D3, N1 and N2 are fixed and covered by tests, and the CI
cost of the Uno toolchain is measured and cached rather than assumed.

**Phase 1 is now the critical path.** B2 — no Windows App SDK head — is the only
remaining blocker, and it is the one that decides whether ADR-027's headline claim
is true. Note that `dotnet format` removed the explicit `Microsoft.UI.Xaml` and
`Microsoft.UI.Xaml.Controls` usings from the backend as redundant under the Uno
desktop head's implicit usings; **re-check those when the Windows head is added**,
since its implicit-using set may differ.

Do **not** ship packages before Phase 2: D1 (enum shadowing) and D2 (`Program`
Core/View split) are both breaking, and shipping them post-1.0 costs far more than
the week they take now.

The one thing to be clear-eyed about: until Phase 1, this is an *Uno Skia* renderer
with a WinUI-shaped API, not a WinUI renderer. That distinction should stay visible
in the ADR status and any interim docs.
