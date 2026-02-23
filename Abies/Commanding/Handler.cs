// =============================================================================
// Command Handler Infrastructure
// =============================================================================
// Defines the functional command handling pipeline for the Abies framework.
//
// Instead of a monolithic HandleCommand switch statement, commands are handled
// by small, focused handler functions composed together at the application boundary.
// Each handler receives its dependencies as capability functions (delegates),
// enabling easy testing with fake implementations (plain lambdas).
//
// The handler returns Option<Message> instead of imperatively dispatching:
// - Some(message) when the command was handled and produced a result
// - None when the command was not handled (wrong type)
// The Runtime is responsible for dispatching returned messages and handling
// framework-level commands (None, Batch, Navigation).
//
// Architecture Decision Records:
// - ADR-006: Command Pattern for Side Effects (docs/adr/ADR-006-command-pattern.md)
//
// Design references:
// - Elm "Effects as Data" pattern (elm-program-test)
// - Mark Seemann's "Dependency Rejection"
// - Scott Wlaschin's "Dependency Interpretation" (capabilities as function parameters)
// - Scott Wlaschin's "Railway-Oriented Programming" (errors as values)
// =============================================================================

namespace Abies.Commanding;

/// <summary>
/// A function that handles a command and returns an optional result message.
/// </summary>
/// <remarks>
/// <para>
/// Command handlers are the functional alternative to service objects. Each handler
/// closes over the capability functions it needs, making dependencies explicit and testable.
/// </para>
/// <para>
/// Returns <see cref="Some{T}"/> with a message when the command was handled,
/// or <see cref="None{T}"/> when the handler does not handle this command type.
/// The Runtime dispatches returned messages into the MVU loop.
/// </para>
/// <example>
/// <code>
/// Handler loginHandler = Pipeline.For&lt;LoginCommand&gt;(async cmd =>
/// {
///     var result = await login(cmd.Email, cmd.Password);
///     return new Some&lt;Message&gt;(result switch
///     {
///         Ok&lt;User, AuthError&gt;(var user) => new LoginSuccess(user),
///         Error&lt;User, AuthError&gt;(var err) => new LoginError(err),
///         _ => throw new UnreachableException()
///     });
/// });
/// </code>
/// </example>
/// </remarks>
/// <param name="command">The command describing the side effect to execute.</param>
/// <returns>
/// <see cref="Some{T}"/> containing a message when the command was handled,
/// or <see cref="None{T}"/> when the handler does not handle this command type.
/// </returns>
public delegate Task<Option<Message>> Handler(Command command);

/// <summary>
/// Composes multiple <see cref="Handler"/> functions into a single handler.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline is pure routing — it tries each handler in order and returns the
/// first match. Framework-level commands (<see cref="Command.None"/>,
/// <see cref="Command.Batch"/>) and navigation commands are handled by the
/// Runtime, not the pipeline.
/// </para>
/// </remarks>
public static class Pipeline
{
    /// <summary>
    /// A command handler that does nothing. Useful for programs with no side effects.
    /// </summary>
    public static readonly Handler Empty = _ => Task.FromResult<Option<Message>>(new None<Message>());

    /// <summary>
    /// Composes multiple command handlers into a single handler.
    /// Handlers are tried in order; the first handler that returns <see cref="Some{T}"/> wins.
    /// </summary>
    /// <param name="handlers">The handlers to compose.</param>
    /// <returns>A single <see cref="Handler"/> that routes commands to the first matching handler.</returns>
    /// <example>
    /// <code>
    /// var handler = Pipeline.Compose(
    ///     Handlers.LoginHandler(login: AuthService.LoginAsync),
    ///     Handlers.LoadArticlesHandler(loadArticles: ArticleService.GetArticlesAsync)
    /// );
    /// </code>
    /// </example>
    public static Handler Compose(params Handler[] handlers) =>
        async command =>
        {
            foreach (var handler in handlers)
            {
                var result = await handler(command);
                if (result is Some<Message>)
                {
                    return result;
                }
            }

            return new None<Message>();
        };

    /// <summary>
    /// Creates a type-safe handler that only processes commands of type <typeparamref name="TCommand"/>.
    /// All other command types return <see cref="None{T}"/>.
    /// </summary>
    /// <typeparam name="TCommand">The specific command type to handle.</typeparam>
    /// <param name="handle">
    /// The handler function for the matched command type.
    /// Returns <see cref="Option{T}"/> of <see cref="Message"/>:
    /// <see cref="Some{T}"/> when a result message was produced,
    /// <see cref="None{T}"/> when the command was handled but produced no message.
    /// </param>
    /// <returns>A <see cref="Handler"/> that pattern-matches on the command type.</returns>
    /// <example>
    /// <code>
    /// var handler = Pipeline.For&lt;LoginCommand&gt;(async cmd =>
    /// {
    ///     var result = await login(cmd.Email, cmd.Password);
    ///     return new Some&lt;Message&gt;(result switch
    ///     {
    ///         Ok&lt;User, AuthError&gt;(var user) => new LoginSuccess(user),
    ///         _ => new LoginError("Invalid credentials")
    ///     });
    /// });
    /// </code>
    /// </example>
    public static Handler For<TCommand>(Func<TCommand, Task<Option<Message>>> handle) where TCommand : Command =>
        async command => command is TCommand typed
            ? await handle(typed)
            : new None<Message>();
}
