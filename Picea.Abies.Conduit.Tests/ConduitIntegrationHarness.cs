using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Picea;
using Picea.Abies;
using Picea.Abies.Conduit.App;
using Picea.Abies.Testing;

namespace Picea.Abies.Conduit.Tests;

public sealed class ConduitIntegrationHarness : IDisposable
{
    private static readonly JsonSerializerOptions ReplayJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, Type> ReplayMessageTypes =
        typeof(ConduitProgram).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                typeof(ConduitMessage).IsAssignableFrom(type))
            .ToDictionary(type => type.Name, type => type, StringComparer.Ordinal);

    private readonly TestHarness<ConduitProgram, Model, ConduitStartup> _harness;

    private ConduitIntegrationHarness(TestHarness<ConduitProgram, Model, ConduitStartup> harness) =>
        _harness = harness;

    public Model Model => _harness.Model;

    public static ConduitIntegrationHarness Create(
        string apiUrl = "http://localhost:5179",
        Session? session = null,
        Url? initialUrl = null,
        TestHarnessOptions? options = null)
    {
        var startup = new ConduitStartup(apiUrl, session, initialUrl);
        var harness = TestHarness<ConduitProgram, Model, ConduitStartup>.Create(startup, options);
        return new ConduitIntegrationHarness(harness);
    }

    public void MockCommand<TCommand>(Func<TCommand, IReadOnlyList<Message>> resolver)
        where TCommand : Command =>
        _harness.MockCommand(resolver);

    public void Dispatch(Message message) => _harness.Dispatch(message);

    public void DrainCommands() => _harness.DrainCommands();

    public void DispatchAndDrain(Message message)
    {
        _harness.Dispatch(message);
        _harness.DrainCommands();
    }

    public string ExportReplaySessionJson(string sessionId)
    {
        var metadata = new TestHarnessReplayMetadataV1
        {
            SessionId = sessionId,
            ProgramName = nameof(ConduitProgram),
            ProgramVersion = "2.0.0",
            RecordedAtUnixMs = 0
        };

        return _harness.ExportReplaySessionJson(
            mapEntryPayload: entry =>
                JsonSerializer.SerializeToElement(entry.Message, entry.Message.GetType(), ReplayJsonOptions),
            mapEntryMessageType: entry => entry.Message.GetType().Name,
            metadata: metadata,
            sourceFilter: TestHarnessMessageSource.Dispatch);
    }

    public void ReplaySessionJson(string sessionJson) =>
        _harness.LoadAndReplaySession(sessionJson, MapReplayEntry);

    public string RenderNormalizedBodyHtml()
    {
        var html = Render.Html(ConduitProgram.View(_harness.Model).Body);
        return NormalizeHtml(html);
    }

    public Task<VisualComparisonResult> CompareVisualAsync(
        IPage page,
        string baselinePath,
        VisualComparisonOptions? options = null) =>
        _harness.CompareVisual(page, baselinePath, options);

    public static string NormalizeHtml(string html)
    {
        var withoutHandlerIds = Regex.Replace(
            html,
            "data-event-([a-zA-Z0-9_-]+)=\"[^\"]+\"",
            "data-event-$1=\"handler\"");

        var collapsed = Regex.Replace(withoutHandlerIds, @">\s+<", "><");
        return collapsed.Trim();
    }

    private static Message MapReplayEntry(TestHarnessReplayEntryV1 entry)
    {
        if (!ReplayMessageTypes.TryGetValue(entry.MessageType, out var messageType))
            throw new InvalidOperationException($"Unsupported Conduit replay message type '{entry.MessageType}'.");

        var message = JsonSerializer.Deserialize(entry.Payload.GetRawText(), messageType, ReplayJsonOptions) as Message;
        return message ?? throw new InvalidOperationException($"Failed to deserialize replay message type '{entry.MessageType}'.");
    }

    public void Dispose()
    {
        // TestHarness has no unmanaged resources and is not IDisposable.
    }
}
