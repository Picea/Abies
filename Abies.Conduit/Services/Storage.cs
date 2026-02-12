namespace Abies.Conduit.Services;

/// <summary>
/// Storage capability abstraction.
/// In browser context, this uses Interop (localStorage).
/// In test context, this uses an in-memory store.
/// </summary>
public static class Storage
{
    private static StorageProvider? _provider;

    /// <summary>
    /// Configure the storage provider. Call once at startup.
    /// If not configured, defaults to browser interop (which may fail in non-browser environments).
    /// </summary>
    public static void Configure(StorageProvider provider) => _provider = provider;

    /// <summary>
    /// Reset to default (browser) provider. Useful for cleanup after tests.
    /// </summary>
    public static void Reset() => _provider = null;

    public static Task SetAsync(string key, string value) =>
        (_provider ?? BrowserStorageProvider.Instance).SetAsync(key, value);

    public static string? Get(string key) =>
        (_provider ?? BrowserStorageProvider.Instance).Get(key);

    public static Task RemoveAsync(string key) =>
        (_provider ?? BrowserStorageProvider.Instance).RemoveAsync(key);
}

/// <summary>
/// Abstract storage provider interface (using abstract record for functional pattern).
/// </summary>
public abstract record StorageProvider
{
    public abstract Task SetAsync(string key, string value);
    public abstract string? Get(string key);
    public abstract Task RemoveAsync(string key);
}

/// <summary>
/// Browser localStorage provider using Interop.
/// </summary>
public sealed record BrowserStorageProvider : StorageProvider
{
    public static readonly BrowserStorageProvider Instance = new();

    private BrowserStorageProvider() { }

    public override Task SetAsync(string key, string value) =>
        Interop.SetLocalStorage(key, value);

    public override string? Get(string key) =>
        Interop.GetLocalStorage(key);

    public override Task RemoveAsync(string key) =>
        Interop.RemoveLocalStorage(key);
}

/// <summary>
/// In-memory storage provider for testing.
/// </summary>
public sealed record InMemoryStorageProvider : StorageProvider
{
    private readonly Dictionary<string, string> _store = [];

    public override Task SetAsync(string key, string value)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public override string? Get(string key) =>
        _store.TryGetValue(key, out var value) ? value : null;

    public override Task RemoveAsync(string key)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clear all stored values. Useful for test cleanup.
    /// </summary>
    public void Clear() => _store.Clear();
}
