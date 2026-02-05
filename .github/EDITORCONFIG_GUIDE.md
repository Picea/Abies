# EditorConfig Enforcement Guide

This document explains how the `.editorconfig` file enforces the coding style rules from our instruction files ([csharp.instructions.md](.github/instructions/csharp.instructions.md) and [ddd.instructions.md](.github/instructions/ddd.instructions.md)).

## Overview

The `.editorconfig` file provides **automated enforcement** of coding standards through:
1. **IDE integration** - Real-time warnings/errors in Visual Studio, VS Code, Rider
2. **CI lint-check** - The `lint-check` job in PR validation runs `dotnet format --verify-no-changes` to block PRs with style violations

## C# Instructions Enforcement

### Latest C# Features

**Instruction**: "Always use the latest version C# features."

**Enforcement**:
- `dotnet_diagnostic.IDE0001.severity = warning` - Enforces name simplifications
- `dotnet_diagnostic.IDE0002.severity = warning` - Enforces member access simplifications  
- `dotnet_diagnostic.IDE0004.severity = warning` - Removes unnecessary casts
- `dotnet_diagnostic.IDE0005.severity = warning` - Removes unnecessary using directives
- `csharp_style_prefer_utf8_string_literals = true:suggestion` - Prefers UTF-8 string literals

### BREAKING CHANGE: No "I" Prefix for Interfaces

**Instruction**: "Never prefix interface names with 'I' (e.g., IUserService)."

**Enforcement**:
```properties
# OLD (removed):
dotnet_naming_rule.interface_should_be_begins_with_i.style = begins_with_i

# NEW:
dotnet_naming_rule.interface_should_be_pascal_case.style = pascal_case
```

**Rationale**: Pure functional programming uses interfaces as **capabilities** (functions that can be called), not OO contracts. The "I" prefix is an OO naming convention we're moving away from.

**Examples**:
```csharp
// ❌ Old OO style
public interface IUserService { ... }

// ✅ New functional style
public interface UserService { ... }
```

### BREAKING CHANGE: No "Async" Suffix

**Instruction**: "Never use the naming convention for async code using the Async suffix"

**Enforcement**: Naming rules do not require "Async" suffix for async methods.

**Rationale**: The `Task<T>` or `ValueTask<T>` return type already indicates async behavior. The suffix is redundant.

**Examples**:
```csharp
// ❌ Old style with redundant suffix
public async Task<User> GetUserAsync(string id) { ... }

// ✅ New style without suffix
public async Task<User> GetUser(string id) { ... }
```

### File-Scoped Namespaces

**Instruction**: "Prefer file-scoped namespace declarations"

**Enforcement**:
```properties
csharp_style_namespace_declarations = file_scoped:warning
```

**Example**:
```csharp
// ✅ Enforced
namespace Abies.Core;

public class MyClass { }
```

### Pattern Matching

**Instruction**: "Use pattern matching and switch expressions by default."

**Enforcement**:
```properties
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion
csharp_style_prefer_switch_expression = true:suggestion
csharp_style_prefer_pattern_matching = true:suggestion
csharp_style_prefer_not_pattern = true:suggestion
```

### Expression-Bodied Members

**Instruction**: "Use expression-bodied members by default."

**Enforcement**:
```properties
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_indexers = true:suggestion
csharp_style_expression_bodied_accessors = true:suggestion
csharp_style_expression_bodied_lambdas = true:suggestion
```

### Nameof Usage

**Instruction**: "Use `nameof` instead of string literals when referring to member names."

**Enforcement**:
```properties
dotnet_diagnostic.IDE0280.severity = warning
```

**Example**:
```csharp
// ❌ Blocked by CI
throw new ArgumentNullException("userId");

// ✅ Passes CI
throw new ArgumentNullException(nameof(userId));
```

### Nullable Reference Types

**Instruction**: "Always use `is null` or `is not null` instead of `== null` or `!= null`."

**Enforcement**:
```properties
# Warn on null safety violations
dotnet_diagnostic.CS8600.severity = warning  # Converting null to non-nullable
dotnet_diagnostic.CS8601.severity = warning  # Possible null assignment
dotnet_diagnostic.CS8602.severity = warning  # Dereference possibly null
dotnet_diagnostic.CS8603.severity = warning  # Possible null return
dotnet_diagnostic.CS8604.severity = warning  # Possible null argument

# Prefer "is null" pattern
dotnet_diagnostic.IDE0041.severity = warning
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:warning
```

**Example**:
```csharp
// ❌ Blocked by CI
if (user == null) { ... }

// ✅ Passes CI
if (user is null) { ... }
```

## DDD/Functional Programming Enforcement

### Immutability

**Instruction**: "Use `record` / `record struct`: Immutable, Equality by value"

**Enforcement**:
```properties
# Readonly enforcement
dotnet_diagnostic.IDE0044.severity = warning  # Add readonly modifier
csharp_style_prefer_readonly_struct = true:warning
csharp_style_prefer_readonly_struct_member = true:warning

# Init-only setters
dotnet_diagnostic.IDE0090.severity = suggestion  # Use 'new(...)' for init
```

### Pure Functions

**Instruction**: "Push side effects to the edge via **capability functions**"

**Enforcement**:
```properties
# Static anonymous functions (avoid closure over mutable state)
csharp_style_prefer_static_anonymous_function = true:warning
```

**Example**:
```csharp
// ❌ Warning: captures mutable state
int count = 0;
var increment = () => count++;

// ✅ No warning: static function
var add = static (int a, int b) => a + b;
```

### Primary Constructors

**Instruction**: Support DDD constrained types (smart constructors)

**Enforcement**:
```properties
csharp_style_prefer_primary_constructors = true:suggestion
```

**Example**:
```csharp
// ✅ Suggested for records and simple classes
public record Email(string Value)
{
    // Validation in constructor
    public Email(string Value) : this(Validate(Value)) { }
    private static string Validate(string email) => ...;
}
```

## Not Enforceable via EditorConfig

Some practices from our instructions **cannot** be automatically enforced and require code review:

1. **Pure functional programming** - "Do NOT use object orientation!" (requires manual review)
2. **Domain ubiquitous language** - "Use **ubiquitous language** names that match the domain" (requires domain knowledge)
3. **Result/Option for errors** - "Make errors explicit using **Result/Option**, not exceptions/null" (requires architectural review)
4. **Comments explaining design** - "Write clear and concise comments for each function" (requires manual review)
5. **Performance optimization** - "Optimize performance in performance critical (hot) paths" (requires profiling)
6. **OpenTelemetry instrumentation** - "Always instrument the code base using OTEL" (requires manual verification)

These practices are checked during PR review by maintainers and GitHub Copilot.

## Formatting Defaults

The `.editorconfig` enforces consistent formatting:

- **Braces**: Allman style (opening brace on new line)
- **Indentation**: 4 spaces (no tabs)
- **Line endings**: LF (Unix-style)
- **Charset**: UTF-8
- **Final newline**: Required
- **Trailing whitespace**: Trimmed

## Validation

### Local Validation

Check your code before committing:

```bash
# Check for violations (no changes)
dotnet format --verify-no-changes --verbosity minimal

# Auto-fix violations (caution: review changes!)
# NOTE: dotnet format can introduce merge conflict markers
# in complex files. Always review changes before committing.
dotnet format

# Check specific project
dotnet format Abies.Conduit/Abies.Conduit.csproj --verify-no-changes
```

### CI Validation

The `lint-check` job in `.github/workflows/pr-validation.yml` runs:

```yaml
- name: Check code formatting
  run: dotnet format --verify-no-changes --verbosity diagnostic
```

If this fails, your PR is blocked until formatting is fixed.

## Common Scenarios

### Scenario 1: Creating a New Interface

```csharp
// ❌ CI will fail - "I" prefix not allowed
public interface IArticleRepository { ... }

// ✅ CI passes - PascalCase without prefix
public interface ArticleRepository { ... }
```

### Scenario 2: Async Method Naming

```csharp
// ❌ Old OO convention (discouraged but not enforced)
public async Task<Article> GetArticleAsync(string slug) { ... }

// ✅ Functional convention (encouraged)
public async Task<Article> GetArticle(string slug) { ... }
```

### Scenario 3: Null Checking

```csharp
// ❌ CI will warn - use "is null"
if (article == null) { ... }
if (article != null) { ... }

// ✅ CI passes
if (article is null) { ... }
if (article is not null) { ... }
```

### Scenario 4: Nameof Usage

```csharp
// ❌ CI will warn
throw new ArgumentNullException("articleSlug");
_logger.LogInfo($"Processing {nameof(articleSlug)}");

// ✅ CI passes
throw new ArgumentNullException(nameof(articleSlug));
_logger.LogInfo($"Processing {nameof(articleSlug)}");
```

### Scenario 5: Immutable Types

```csharp
// ❌ Warning: field can be readonly
private List<string> _tags = new();

// ✅ No warning
private readonly List<string> _tags = new();

// ✅ Better: immutable collection
private readonly IReadOnlyList<string> _tags = new List<string>().AsReadOnly();
```

## References

- [csharp.instructions.md](.github/instructions/csharp.instructions.md) - C# coding guidelines
- [ddd.instructions.md](.github/instructions/ddd.instructions.md) - Functional DDD guidelines
- [EditorConfig documentation](https://editorconfig.org/)
- [.NET code style rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/)
