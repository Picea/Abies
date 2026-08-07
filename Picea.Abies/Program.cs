using Picea;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;

namespace Picea.Abies;

/// <summary>
/// Everything in a program except how it is drawn: initialization, transitions,
/// command decisions, termination and subscriptions. This is the platform-free
/// half — it mentions no DOM and no controls — so one core can be shared by
/// views targeting the browser, the server and native controls.
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
/// <typeparam name="TArgument">The initialization argument type.</typeparam>
public interface ProgramCore<TModel, TArgument> : Decider<TModel, Message, Message, Command, Message, TArgument>
{
    /// <summary>Validates a message against the current model.</summary>
    new static abstract Result<Message[], Message> Decide(TModel state, Message command);

    /// <summary>Whether the program has reached a terminal state.</summary>
    new static abstract bool IsTerminal(TModel state);

    /// <summary>The subscriptions the model currently wants active.</summary>
    static abstract Subscription Subscriptions(TModel model);
}

/// <summary>
/// How a model is drawn. Separate from <see cref="ProgramCore{TModel, TArgument}"/>
/// so a per-platform view can be paired with a shared core via
/// <see cref="WithView{TCore, TView, TModel, TArgument}"/>.
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
public interface ProgramView<TModel>
{
    /// <summary>Renders the model to a document.</summary>
    static abstract Document View(TModel model);
}

/// <summary>
/// A complete program: a core plus a view. Unchanged from before the Core/View
/// split — existing programs implement this exactly as they always have, and
/// the runtime still takes a single <c>TProgram</c>.
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
/// <typeparam name="TArgument">The initialization argument type.</typeparam>
public interface Program<TModel, TArgument> : ProgramCore<TModel, TArgument>, ProgramView<TModel>;

/// <summary>
/// Pairs a shared <typeparamref name="TCore"/> with a per-platform
/// <typeparamref name="TView"/> to form a complete <see cref="Program{TModel, TArgument}"/>.
/// <para>
/// C# cannot inherit static abstract implementations, so without this a program
/// that only wants to swap the view still has to hand-forward every core member
/// to the shared class. This does that forwarding once, in the framework:
/// </para>
/// <code>
/// Runtime&lt;WithView&lt;CounterProgram, NativeCounterView, CounterModel, Unit&gt;, CounterModel, Unit&gt;.Start(...)
/// </code>
/// </summary>
/// <typeparam name="TCore">The shared core.</typeparam>
/// <typeparam name="TView">The platform-specific view.</typeparam>
/// <typeparam name="TModel">The model type.</typeparam>
/// <typeparam name="TArgument">The initialization argument type.</typeparam>
public sealed class WithView<TCore, TView, TModel, TArgument> : Program<TModel, TArgument>
    where TCore : ProgramCore<TModel, TArgument>
    where TView : ProgramView<TModel>
{
    /// <inheritdoc cref="Automaton{TState, TEvent, TEffect, TParameters}.Initialize" />
    public static (TModel, Command) Initialize(TArgument argument) => TCore.Initialize(argument);

    /// <inheritdoc cref="Automaton{TState, TEvent, TEffect, TParameters}.Transition" />
    public static (TModel, Command) Transition(TModel model, Message message) => TCore.Transition(model, message);

    /// <inheritdoc cref="ProgramCore{TModel, TArgument}.Decide" />
    public static Result<Message[], Message> Decide(TModel state, Message command) => TCore.Decide(state, command);

    /// <inheritdoc cref="ProgramCore{TModel, TArgument}.IsTerminal" />
    public static bool IsTerminal(TModel state) => TCore.IsTerminal(state);

    /// <inheritdoc cref="ProgramCore{TModel, TArgument}.Subscriptions" />
    public static Subscription Subscriptions(TModel model) => TCore.Subscriptions(model);

    /// <inheritdoc cref="ProgramView{TModel}.View" />
    public static Document View(TModel model) => TView.View(model);
}

public record Url(IReadOnlyList<string> Path, IReadOnlyDictionary<string, string> Query, Option<string> Fragment)
{
    public static readonly Url Root = new(
        Array.Empty<string>(),
        new Dictionary<string, string>(),
        Option<string>.None);

    public static Url FromUri(Uri uri)
    {
        var path = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        var query = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var queryString = uri.Query.TrimStart('?');
            foreach (var pair in queryString.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(parts[0]);
                var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
                query[key] = value;
            }
        }

        var fragment = string.IsNullOrEmpty(uri.Fragment)
            ? Option<string>.None
            : Option.Some(uri.Fragment.TrimStart('#'));

        return new Url(path, query, fragment);
    }

    public string ToRelativeUri()
    {
        var pathPart = Path.Count > 0 ? "/" + string.Join("/", Path) : "/";
        var queryPart = Query.Count > 0
            ? "?" + string.Join("&", Query.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"))
            : string.Empty;
        var fragmentPart = Fragment.Match(f => "#" + f, () => string.Empty);
        return $"{pathPart}{queryPart}{fragmentPart}";
    }
}

public interface UrlRequest : Message
{
    record Internal(Url Url) : UrlRequest;
    record External(string Href) : UrlRequest;
}

public sealed record UrlChanged(Url Url) : Message;
