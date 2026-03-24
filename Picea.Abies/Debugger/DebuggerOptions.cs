#if DEBUG

namespace Picea.Abies.Debugger;

/// <summary>
/// Configuration for the Abies Time Travel Debugger.
/// </summary>
/// <remarks>
/// <para>
/// The debugger is automatically enabled in Debug builds and injects a timeline UI
/// for inspecting MVU events, state transitions, and rendered DOM patches.
/// </para>
/// <para>
/// In Release builds, all debugger code is compiled out (zero bytes/overhead).
/// </para>
/// </remarks>
public record DebuggerOptions
{
    /// <summary>
    /// Enable or disable the debugger UI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <c>true</c> in Debug builds (debugger is active).
    /// In Release builds, this setting is ignored and the debugger is always disabled.
    /// </para>
    /// <para>
    /// Set to <c>false</c> to disable the debugger in Debug builds (e.g., in CI/shared environments
    /// where the UI is unwanted but Debug configuration is necessary for other reasons).
    /// </para>
    /// </remarks>
    public bool Enabled { get; init; } = true;
}

#endif
