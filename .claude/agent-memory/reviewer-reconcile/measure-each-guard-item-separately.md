---
name: measure-each-guard-item-separately
description: A multi-item MSBuild Remove guard hides no-ops and side effects; install a minimal variant and read the item lists instead of reasoning about globs
metadata:
  type: feedback
---

When a `.csproj` adds several `<X Remove="…" />` items in one `ItemGroup` to make a
directory inert, **grade each item separately by measurement**, not the group as a whole.
The group's comment states an intent; individual items routinely fail to serve it.

**Why:** on `presentation-demo` (2026-09-07) a four-item guard for
`Picea.Abies.Presentation/content/**` read as reasonable belt-and-braces. Measured:

| item | effect once `<Compile Remove>` is present |
|---|---|
| `<Compile Remove>` | necessary — without it `error CS0106` + `CS1513`, `Build FAILED` |
| `<Content Remove>` | **no-op** — 0 matching items either way |
| `<EmbeddedResource Remove>` | **no-op** — 0 matching items either way |
| `<None Remove>` | removes **42** items — the only other observable effect |

`None` items are what populate the Solution Explorer / Rider file tree and are *not*
published by the Web/WebAssembly SDK. So the stated intent ("never compiled, embedded, or
published") was fully met by **one** line, and the only item doing extra work was making the
directory invisible in the IDE — for a folder whose whole purpose was to be presented from.
Reasoning about SDK glob ordering would never have surfaced that; two commands did.

**How to apply:** copy the csproj, install the minimal guard, then run

- `dotnet msbuild <proj> -getItem:Compile,Content,None,EmbeddedResource` — count items under
  the guarded path per item type;
- `dotnet msbuild <proj> -t:ComputeFilesToPublish -getItem:ResolvedFileToPublish` — confirm
  nothing leaks into publish;
- `dotnet build` with the guard fully removed — proves whether it is load-bearing at all.

Restore the file and verify with `git diff --numstat` that it is byte-identical before you
write the verdict. Related: [[a-blocker-list-is-not-the-criterion]].
