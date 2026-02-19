# ADR-021: Roslyn Analyzers for HTML Validation over Type-Safe DSL

**Status:** Accepted  
**Date:** 2026-02-18  
**Decision Makers:** Maurice Peters  
**Related Issue:** [#86 — feat: Type-safe HTML DSL — make illegal states unrepresentable](https://github.com/Picea/Abies/issues/86)

## Context

The Abies HTML DSL is stringly typed — all elements return `Node`, all attributes return `DOM.Attribute`, and all attribute values are `string`. This means the type system allows many illegal HTML states that compile but produce invalid markup:

```csharp
// ❌ Compiles: <p> cannot contain <div> (phrasing vs flow content)
p([], [div([], [text("nested")])])

// ❌ Compiles: href is only valid on <a>, <area>, <base>, <link>
div([href("/page")], [text("not a link")])

// ❌ Compiles: <img> missing required alt attribute
img([src("/photo.jpg")])
```

We explored two fundamentally different approaches to catching these errors at compile time:

1. **Type-safe HTML DSL** — encode HTML content models and attribute validity into the C# type system itself
2. **Roslyn analyzers** — keep the existing DSL unchanged and add compile-time diagnostics via analyzers

A full prototype of the type-safe DSL was built (~9,300 lines across 90+ files) before the cost-benefit analysis led to its rejection.

## Decision

**Use Roslyn analyzers to validate HTML correctness at compile time, keeping the existing stringly-typed DSL unchanged.**

The analyzer ships bundled inside the Abies NuGet package (`analyzers/dotnet/cs/`) so that all consumers — including template users — get HTML validation automatically with zero configuration.

### Initial diagnostic rules

| ID       | Severity | Rule                                                                         |
| -------- | -------- | ---------------------------------------------------------------------------- |
| ABIES001 | Warning  | `img()` must include `alt()` for accessibility                               |
| ABIES002 | Warning  | Flow content (e.g., `div`) inside phrasing-only parents (e.g., `span`, `p`)  |
| ABIES003 | Info     | `a()` should include `href()`                                                |
| ABIES004 | Info     | `button()` should include `type()`                                           |
| ABIES005 | Info     | `input()` should include `type()`                                            |

### Distribution

| Consumer                       | Mechanism                                         | Configuration needed |
| ------------------------------ | ------------------------------------------------- | -------------------- |
| NuGet / template users         | analyzers/dotnet/cs/ convention in NuGet package  | None — automatic     |
| ProjectReference (in solution) | Explicit ProjectReference to Abies.Analyzers      | One line per project |

## Consequences

### Positive

- **Zero migration cost** — existing Abies applications require no code changes
- **Zero API surface change** — the DSL remains a single `Node` type; no breaking changes
- **Familiar developer experience** — warnings appear in the IDE like any other diagnostic
- **Incrementally extensible** — new rules can be added without touching the core framework
- **Low maintenance burden** — analyzer project is ~500 lines, self-contained, netstandard2.0
- **Automatic for NuGet consumers** — template users get validation out of the box
- **Composability preserved** — `Node` remains a single type, so helper functions, `Select`, spread (`..`), and conditional rendering all work unchanged

### Negative

- **Not exhaustive at the type level** — analyzers can only check patterns they recognise; novel misuse patterns may slip through until a rule is added
- **Heuristic-based** — relies on semantic model analysis of method calls, so indirect or dynamic construction may evade detection
- **Two projects to maintain** — `Abies.Analyzers` (netstandard2.0) and `Abies.Analyzers.Tests` are separate from the main framework
- **ProjectReference consumers need an explicit reference** — MSBuild does not propagate `OutputItemType="Analyzer"` transitively through project references

### Neutral

- Analyzer tests use inline type stubs (`AbiesStubs.cs`) rather than referencing the real `Abies.dll`, avoiding cross-TFM compatibility issues (netstandard2.0 vs net10.0)
- Diagnostic severity levels are configurable via `.editorconfig`, so teams can promote Info rules to Warning or suppress rules as needed

## Alternatives Considered

### Alternative 1: Type-Safe HTML DSL (Rejected)

A full prototype was built that encoded HTML content models into the C# type system using phantom type parameters and constrained generic interfaces:

```csharp
// Prototype approach — elements carry content model constraints
public static Element<FlowContent> div<TContent>(
    Attribute<HtmlDivElement>[] attributes,
    TContent[] children) where TContent : FlowContent;

public static Element<PhrasingContent> span<TContent>(
    Attribute<HtmlSpanElement>[] attributes,
    TContent[] children) where TContent : PhrasingContent;
```

**Why it was rejected:**

1. **Massive API surface explosion** — ~90 new types for content models, element-specific attribute sets, and type constraints. Every HTML element needs its own attribute type, content model type, and constraint hierarchy.

2. **C# type system limitations** — C# lacks union types and higher-kinded types, so encoding "this element accepts phrasing OR flow content" requires awkward workarounds (multiple overloads, marker interfaces, explicit casts). The prototype needed implicit conversion operators and bridge types to remain usable.

3. **Breaks composability** — the single biggest cost. With typed elements, you cannot:
   - Put a `div` and a `span` in the same array (different types)
   - Write generic helper functions that return "any element"
   - Use `Select()` to map over heterogeneous children
   - Use spread (`..`) to flatten mixed content
   - Use ternary expressions for conditional rendering

   Every one of these patterns is common in real Abies applications (Conduit uses them extensively). The workaround — wrapper types, explicit casts, or `.AsNode()` calls everywhere — destroys the ergonomics that make the DSL pleasant to use.

4. **Migration cost** — every existing Abies application would need rewriting. The Conduit app alone would require hundreds of changes across all page modules.

5. **Ongoing maintenance burden** — every new HTML element or attribute requires updating the type hierarchy. The HTML spec evolves; keeping a parallel type system in sync is a permanent cost.

6. **Diminishing returns** — the type system can catch nesting and attribute validity, but cannot enforce semantic rules (e.g., "forms need at least one submit button", "tables need `<th>` for accessibility"). Analyzers can encode arbitrary semantic rules.

### Alternative 2: Source Generator (Considered, deferred)

A source generator could analyze `View` functions and emit compile-time warnings for invalid HTML patterns, similar to what the analyzer does but with access to the full syntax tree at generation time.

**Why deferred:**

- Analyzers are simpler to implement and test
- Source generators have stricter performance requirements (they run on every keystroke)
- The analyzer approach can be migrated to a source generator later if needed
- No clear advantage over analyzers for diagnostic-only use cases

### Alternative 3: Runtime Validation (Rejected)

Validate HTML structure at runtime during rendering or in development mode.

**Why rejected:**

- Errors only surface when the code path executes
- No IDE feedback during development
- Performance cost in production (even if dev-only, adds complexity)
- Fundamentally less useful than compile-time validation

## Implementation

### Project structure

```text
Abies.Analyzers/                      # netstandard2.0, Roslyn 4.8.0
├── DiagnosticDescriptors.cs          # ABIES001–ABIES005 definitions
├── HtmlSpec.cs                       # HTML content model data
├── AnalysisHelpers.cs                # Shared semantic model utilities
├── MissingAttributeAnalyzer.cs       # ABIES001, ABIES003–ABIES005
└── ContentModelAnalyzer.cs           # ABIES002

Abies.Analyzers.Tests/                # net10.0, xUnit
├── AbiesStubs.cs                     # Minimal type stubs for testing
├── MissingAttributeAnalyzerTests.cs  # 12 tests
└── ContentModelAnalyzerTests.cs      # 5 tests
```

### NuGet packaging (in `Abies.csproj`)

```xml
<!-- Run the analyzer on the Abies library itself -->
<ProjectReference Include="..\Abies.Analyzers\Abies.Analyzers.csproj"
                  ReferenceOutputAssembly="false"
                  OutputItemType="Analyzer"
                  PrivateAssets="all" />

<!-- Pack the DLL into the NuGet package for consumers -->
<None Include="...\Abies.Analyzers.dll"
      Pack="true"
      PackagePath="analyzers/dotnet/cs" />
```

### Extending with new rules

To add a new diagnostic:

1. Add a `DiagnosticDescriptor` to `DiagnosticDescriptors.cs`
2. Create an analyzer class (or extend an existing one)
3. Add the rule to `AnalyzerReleases.Unshipped.md`
4. Add tests in `Abies.Analyzers.Tests`

## Related Decisions

- [ADR-001: MVU Architecture](./ADR-001-mvu-architecture.md) — the DSL serves the View function
- [ADR-002: Pure Functional Programming](./ADR-002-pure-functional-programming.md) — composability of `Node` aligns with FP principles
- [ADR-003: Virtual DOM](./ADR-003-virtual-dom.md) — `Node` is the VDOM tree type
- [ADR-014: Compile-Time IDs](./ADR-014-compile-time-ids.md) — precedent for using Roslyn tooling (source generators) in the build

## References

- [GitHub Issue #86 — Type-safe HTML DSL](https://github.com/Picea/Abies/issues/86)
- [PR #90 — Implementation](https://github.com/Picea/Abies/pull/90)
- [Roslyn Analyzer Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [NuGet Analyzer Convention](https://learn.microsoft.com/en-us/nuget/guides/analyzers-conventions)
- [HTML Content Models (MDN)](https://developer.mozilla.org/en-US/docs/Web/HTML/Content_categories)
- [Elm's approach — single `Html msg` type + runtime validation](https://package.elm-lang.org/packages/elm/html/latest/)
