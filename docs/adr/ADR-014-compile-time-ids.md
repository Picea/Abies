# ADR-014: Compile-Time Unique ID Generation

**Status:** Accepted  
**Date:** 2024-01-15  
**Decision Makers:** Abies Core Team  
**Supersedes:** None  
**Superseded by:** None

## Context

Virtual DOM diffing requires stable, unique identifiers for elements to:

- Track which elements have changed
- Apply patches to the correct DOM nodes
- Maintain event handler mappings
- Preserve state across renders

Traditional approaches:

1. **Runtime GUID generation**: `Guid.NewGuid().ToString()`
2. **Incremental counters**: Global counter incremented per element
3. **Content-based hashing**: Hash of element properties
4. **Developer-specified keys**: Manual `key` props like React

Each has trade-offs around stability, uniqueness, and performance.

## Decision

We use **compile-time unique ID generation** via the Praefixum source generator, which assigns stable IDs based on call-site location.

The `[UniqueId]` attribute on parameters triggers compile-time ID generation:

```csharp
public static Node div(
    DOM.Attribute[] attributes, 
    Node[] children, 
    [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
    => element("div", attributes, children, id);
```

At compile time, each call site gets a unique ID based on:

- File path
- Line number
- Column position
- Call sequence

Example generated code:

```csharp
// Source
var el = div([], [text("Hello")]);

// Compiled (conceptually)
var el = div([], [text("Hello", id: "txt_Main_42_8")], id: "div_Main_42_1");
```

This provides:

- **Stable IDs**: Same call site always produces same ID
- **Unique IDs**: Different call sites produce different IDs
- **Zero runtime cost**: IDs are string constants

## Consequences

### Positive

- **No runtime overhead**: IDs are compile-time constants
- **Stable across renders**: Same code path produces same ID
- **Automatic uniqueness**: No manual key management for most cases
- **Diff efficiency**: Stable IDs enable accurate element matching
- **Debugging**: IDs show file/line for traceability

### Negative

- **Source generator dependency**: Requires Praefixum NuGet package
- **Build complexity**: Source generators affect build time
- **Loop handling**: Dynamic lists still need manual keys
- **Refactoring sensitivity**: Moving code changes IDs

### Neutral

- Works alongside manual `data-key` attributes for lists
- ID format is HTML-safe (letters, numbers, underscores)
- Build-time generation means no reflection cost

## Alternatives Considered

### Alternative 1: Runtime GUID Generation

Generate fresh GUIDs for each element:

```csharp
public static Node div(...) => new Element(Guid.NewGuid().ToString(), ...);
```

- Simple implementation
- Every render creates new IDs
- Diff always sees "new" elements
- Destroys element identity

Rejected because it breaks the fundamental diffing optimization.

### Alternative 2: Manual Keys Only

Require developers to specify keys (React-style):

```csharp
div([key("main-content")], [...]);
```

- Maximum control
- Tedious for static content
- Easy to forget
- Verbose

Rejected as too burdensome for simple cases.

### Alternative 3: Hash-Based IDs

Hash element properties to generate IDs:

```csharp
var id = Hash(tag + string.Join(",", attributes) + ...);
```

- Content-addressable
- Expensive to compute
- Changes with any content change
- Collisions possible

Rejected because it doesn't provide stable identity.

### Alternative 4: Incremental Counter

Use a global counter:

```csharp
private static int _counter;
public static Node div(...) => new Element($"e{++_counter}", ...);
```

- Simple
- Order-dependent
- Changes if render order changes
- Threading issues

Rejected because order dependency breaks diffing.

## Related Decisions

- [ADR-003: Virtual DOM Implementation](./ADR-003-virtual-dom.md)
- [ADR-011: JavaScript Interop Strategy](./ADR-011-javascript-interop.md)

## References

- [Praefixum Source Generator](https://github.com/...)
- [React Reconciliation and Keys](https://reactjs.org/docs/reconciliation.html#keys)
- [C# Source Generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [`Abies/Html/Elements.cs`](../../Abies/Html/Elements.cs) - Usage of UniqueId
