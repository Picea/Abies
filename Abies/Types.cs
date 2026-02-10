// =============================================================================
// Abies Core Types
// =============================================================================
// This file defines the fundamental types for the MVU (Model-View-Update) architecture.
//
// Architecture Decision Records:
// - ADR-001: Model-View-Update Architecture (docs/adr/ADR-001-mvu-architecture.md)
// - ADR-006: Command Pattern for Side Effects (docs/adr/ADR-006-command-pattern.md)
// - ADR-009: Sum Types for State Representation (docs/adr/ADR-009-sum-types.md)
// =============================================================================

using Abies.DOM;

namespace Abies
{
    /// <summary>
    /// Represents a reusable UI element with its own model and update logic.
    /// Elements are composable building blocks for application UIs.
    /// </summary>
    /// <remarks>
    /// See ADR-001: Model-View-Update Architecture
    /// </remarks>
    public interface Element<TModel, in TArgument>
    {
        public static abstract Node View(TModel model);
        public static abstract (TModel model, Command command) Update(Message message, TModel model);
        public static abstract TModel Initialize(TArgument argument);
        public static abstract Subscription Subscriptions(TModel model);
        
    }

    /// <summary>
    /// The main application interface implementing the MVU pattern.
    /// </summary>
    /// <remarks>
    /// This interface defines the complete MVU lifecycle:
    /// - Initialize: Create initial model and commands
    /// - Update: Pure function handling messages and producing new state
    /// - View: Pure function rendering model to virtual DOM
    /// - Subscriptions: Declarative external event sources
    /// - HandleCommand: Side effect execution
    /// 
    /// See ADR-001: Model-View-Update Architecture
    /// See ADR-006: Command Pattern for Side Effects
    /// See ADR-007: Subscription Model for External Events
    /// </remarks>
    public interface Program<TModel, in TArgument> 
    {
        public static abstract (TModel, Command) Initialize(Url url, TArgument argument);
        public static abstract (TModel model, Command command) Update(Message message, TModel model);
        public static abstract Document View(TModel model);
        public static abstract Message OnUrlChanged(Url url);
        public static abstract Message OnLinkClicked(UrlRequest urlRequest);
        public static abstract Subscription Subscriptions(TModel model);
        
        public static abstract Task HandleCommand(Command command, Func<Message, System.ValueTuple> dispatch);
    }

    /// <summary>
    /// Represents a URL navigation request, distinguishing internal and external links.
    /// </summary>
    /// <remarks>
    /// This is a sum type (discriminated union) following ADR-009.
    /// Internal links are handled by the application router.
    /// External links navigate away from the application.
    /// </remarks>
    public interface UrlRequest : Message
    {
        public sealed record Internal(Url Url) : UrlRequest;
        public sealed record External(string Url) : UrlRequest;
    }


    /// <summary>
    /// Marker interface for all messages in the MVU loop.
    /// </summary>
    /// <remarks>
    /// Messages are immutable records describing events that can change state.
    /// The Update function pattern-matches on message types.
    /// 
    /// See ADR-001: Model-View-Update Architecture
    /// See ADR-009: Sum Types for State Representation
    /// </remarks>
    public interface Message;

    /// <summary>
    /// Represents side effects to be executed by the runtime.
    /// </summary>
    /// <remarks>
    /// Commands keep the Update function pure. Instead of performing effects
    /// directly, Update returns a Command describing the intent.
    /// The runtime executes commands and dispatches result messages.
    /// 
    /// See ADR-006: Command Pattern for Side Effects
    /// </remarks>
    public interface Command
    {
        /// <summary>No side effect to perform.</summary>
        public record struct None : Command;
        
        /// <summary>Execute multiple commands in sequence.</summary>
        public record struct Batch(IEnumerable<Command> Commands) : Command;
                
    }

    /// <summary>
    /// Factory methods for creating Command values.
    /// </summary>
    public static class Commands
    {
        /// <summary>Returns a command that does nothing.</summary>
        public static Command.None None = new(); 
        
        /// <summary>Combines multiple commands into a single batch.</summary>
        public static Command.Batch Batch(IEnumerable<Command> commands) => new(commands);
    }

    
}

   

namespace Abies.DOM
{
    

    // Operations class moved to a dedicated file for clarity
}
