---
name: spec-project-layout-abies
description: Abies has no *.Specs project and no [Spec] attribute; specs go in one locked file inside the existing test project, because a new project would need a csproj change
metadata:
  type: project
---

The `spec-by-example` skill's default is a separate `tests/*.Specs/` project. **In Abies that is
usually the wrong call**, and the reason is mechanical rather than stylistic.

**Why:** framework types under test get **internal** constructors (*Make Illegal States
Unrepresentable*), and `Picea.Abies.csproj:24-25` already carries
`<InternalsVisibleTo Include="Picea.Abies.Tests" />` (and one for the benchmarks). A new spec
project needs a new `InternalsVisibleTo` line — a `csproj` change — and design plans in this repo
routinely list `Picea.Abies.csproj` under **unchanged, deliberately**, with the note that touching
it signals the design has slipped. The repo layout is also flat (`Picea.Abies.Tests/` at root, no
`tests/` folder), so a `tests/` project would be the odd one out.

**How to apply:** put the whole spec — acceptance layer *and* the `INV-n` properties — in **one
file** inside the existing test project (e.g. `Picea.Abies.Tests/<Context>/<Feature>Spec.cs`) and
lock that one file. It gives the same clean approval surface ("I approved this file as of `<sha>`")
without a project change. Support files (fixture program, generators, comparers) live in the plan's
already-named files and are **not** locked — so state the generator contract inside the locked spec
text and give every property a named falsifier, or a weakened generator can make a locked property
vacuous without the locked file changing.

**There is no `[Spec]` attribute in this repository** (only in the skill doc). Introducing one is a
new file and should be flagged to the user as an addition to the plan's file table, not slipped in.

Related: [[level-choice-framework-internals]]
