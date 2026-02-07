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

