namespace Picea.Abies.Testing;

/// <summary>
/// Origin of a message processed by <see cref="TestHarness{TProgram,TModel,TArgument}"/>.
/// </summary>
public enum TestHarnessMessageSource
{
    /// <summary>
    /// Message came from a direct dispatch through the harness API.
    /// </summary>
    Dispatch = 0,

    /// <summary>
    /// Message came from deterministic replay session execution.
    /// </summary>
    Replay = 1
}

/// <summary>
/// Command lifecycle stage captured by the harness.
/// </summary>
public enum TestHarnessCommandStage
{
    /// <summary>
    /// Command was enqueued and is pending drain.
    /// </summary>
    Enqueued = 0,

    /// <summary>
    /// Command was dequeued for mock execution.
    /// </summary>
    Dequeued = 1
}

/// <summary>
/// Recorded message processed by the harness.
/// </summary>
/// <param name="Sequence">Monotonic sequence number.</param>
/// <param name="Message">The message instance that was processed.</param>
/// <param name="MessageType">The CLR type name of <paramref name="Message"/>.</param>
/// <param name="Source">Whether this message came from dispatch or replay.</param>
public sealed record TestHarnessMessageHistoryEntry(
    int Sequence,
    Message Message,
    string MessageType,
    TestHarnessMessageSource Source);

/// <summary>
/// Recorded Decide output produced for a dispatched message.
/// </summary>
/// <param name="Sequence">Monotonic sequence number.</param>
/// <param name="TriggerMessage">Input message passed to Decide.</param>
/// <param name="OutputMessage">Event or error message emitted by Decide.</param>
/// <param name="IsError">True when <paramref name="OutputMessage"/> came from the error branch.</param>
public sealed record TestHarnessDecisionHistoryEntry(
    int Sequence,
    Message TriggerMessage,
    Message OutputMessage,
    bool IsError);

/// <summary>
/// Recorded command queue event.
/// </summary>
/// <param name="Sequence">Monotonic sequence number.</param>
/// <param name="Command">Command instance that entered this stage.</param>
/// <param name="Stage">Current command stage.</param>
public sealed record TestHarnessCommandHistoryEntry(
    int Sequence,
    Command Command,
    TestHarnessCommandStage Stage);