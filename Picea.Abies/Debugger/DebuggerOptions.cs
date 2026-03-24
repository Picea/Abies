namespace Picea.Abies.Debugger;

/// <summary>
/// Configuration for the Abies Time Travel Debugger.
/// </summary>
/// <remarks>
/// <para>
/// In Release builds, all debugger code is compiled out (zero bytes/overhead).
/// </para>
/// </remarks>
public record DebuggerOptions
{
    /// <summary>
    /// Enable or disable the debugger UI for Debug builds.
    /// </summary>
    public bool Enabled { get; init; } = true;
}
