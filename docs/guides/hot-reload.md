# Hot Reload

Abies supports .NET hot reload, allowing view changes to appear immediately on file save — no restart required. Model state is fully preserved across reloads.

## How it works

Because Abies views are pure functions (`Model → Document`), hot reload is simpler than in component-based frameworks:

1. You save a view file.
2. `dotnet watch` recompiles the changed assembly using .NET's [MetadataUpdate API](https://learn.microsoft.com/en-us/dotnet/standard/assembly/hot-reload).
3. Abies's `AbiesMetadataUpdateHandler` receives the notification.
4. All active `Runtime` instances re-invoke `View(currentModel)` and apply the diff.
5. The UI updates in place — with your current model state intact.

## Running with hot reload

No manual attribute setup is required. In Debug builds, Abies injects the
`MetadataUpdateHandler` registration automatically for consuming app assemblies.

Then run with `dotnet watch`.

### Server mode

```bash
dotnet watch run --project YourApp.Server
```

### WASM mode

```bash
dotnet watch run --project YourApp.Wasm.Host
```

`dotnet watch` enables hot reload automatically for Debug builds. When you save a `.cs` file containing a view function, the change is applied within ~1 second.

## What hot-reloads

| What | Hot-reloads? |
|------|-------------|
| View functions (`Model → Document`) | ✅ Yes |
| HTML helpers and sub-views | ✅ Yes |
| Subscriptions | ❌ No — requires restart |
| `Update`/`Transition` functions | ❌ No — requires restart |
| Commands | ❌ No — requires restart |

Only view-layer changes are live-reloaded. Logic layer changes require a restart (same as Blazor).

## Debug-only

Hot reload is entirely stripped from Release builds:

- `AbiesMetadataUpdateHandler` is compiled only under `#if DEBUG`.
- The auto-generated assembly registration is Debug-only.
- There is zero performance or size impact on published applications.

## How it works internally

Abies injects an assembly-level `MetadataUpdateHandlerAttribute` in Debug builds
for consuming app assemblies:

```csharp
// Auto-generated in the consuming app assembly — Debug builds only
[assembly: MetadataUpdateHandler(typeof(AbiesMetadataUpdateHandler))]

public static class AbiesMetadataUpdateHandler
{
    public static void UpdateApplication(Type[]? updatedTypes) => ...
    public static void ClearCache(Type[]? updatedTypes) => ...
}
```

Each `Runtime<TProgram, TModel, TArg>` instance registers itself with `HotReloadRuntimeRegistry`, keyed by `typeof(TProgram).Assembly`. When types in that assembly are updated, `RefreshViewFromCurrentModel()` is called — which re-renders with the current model and pipes the diff patches to the client.
