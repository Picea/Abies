// =============================================================================
// Navigation Commands
// =============================================================================
// Browser navigation is a side effect, so it's expressed as Commands.
//
// Architecture Decision Records:
// - ADR-006: Command Pattern for Side Effects (docs/adr/ADR-006-command-pattern.md)
// - ADR-009: Sum Types for State Representation (docs/adr/ADR-009-sum-types.md)
// =============================================================================

namespace Abies;

/// <summary>
/// Navigation commands for browser history manipulation.
/// </summary>
/// <remarks>
/// Navigation is a side effect and must be expressed as commands.
/// The runtime handles these specially, updating both browser history
/// and dispatching URL change messages.
/// 
/// See ADR-006: Command Pattern for Side Effects
/// </remarks>
public static class Navigation
{
    /// <summary>
    /// Sum type representing all navigation operations.
    /// </summary>
    public interface Command : Abies.Command
    {
        /// <summary>Navigate back in history.</summary>
        record struct Back(int times) : Command;

        /// <summary>Navigate forward in history.</summary>
        record struct Forward(int times) : Command;

        /// <summary>Navigate to a specific history position.</summary>
        record struct Go(int steps) : Command;

        /// <summary>Reload the current page.</summary>
        record struct Reload : Command;

        /// <summary>Navigate to a new URL (full page load).</summary>
        record struct Load(Url Url) : Command;

        /// <summary>Push a new URL onto history (SPA navigation).</summary>
        record struct PushState(Url Url) : Command;

        /// <summary>Replace current URL in history (no new entry).</summary>
        record struct ReplaceState(Url Url) : Command;
    }
}
