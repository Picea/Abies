using System.Text.Json;

namespace Picea.Abies.Testing;

/// <summary>
/// In-memory harness for testing Program workflows deterministically without a browser runtime.
/// </summary>
public sealed class TestHarness<TProgram, TModel, TArgument>
    where TProgram : Program<TModel, TArgument>
{
    private const int MaxBatchDepth = 4_096;
    private readonly Queue<Command> _pendingCommands = new();
    private readonly Dictionary<Type, Func<Command, IReadOnlyList<Message>>> _commandMocks = new();
    private readonly List<Type> _mockRegistrationOrder = [];
    private readonly List<TestHarnessMessageHistoryEntry> _messageHistory = [];
    private readonly List<TestHarnessDecisionHistoryEntry> _decisionHistory = [];
    private readonly List<TestHarnessCommandHistoryEntry> _commandHistory = [];
    private readonly List<TestHarnessReplayMetadataV1> _replayMetadataHistory = [];
    private readonly TestHarnessOptions _options;
    private int _transitionCount;
    private int _messageSequence;
    private int _decisionSequence;
    private int _commandSequence;

    private TestHarness(TModel initialModel, TestHarnessOptions options)
    {
        Model = initialModel;
        _options = options;
    }

    /// <summary>
    /// Current model after all dispatched messages and drained commands.
    /// </summary>
    public TModel Model { get; private set; }

    /// <summary>
    /// Commands that have been produced but not yet drained.
    /// </summary>
    public IReadOnlyList<Command> PendingCommands => _pendingCommands.ToArray();

    /// <summary>
    /// Number of queued commands waiting to be drained.
    /// </summary>
    public int PendingCommandCount => _pendingCommands.Count;

    /// <summary>
    /// Ordered history of messages processed by the harness.
    /// </summary>
    public IReadOnlyList<TestHarnessMessageHistoryEntry> MessageHistory => _messageHistory.AsReadOnly();

    /// <summary>
    /// Ordered history of Decide outputs produced for processed messages.
    /// </summary>
    public IReadOnlyList<TestHarnessDecisionHistoryEntry> DecisionHistory => _decisionHistory.AsReadOnly();

    /// <summary>
    /// Ordered history of command queue stages.
    /// </summary>
    public IReadOnlyList<TestHarnessCommandHistoryEntry> CommandHistory => _commandHistory.AsReadOnly();

    /// <summary>
    /// Ordered metadata for replay sessions executed by this harness.
    /// </summary>
    public IReadOnlyList<TestHarnessReplayMetadataV1> ReplayMetadataHistory => _replayMetadataHistory.AsReadOnly();

    /// <summary>
    /// Creates a harness from the program's initial state and initial command.
    /// </summary>
    public static TestHarness<TProgram, TModel, TArgument> Create(
        TArgument argument,
        TestHarnessOptions? options = null)
    {
        var resolvedOptions = options ?? new TestHarnessOptions();
        resolvedOptions.Validate();

        var (model, initialCommand) = TProgram.Initialize(argument);
        var harness = new TestHarness<TProgram, TModel, TArgument>(model, resolvedOptions);
        harness.EnqueueCommand(initialCommand);

        return harness;
    }

    /// <summary>
    /// Loads and validates a replay session payload using the deterministic v1 schema.
    /// </summary>
    /// <param name="sessionJson">The session JSON payload.</param>
    /// <returns>The validated replay session.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sessionJson"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the payload is invalid or unsupported.</exception>
    public static TestHarnessReplaySessionV1 LoadReplaySession(string sessionJson)
    {
        if (string.IsNullOrWhiteSpace(sessionJson))
        {
            throw new ArgumentException("Session payload cannot be empty.", nameof(sessionJson));
        }

        TestHarnessReplaySessionV1? session;
        try
        {
            session = JsonSerializer.Deserialize(sessionJson, TestHarnessReplayJsonContext.Default.TestHarnessReplaySessionV1);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Session payload is invalid JSON.", exception);
        }

        if (session is null)
        {
            throw new InvalidOperationException("Session payload is invalid: the root document is missing.");
        }

        session.Validate();
        return session;
    }

    /// <summary>
    /// Serializes a replay session using the harness source-generated JSON context.
    /// </summary>
    /// <param name="session">The replay session to serialize.</param>
    /// <returns>Serialized JSON payload.</returns>
    public static string SerializeReplaySession(TestHarnessReplaySessionV1 session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return JsonSerializer.Serialize(session, TestHarnessReplayJsonContext.Default.TestHarnessReplaySessionV1);
    }

    /// <summary>
    /// Replays a validated session by mapping each entry to a domain message and dispatching it in sequence order.
    /// </summary>
    /// <param name="session">The replay session to execute.</param>
    /// <param name="mapEntryToMessage">Maps one replay entry into the program-specific message.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when entry mapping fails.</exception>
    public void ReplaySession(
        TestHarnessReplaySessionV1 session,
        Func<TestHarnessReplayEntryV1, Message> mapEntryToMessage)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(mapEntryToMessage);

        session.Validate();
        _replayMetadataHistory.Add(session.Metadata);

        for (var index = 0; index < session.Entries.Count; index++)
        {
            var entry = session.Entries[index];
            var message = mapEntryToMessage(entry) ?? throw new InvalidOperationException(
                $"Session entry at index {index} ({entry.MessageType}) was mapped to null.");

            DispatchCore(message, TestHarnessMessageSource.Replay);
        }
    }

    /// <summary>
    /// Loads and replays a session payload in one operation.
    /// </summary>
    /// <param name="sessionJson">The session JSON payload.</param>
    /// <param name="mapEntryToMessage">Maps one replay entry into the program-specific message.</param>
    public void LoadAndReplaySession(
        string sessionJson,
        Func<TestHarnessReplayEntryV1, Message> mapEntryToMessage)
    {
        var session = LoadReplaySession(sessionJson);
        ReplaySession(session, mapEntryToMessage);
    }

    /// <summary>
    /// Exports recorded message history to replay schema v1 with deterministic entry ordering.
    /// </summary>
    /// <param name="mapEntryPayload">Maps one history entry into a replay payload object.</param>
    /// <param name="mapEntryMessageType">Optional message type mapper. Defaults to the recorded CLR type name.</param>
    /// <param name="metadata">Optional replay metadata. Supply explicit values for stable golden files; default metadata includes runtime-generated values.</param>
    /// <param name="sourceFilter">Optional source filter when exporting only dispatch or replay-origin messages.</param>
    /// <returns>A validated replay session payload with entries exported in deterministic sequence order.</returns>
    public TestHarnessReplaySessionV1 ExportReplaySession(
        Func<TestHarnessMessageHistoryEntry, JsonElement> mapEntryPayload,
        Func<TestHarnessMessageHistoryEntry, string>? mapEntryMessageType = null,
        TestHarnessReplayMetadataV1? metadata = null,
        TestHarnessMessageSource? sourceFilter = null)
    {
        ArgumentNullException.ThrowIfNull(mapEntryPayload);

        var filter = sourceFilter;
        var selectedEntries = filter is null
            ? _messageHistory
            : _messageHistory.Where(entry => entry.Source == filter.Value).ToList();

        var replayEntries = selectedEntries
            .Select((entry, index) => new TestHarnessReplayEntryV1
            {
                Sequence = index,
                MessageType = mapEntryMessageType?.Invoke(entry) ?? entry.MessageType,
                Payload = mapEntryPayload(entry)
            })
            .ToArray();

        var session = new TestHarnessReplaySessionV1
        {
            SchemaVersion = TestHarnessReplaySchema.Version1,
            Metadata = metadata ?? CreateDefaultReplayMetadata(),
            Entries = replayEntries
        };

        session.Validate();
        return session;
    }

    /// <summary>
    /// Exports message history to deterministic replay schema v1 JSON.
    /// </summary>
    /// <param name="mapEntryPayload">Maps one history entry into a replay payload object.</param>
    /// <param name="mapEntryMessageType">Optional message type mapper. Defaults to the recorded CLR type name.</param>
    /// <param name="metadata">Optional replay metadata. Defaults to harness-generated metadata.</param>
    /// <param name="sourceFilter">Optional source filter when exporting only dispatch or replay-origin messages.</param>
    /// <returns>Serialized replay session JSON.</returns>
    public string ExportReplaySessionJson(
        Func<TestHarnessMessageHistoryEntry, JsonElement> mapEntryPayload,
        Func<TestHarnessMessageHistoryEntry, string>? mapEntryMessageType = null,
        TestHarnessReplayMetadataV1? metadata = null,
        TestHarnessMessageSource? sourceFilter = null)
    {
        var session = ExportReplaySession(mapEntryPayload, mapEntryMessageType, metadata, sourceFilter);
        return SerializeReplaySession(session);
    }

    /// <summary>
    /// Registers a typed command mock that returns one or more messages for the command.
    /// </summary>
    public void MockCommand<TCommand>(Func<TCommand, IReadOnlyList<Message>> mock)
        where TCommand : Command
    {
        ArgumentNullException.ThrowIfNull(mock);

        if (_mockRegistrationOrder.Contains(typeof(TCommand)) is false)
        {
            _mockRegistrationOrder.Add(typeof(TCommand));
        }

        _commandMocks[typeof(TCommand)] = command =>
        {
            var typedCommand = (TCommand)command;
            var messages = mock(typedCommand);

            if (messages is null)
            {
                throw new InvalidOperationException(
                    $"Mock for command '{typeof(TCommand).Name}' returned null. Command mocks must return a non-null message list.");
            }

            var materialized = messages.ToArray();
            if (materialized.Any(static message => message is null))
            {
                throw new InvalidOperationException(
                    $"Mock for command '{typeof(TCommand).Name}' returned a message list containing null entries.");
            }

            return materialized;
        };
    }

    /// <summary>
    /// Registers a typed command mock that returns one message for the command.
    /// </summary>
    public void MockCommand<TCommand>(Func<TCommand, Message> mock)
        where TCommand : Command
    {
        ArgumentNullException.ThrowIfNull(mock);
        MockCommand<TCommand>(command => [mock(command)]);
    }

    /// <summary>
    /// Dispatches one message through Decide and Transition.
    /// </summary>
    public void Dispatch(Message message) =>
        DispatchMany([message]);

    /// <summary>
    /// Dispatches messages in order through Decide and Transition.
    /// </summary>
    public void DispatchMany(IEnumerable<Message> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        foreach (var message in messages)
        {
            DispatchCore(message, TestHarnessMessageSource.Dispatch);
        }
    }

    /// <summary>
    /// Drains the command queue by invoking registered mocks and dispatching emitted messages.
    /// </summary>
    public void DrainCommands()
    {
        var iterations = 0;

        while (_pendingCommands.Count > 0)
        {
            iterations++;
            if (iterations > _options.MaxDrainIterations)
            {
                throw new InvalidOperationException(
                    $"DrainCommands exceeded {_options.MaxDrainIterations} iterations. " +
                    "The command graph is likely non-terminating.");
            }

            var command = _pendingCommands.Dequeue();
            RecordCommandHistory(command, TestHarnessCommandStage.Dequeued);
            var mock = ResolveCommandMock(command.GetType());
            if (mock is null)
            {
                throw new InvalidOperationException(
                    $"No command mock registered for {command.GetType().Name}. " +
                    "Register it via MockCommand<TCommand>(...).");
            }

            var resultingMessages = mock(command);
            DispatchMany(resultingMessages);
        }
    }

    private void DispatchCore(Message message, TestHarnessMessageSource source)
    {
        RecordMessageHistory(message, source);

        if (TProgram.IsTerminal(Model))
        {
            return;
        }

        var decision = TProgram.Decide(Model, message);
        if (decision.IsErr)
        {
            RecordDecisionHistory(message, decision.Error, isError: true);
            ApplyTransition(decision.Error);
            return;
        }

        foreach (var decidedEvent in decision.Value)
        {
            RecordDecisionHistory(message, decidedEvent, isError: false);
            ApplyTransition(decidedEvent);
        }
    }

    private void ApplyTransition(Message message)
    {
        _transitionCount++;
        if (_transitionCount > _options.MaxTransitions)
        {
            throw new InvalidOperationException(
                $"Dispatch exceeded {_options.MaxTransitions} transitions. " +
                "The message flow is likely non-terminating.");
        }

        var (nextModel, command) = TProgram.Transition(Model, message);
        Model = nextModel;
        EnqueueCommand(command);
    }

    private void EnqueueCommand(Command command)
    {
        var stack = new Stack<(Command Command, int Depth)>();
        stack.Push((command, 0));

        while (stack.Count > 0)
        {
            var (current, depth) = stack.Pop();
            if (current is Command.None)
            {
                continue;
            }

            if (current is Command.Batch batch)
            {
                if (depth >= MaxBatchDepth)
                {
                    throw new InvalidOperationException(
                    $"Command batch nesting exceeded {MaxBatchDepth}. The command graph is likely malformed.");
                }

                for (var i = batch.Commands.Count - 1; i >= 0; i--)
                {
                    stack.Push((batch.Commands[i], depth + 1));
                }

                continue;
            }

            _pendingCommands.Enqueue(current);
            RecordCommandHistory(current, TestHarnessCommandStage.Enqueued);
        }
    }

    private void RecordMessageHistory(Message message, TestHarnessMessageSource source)
    {
        var entry = new TestHarnessMessageHistoryEntry(
            Sequence: _messageSequence,
            Message: message,
            MessageType: GetRecordedTypeName(message),
            Source: source);

        _messageHistory.Add(entry);
        _messageSequence++;
    }

    private void RecordDecisionHistory(Message triggerMessage, Message outputMessage, bool isError)
    {
        var entry = new TestHarnessDecisionHistoryEntry(
            Sequence: _decisionSequence,
            TriggerMessage: triggerMessage,
            OutputMessage: outputMessage,
            IsError: isError);

        _decisionHistory.Add(entry);
        _decisionSequence++;
    }

    private void RecordCommandHistory(Command command, TestHarnessCommandStage stage)
    {
        var entry = new TestHarnessCommandHistoryEntry(
            Sequence: _commandSequence,
            Command: command,
            Stage: stage);

        _commandHistory.Add(entry);
        _commandSequence++;
    }

    private static TestHarnessReplayMetadataV1 CreateDefaultReplayMetadata()
    {
        var programVersion = typeof(TProgram).Assembly.GetName().Version?.ToString() ?? "0.0.0";

        return new TestHarnessReplayMetadataV1
        {
            SessionId = Guid.NewGuid().ToString("N"),
            ProgramName = typeof(TProgram).FullName ?? typeof(TProgram).Name,
            ProgramVersion = programVersion,
            RecordedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    private static string GetRecordedTypeName(Message message) =>
        message.GetType().FullName ?? message.GetType().Name;

    private Func<Command, IReadOnlyList<Message>>? ResolveCommandMock(Type commandType)
    {
        if (_commandMocks.TryGetValue(commandType, out var exact))
        {
            return exact;
        }

        foreach (var registeredType in _mockRegistrationOrder)
        {
            if (registeredType.IsAssignableFrom(commandType) && _commandMocks.TryGetValue(registeredType, out var assignable))
            {
                return assignable;
            }
        }

        return null;
    }
}
