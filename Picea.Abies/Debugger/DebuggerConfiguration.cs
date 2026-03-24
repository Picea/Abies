#if DEBUG

namespace Picea.Abies.Debugger;

/// <summary>
/// Global configuration for the Abies debugger.
/// </summary>
/// <remarks>
/// <para>
/// Set this before calling <c>Runtime.Run&lt;&gt;()</c> to configure the debugger:
/// </para>
/// <example>
/// <code>
/// // Disable the debugger
/// DebuggerConfiguration.ConfigureDebugger(new DebuggerOptions { Enabled = false });
/// 
/// // Then start the runtime
/// await Picea.Abies.Browser.Runtime.Run&lt;MyProgram, MyModel, Unit&gt;();
/// </code>
/// </example>
/// <para>
/// In Release builds, the debugger is always disabled and this configuration is ignored.
/// </para>
/// </remarks>
public static class DebuggerConfiguration
{
    private static DebuggerOptions _default = new() { Enabled = true };

    /// <summary>
    /// The current debugger configuration.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>new DebuggerOptions { Enabled = true }</c> (enabled in Debug builds).
    /// </remarks>
    public static DebuggerOptions Default => _default;

    /// <summary>
    /// Configures debugger behavior for subsequent runtime startup.
    /// </summary>
    /// <param name="options">Debugger options to apply. If null, defaults are restored.</param>
    public static void ConfigureDebugger(DebuggerOptions? options)
    {
        _default = options ?? new DebuggerOptions { Enabled = true };
    }
}

#endif
