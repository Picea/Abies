#if DEBUG
using System.Reflection;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Picea.Abies.AbiesMetadataUpdateHandler))]

namespace Picea.Abies;

internal interface IHotReloadRuntime
{
    void RefreshViewFromCurrentModel();
}

internal static class HotReloadRuntimeRegistry
{
    private static readonly Lock _gate = new();
    private static readonly Dictionary<Assembly, Dictionary<long, IHotReloadRuntime>> _runtimesByAssembly = [];
    private static long _nextId;

    internal static IDisposable Register(Assembly programAssembly, IHotReloadRuntime runtime)
    {
        lock (_gate)
        {
            if (!_runtimesByAssembly.TryGetValue(programAssembly, out var runtimes))
            {
                runtimes = [];
                _runtimesByAssembly[programAssembly] = runtimes;
            }

            var id = Interlocked.Increment(ref _nextId);
            runtimes[id] = runtime;

            return new Registration(programAssembly, id);
        }
    }

    internal static void NotifyUpdatedTypes(Type[]? updatedTypes)
    {
        if (updatedTypes is null || updatedTypes.Length == 0)
            return;

        var updatedAssemblies = updatedTypes
            .Select(static t => t.Assembly)
            .Distinct()
            .ToArray();

        foreach (var updatedAssembly in updatedAssemblies)
            NotifyAssembly(updatedAssembly);
    }

    internal static void NotifyAssembly(Assembly assembly)
    {
        IHotReloadRuntime[] targets;
        lock (_gate)
        {
            if (!_runtimesByAssembly.TryGetValue(assembly, out var runtimes) || runtimes.Count == 0)
                return;

            targets = runtimes.Values.ToArray();
        }

        foreach (var runtime in targets)
            runtime.RefreshViewFromCurrentModel();
    }

    private static void Unregister(Assembly programAssembly, long id)
    {
        lock (_gate)
        {
            if (!_runtimesByAssembly.TryGetValue(programAssembly, out var runtimes))
                return;

            runtimes.Remove(id);
            if (runtimes.Count == 0)
                _runtimesByAssembly.Remove(programAssembly);
        }
    }

    private sealed class Registration(Assembly programAssembly, long id) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            Unregister(programAssembly, id);
        }
    }
}

public static class AbiesMetadataUpdateHandler
{
    public static void UpdateApplication(Type[]? updatedTypes) =>
        HotReloadRuntimeRegistry.NotifyUpdatedTypes(updatedTypes);

    public static void ClearCache(Type[]? updatedTypes) =>
        HotReloadRuntimeRegistry.NotifyUpdatedTypes(updatedTypes);
}
#endif
