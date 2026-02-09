---
applyTo: '**'
---

# Agent Memory

This file contains important reminders and learned preferences for the AI assistant.

## Pull Request Guidelines

**See `.github/instructions/pr.instructions.md` for comprehensive PR guidelines.**

Key reminders:
- Always run `dotnet format --verify-no-changes` before submitting
- Follow the PR template at `.github/pull_request_template.md`
- Use Conventional Commits format for PR titles

## Benchmark Suite

The project has benchmark suites in `Abies.Benchmarks/`:
- `DomDiffingBenchmarks.cs` - Virtual DOM diffing performance
- `RenderingBenchmarks.cs` - HTML rendering performance
- `EventHandlerBenchmarks.cs` - Event handler creation performance

## Performance Optimizations Applied

The following Toub-inspired optimizations have been applied:
1. **Atomic counter for CommandIds** - Replaced `Guid.NewGuid().ToString()` with atomic counter
2. **SearchValues fast-path** - Skip HtmlEncode when no special chars present
3. **FrozenDictionary cache** - Cache event attribute names to avoid interpolation
4. **StringBuilder pooling** - Pool StringBuilders for HTML rendering
5. **Index string cache** - Pre-allocate "__index:{n}" strings for keyed diffing

## Known Performance Trade-offs

### Source-Generated JSON (PR #38)
- **What**: Switched to source-generated JSON serialization for .NET 10 WASM trim-safety
- **Trade-off**: Accepted 10-20% regression in event handler creation benchmarks
- **Why**: Required for .NET 10 WASM compatibility - cannot use reflection-based JSON in trimmed builds
- **Impact**: Event handler creation is still fast enough, and WASM bundle size/startup time improvements outweigh this cost

## Build System Issues & Fixes

### NETSDK1152 - Duplicate abies.js in Publish Output

**Problem**: Projects referencing Abies get duplicate `wwwroot/abies.js` files during `dotnet publish`:
- One from `Abies/wwwroot/abies.js` (via `<Content>` with `CopyToPublishDirectory`)
- One from the consuming project's local copy (via `SyncAbiesJs` target)

**Solution**: Use dual MSBuild target approach in consuming projects:

```xml
<!-- Copy the canonical abies.js before build/publish -->
<Target Name="SyncAbiesJs" BeforeTargets="Build;ComputeFilesToPublish" 
        Inputs="..\Abies\wwwroot\abies.js" 
        Outputs="wwwroot\abies.js">
  <Copy SourceFiles="..\Abies\wwwroot\abies.js" 
        DestinationFiles="wwwroot\abies.js" />
</Target>

<!-- Remove the Abies project's copy from publish to avoid NETSDK1152 -->
<Target Name="RemoveDuplicateAbiesJs" AfterTargets="ComputeFilesToPublish">
  <ItemGroup>
    <!-- Use Identity metadata (full path) to identify and remove Abies project's copy -->
    <ResolvedFileToPublish Remove="@(ResolvedFileToPublish)" 
      Condition="'%(ResolvedFileToPublish.RelativePath)' == 'wwwroot\abies.js' 
                 AND $([System.String]::new('%(ResolvedFileToPublish.Identity)').Contains('\Abies\wwwroot\abies.js'))" />
    <ResolvedFileToPublish Remove="@(ResolvedFileToPublish)" 
      Condition="'%(ResolvedFileToPublish.RelativePath)' == 'wwwroot/abies.js' 
                 AND $([System.String]::new('%(ResolvedFileToPublish.Identity)').Contains('/Abies/wwwroot/abies.js'))" />
  </ItemGroup>
</Target>
```

**Key Insights**:
- Must use `%(Identity)` metadata (full file path) NOT `%(OriginalItemSpec)` (relative path)
- Need both Windows (`\`) and Unix (`/`) path separators for cross-platform compatibility
- `BeforeTargets="Build;ComputeFilesToPublish"` ensures local copy exists before publish resolution
- `AfterTargets="ComputeFilesToPublish"` allows removal BEFORE NETSDK1152 check runs

**Applied to**: Abies.Conduit, Abies.Counter, Abies.Presentation, Abies.SubscriptionsDemo

## js-framework-benchmark (Official Performance Testing)

### Setup

The [js-framework-benchmark](https://github.com/nicknash/js-framework-benchmark) is the standard benchmark for comparing frontend framework performance.

**Clone the fork alongside Abies** (same parent directory):
```bash
# From Abies parent directory
cd ..
git clone https://github.com/nicknash/js-framework-benchmark.git js-framework-benchmark-fork
```

Expected structure:
```
parent-directory/
├── Abies/                          # This repository
└── js-framework-benchmark-fork/    # Benchmark fork
    └── frameworks/keyed/abies/     # Abies framework entry
        ├── src/                    # Source (references Abies project)
        └── bundled-dist/           # Published WASM output
```

### Building Abies for Benchmark

**IMPORTANT**: Always do a clean rebuild when testing code changes!

```bash
cd ../js-framework-benchmark-fork/frameworks/keyed/abies/src

# Clean rebuild
rm -rf bin obj
dotnet publish -c Release

# Copy to bundled-dist
rm -rf ../bundled-dist/*
cp -R bin/Release/net10.0/publish/wwwroot/* ../bundled-dist/
```

### Running the Benchmark

1. **Install dependencies** (first time only):
```bash
cd ../js-framework-benchmark-fork
npm ci
cd webdriver-ts
npm ci
```

2. **Start the benchmark server**:
```bash
cd ../js-framework-benchmark-fork
npm run start
# Server runs on http://localhost:8080
```

3. **Run specific benchmarks** (in a new terminal):
```bash
cd ../js-framework-benchmark-fork/webdriver-ts

# Run swap rows benchmark (05_swap1k)
npm run selenium -- --headless --framework abies-v1.0.151-keyed --benchmark 05_swap1k

# Run all benchmarks for Abies
npm run selenium -- --headless --framework abies-v1.0.151-keyed

# Common benchmarks:
# - 01_run1k: Create 1000 rows
# - 02_replace1k: Replace all 1000 rows
# - 03_update10th1k: Update every 10th row
# - 04_select1k: Select a row
# - 05_swap1k: Swap two rows (LIS optimization target)
# - 06_remove1k: Remove a row
# - 07_create10k: Create 10,000 rows
```

4. **View results**:
```bash
# Results saved to:
ls webdriver-ts/results/abies-v1.0.151-keyed_*.json

# View specific result:
cat webdriver-ts/results/abies-v1.0.151-keyed_05_swap1k.json | jq .
```

### Comparison Frameworks
For reference, compare against:
- `vanillajs-keyed` - Baseline (raw DOM manipulation)
- `blazor-wasm-keyed` - .NET Blazor WASM (similar tech stack)

### Benchmark Results (Feb 2025)

| Benchmark | Abies | Blazor | VanillaJS |
|-----------|-------|--------|-----------|
| 05_swap1k | 406.7ms | 94.4ms | 32.2ms |

**Note**: Abies is ~4.3x slower than Blazor on swap due to O(n) diffing overhead, but the LIS algorithm is optimal (2 DOM ops vs 2000 with naive approach).

