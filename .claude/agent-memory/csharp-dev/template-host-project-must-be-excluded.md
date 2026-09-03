---
name: template-host-project-must-be-excluded
description: WASM template root projects must Remove the sibling *.Host project from all item groups or the two sets of top-level statements collide
metadata:
  type: feedback
---

The WASM templates ship a sibling `AbiesApp.Host` project inside the template
root, so the root project must exclude it from every item group:

```xml
<Compile Remove="*.Host/**/*.cs" />
<Content Remove="*.Host/**" />
<EmbeddedResource Remove="*.Host/**" />
<None Remove="*.Host/**" />
```

**Why:** Without the `Compile Remove`, a normal `dotnet build` of the generated
WASM project globs in the host's `Program.cs` and fails on colliding top-level
statements — two entry points in one compilation. Learned 2026-03-26 when the
host project was added to serve the AppBundle and map `MapOtlpProxy()`.

**How to apply:** Any time a template gains a nested sibling project, add the
four `Remove` globs before anything else. The failure only appears when someone
builds the *generated* app, which is downstream of the template tests, so it is
easy to ship broken.

Verified 2026-09-02 in
`Picea.Abies.Templates/templates/abies-browser/AbiesApp.csproj` (and the
`abies-browser-empty` equivalent).
