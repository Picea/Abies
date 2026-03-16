// =============================================================================
// Program.cs — Conduit WASM Bootstrap
// =============================================================================
// Entry point for the Conduit Blazor WebAssembly application.
//
// Picea.Abies.Browser.Runtime.Run handles all browser-specific wiring:
//   - Loading abies.js
//   - Setting up event delegation and navigation
//   - Creating the binary batch writer and Apply delegate
//   - Starting the MVU runtime
//   - Keeping the WASM process alive
//
// The interpreter converts ConduitCommands into HTTP API calls.
//
// API URL resolution:
//   The WASM app reads window.location.origin at startup so that the
//   interpreter can build absolute URLs for HttpClient (which requires
//   absolute URIs unless BaseAddress is set).  Because the hosting
//   server reverse-proxies /api/** to the backend, using the page
//   origin as the API base works for both development and production.
// =============================================================================

using Picea;
using Picea.Abies;
using Picea.Abies.Conduit.App;

// Import the Abies JS module early so we can read the browser origin.
// The subsequent import inside Runtime.Run is a cached no-op.
await Picea.Abies.Browser.Runtime.ImportModule();
var apiUrl = Picea.Abies.Browser.Runtime.GetOrigin();
const string SessionStorageKey = "conduit.session";

var initialSession = LoadPersistedSession();

await Picea.Abies.Browser.Runtime.Run<ConduitProgram, Model, ConduitStartup>(
    argument: new ConduitStartup(apiUrl, initialSession),
    interpreter: Interpret);

static ValueTask<Result<Message[], PipelineError>> Interpret(Command command)
{
    switch (command)
    {
        case PersistSession persist:
            Picea.Abies.Browser.Runtime.SetLocalStorageItem(
                SessionStorageKey,
                SerializeSession(persist.Session));
            return new(Result<Message[], PipelineError>.Ok([]));

        case ClearPersistedSession:
            Picea.Abies.Browser.Runtime.RemoveLocalStorageItem(SessionStorageKey);
            return new(Result<Message[], PipelineError>.Ok([]));

        default:
            return ConduitInterpreter.Interpret(command);
    }
}

static Session? LoadPersistedSession()
{
    var json = Picea.Abies.Browser.Runtime.GetLocalStorageItem(SessionStorageKey);
    if (string.IsNullOrWhiteSpace(json))
        return null;

    try
    {
        return DeserializeSession(json);
    }
    catch
    {
        Picea.Abies.Browser.Runtime.RemoveLocalStorageItem(SessionStorageKey);
        return null;
    }
}

static string SerializeSession(Session session) =>
    string.Join("|",
        Uri.EscapeDataString(session.Token),
        Uri.EscapeDataString(session.Username),
        Uri.EscapeDataString(session.Email),
        Uri.EscapeDataString(session.Bio),
        Uri.EscapeDataString(session.Image ?? string.Empty));

static Session? DeserializeSession(string value)
{
    var parts = value.Split('|');
    if (parts.Length != 5)
        return null;

    return new Session(
        Uri.UnescapeDataString(parts[0]),
        Uri.UnescapeDataString(parts[1]),
        Uri.UnescapeDataString(parts[2]),
        Uri.UnescapeDataString(parts[3]),
        string.IsNullOrEmpty(parts[4]) ? null : Uri.UnescapeDataString(parts[4]));
}
