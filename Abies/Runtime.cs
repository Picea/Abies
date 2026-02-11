// =============================================================================
// Abies Runtime
// =============================================================================
// The runtime implements the MVU message loop and coordinates all framework
// subsystems: virtual DOM, commands, subscriptions, and JavaScript interop.
//
// Architecture Decision Records:
// - ADR-001: Model-View-Update Architecture (docs/adr/ADR-001-mvu-architecture.md)
// - ADR-003: Virtual DOM Implementation (docs/adr/ADR-003-virtual-dom.md)
// - ADR-005: WebAssembly Runtime (docs/adr/ADR-005-webassembly-runtime.md)
// - ADR-006: Command Pattern for Side Effects (docs/adr/ADR-006-command-pattern.md)
// - ADR-007: Subscription Model (docs/adr/ADR-007-subscriptions.md)
// - ADR-011: JavaScript Interop Strategy (docs/adr/ADR-011-javascript-interop.md)
// - ADR-013: OpenTelemetry Instrumentation (docs/adr/ADR-013-opentelemetry.md)
// =============================================================================

using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Channels;
using Abies.DOM;

namespace Abies;

/// <summary>
/// The Abies runtime implements the MVU message loop.
/// </summary>
/// <remarks>
/// The runtime coordinates:
/// - Message dispatch via an unbounded channel
/// - Virtual DOM diffing and patching
/// - Command execution (side effects)
/// - Subscription lifecycle management
/// - JavaScript interop for browser APIs
/// 
/// See ADR-001: Model-View-Update Architecture
/// See ADR-005: WebAssembly Runtime
/// </remarks>
public static partial class Runtime
{
    // Message queue for ordered processing (see ADR-001: unidirectional data flow)
    private static readonly Channel<Message> _messageChannel = Channel.CreateUnbounded<Message>();

    // Handler registries for event dispatch (see ADR-011: JavaScript interop)
    // Uses Dictionary instead of ConcurrentDictionary since WASM is single-threaded
    private static readonly Dictionary<string, Message> _handlers = new();
    private static readonly Dictionary<string, (Func<object?, Message> handler, Type dataType)> _dataHandlers = new();
    private static readonly Dictionary<string, (Func<object?, Message> handler, Type dataType)> _subscriptionHandlers = new();

    /// <summary>
    /// Starts the MVU runtime loop for the specified program.
    /// </summary>
    /// <remarks>
    /// The runtime loop:
    /// 1. Initializes model and runs initial commands
    /// 2. Renders initial virtual DOM
    /// 3. Starts subscriptions
    /// 4. Processes messages: Update → View → Diff → Patch → Commands
    /// 
    /// See ADR-001: Model-View-Update Architecture
    /// See ADR-013: OpenTelemetry Instrumentation
    /// </remarks>
    public static async Task Run<TProgram, TArguments, TModel>(TArguments arguments)
        where TProgram : Program<TModel, TArguments>
    {
        // ADR-013: Trace the entire runtime lifecycle
        using var runActivity = Instrumentation.ActivitySource.StartActivity("Run");

        // Register the URL change handler
        SetupInteropHandlers<TProgram, TArguments, TModel>();

        var currentUrl = Url.Create(new(Interop.GetCurrentUrl()));

        // Initialize the state (ADR-001: Initialize returns model + command)
        var (initialModel, initialCommand) = TProgram.Initialize(currentUrl, arguments);

        // Generate the virtual DOM (ADR-003: View is pure function)
        var document = TProgram.View(initialModel);

        var html = Render.Html(document.Body);

        // Apply the initial DOM and register handlers for the first render
        await Interop.SetAppContent(html);
        RegisterHandlers(document.Body);

        var model = initialModel;
        var dom = document.Body;

        // Start subscriptions immediately after initialization (Elm-style).
        // See ADR-007: Subscriptions are state-driven
        var subscriptionState = SubscriptionManager.Start(TProgram.Subscriptions(model), Dispatch);

        // ADR-006: Commands are executed outside the pure Update function
        using (Instrumentation.ActivitySource.StartActivity("HandleCommand"))
        {
            await TProgram.HandleCommand(initialCommand, Dispatch);
        }

        await foreach (var message in _messageChannel.Reader.ReadAllAsync())
        {
            using var messageActivity = Instrumentation.ActivitySource.StartActivity("Message");
            messageActivity?.SetTag("message.type", message.GetType().FullName);

            // Update the model based on the message
            var (newModel, command) = TProgram.Update(message, model);

            model = newModel;

            // Generate the new virtual DOM
            var newDom = TProgram.View(model);
            var alignedBody = PreserveIds(dom, newDom.Body);

            // Compute the patches
            var patches = Operations.Diff(dom, alignedBody);

            // Apply patches in batch using binary protocol for zero-copy transfer
            await Operations.ApplyBatch(patches);

            dom = alignedBody;
            await Interop.SetTitle(newDom.Title);

            // Update subscriptions after the model changes.
            subscriptionState = SubscriptionManager.Update(subscriptionState, TProgram.Subscriptions(model), Dispatch);

            // Handle the command
            switch (command)
            {
                case Navigation.Command.PushState pushState:
                    await Interop.PushState(pushState.Url.ToString());
                    var pushedMsg = TProgram.OnUrlChanged(pushState.Url);
                    Dispatch(pushedMsg);
                    break;
                case Navigation.Command.Load load:
                    await Interop.Load(load.Url.ToString());
                    break;
                case Navigation.Command.ReplaceState replaceState:
                    await Interop.ReplaceState(replaceState.Url.ToString());
                    var replacedMsg = TProgram.OnUrlChanged(replaceState.Url);
                    Dispatch(replacedMsg);
                    break;
                case Command.Batch batch:
                    foreach (var cmd in batch.Commands)
                    {
                        switch (cmd)
                        {
                            case Navigation.Command.PushState ps:
                                await Interop.PushState(ps.Url.ToString());
                                var msg = TProgram.OnUrlChanged(ps.Url);
                                Dispatch(msg);
                                break;
                            case Navigation.Command.Load ld:
                                await Interop.Load(ld.Url.ToString());
                                break;
                            case Navigation.Command.ReplaceState rs:
                                await Interop.ReplaceState(rs.Url.ToString());
                                var rmsg = TProgram.OnUrlChanged(rs.Url);
                                Dispatch(rmsg);
                                break;
                            default:
                                using (Instrumentation.ActivitySource.StartActivity("HandleCommand"))
                                {
                                    await TProgram.HandleCommand(cmd, Dispatch);
                                }
                                break;
                        }
                    }
                    break;
                default:
                    using (Instrumentation.ActivitySource.StartActivity("HandleCommand"))
                    {
                        await TProgram.HandleCommand(command, Dispatch);
                    }
                    break;
            }
        }
    }

    private static Node PreserveIds(Node? oldNode, Node newNode)
    {
        // Handle LazyMemo nodes - preserve the lazy structure but don't evaluate
        if (newNode is ILazyMemoNode newLazyMemo)
        {
            // If the old node was a lazy memo with the same key, we can skip evaluation
            if (oldNode is ILazyMemoNode oldLazyMemo && oldLazyMemo.MemoKey.Equals(newLazyMemo.MemoKey))
            {
                // Keys match - return the new lazy with old's cached content preserved
                return oldLazyMemo.CachedNode != null
                    ? newLazyMemo.WithCachedNode(oldLazyMemo.CachedNode)
                    : newNode;
            }
            // For lazy memos, we defer evaluation - just return as-is
            return newNode;
        }

        // Handle Memo nodes by recursing into their cached content
        if (newNode is IMemoNode newMemo)
        {
            // Find the matching old memo or old content
            var oldCached = oldNode is IMemoNode oldMemo ? oldMemo.CachedNode : oldNode;
            var preservedCached = PreserveIds(oldCached, newMemo.CachedNode);

            // Return a new memo with the preserved cached content
            return newMemo.WithCachedNode(preservedCached);
        }

        // Only preserve IDs when elements have the same tag AND the same element ID.
        // This is critical for keyed diffing (ADR-016): elements with different IDs
        // should NOT have their IDs swapped, as that would break key-based matching.
        if (oldNode is Element oldElement && newNode is Element newElement
            && oldElement.Tag == newElement.Tag
            && oldElement.Id == newElement.Id)  // Key match required!
        {
            // Preserve attribute IDs so DiffAttributes can emit UpdateAttribute
            // instead of a remove/add pair. This avoids wiping attributes when
            // remove is processed after add.
            var attrs = new DOM.Attribute[newElement.Attributes.Length];
            for (int i = 0; i < newElement.Attributes.Length; i++)
            {
                var attr = newElement.Attributes[i];
                var oldAttr = Array.Find(oldElement.Attributes, a => a.Name == attr.Name);
                var attrId = oldAttr?.Id ?? attr.Id;

                if (attr.Name == "id")
                {
                    attrs[i] = attr with { Id = attrId, Value = oldElement.Id };
                }
                else
                {
                    attrs[i] = attr with { Id = attrId };
                }
            }

            // For children, use key-based matching instead of positional matching.
            // Build a dictionary of old children by their element ID for O(1) lookup.
            var oldChildrenById = new Dictionary<string, Node>();
            foreach (var child in oldElement.Children)
            {
                // Unwrap memo nodes to get the actual element ID
                Node effectiveChild;
                if (child is ILazyMemoNode lazyMemo)
                {
                    effectiveChild = lazyMemo.CachedNode ?? lazyMemo.Evaluate();
                }
                else if (child is IMemoNode memo)
                {
                    effectiveChild = memo.CachedNode;
                }
                else
                {
                    effectiveChild = child;
                }

                if (effectiveChild is Element childElem)
                {
                    oldChildrenById[childElem.Id] = child; // Store the original (possibly memo) node
                }
                else if (effectiveChild is Text textNode)
                {
                    oldChildrenById[textNode.Id] = child;
                }
            }

            var children = new Node[newElement.Children.Length];
            for (int i = 0; i < newElement.Children.Length; i++)
            {
                var newChild = newElement.Children[i];
                Node? matchingOldChild = null;

                // Unwrap memo nodes to get the actual element ID for matching
                Node effectiveNewChild;
                if (newChild is ILazyMemoNode newLazyChild)
                {
                    // For lazy memo, use its own ID rather than evaluating
                    effectiveNewChild = newChild;
                }
                else if (newChild is IMemoNode newMemoChild)
                {
                    effectiveNewChild = newMemoChild.CachedNode;
                }
                else
                {
                    effectiveNewChild = newChild;
                }

                // Find matching old child by key (element ID)
                if (effectiveNewChild is Element newChildElem && oldChildrenById.TryGetValue(newChildElem.Id, out var oldMatch))
                {
                    matchingOldChild = oldMatch;
                }
                else if (effectiveNewChild is Text newTextNode && oldChildrenById.TryGetValue(newTextNode.Id, out var oldTextMatch))
                {
                    matchingOldChild = oldTextMatch;
                }
                else if (effectiveNewChild is ILazyMemoNode lazyChild && oldChildrenById.TryGetValue(lazyChild.MemoKey?.ToString() ?? newChild.Id, out var oldLazyMatch))
                {
                    // Try to match lazy nodes by their ID
                    matchingOldChild = oldLazyMatch;
                }

                children[i] = PreserveIds(matchingOldChild, newChild);
            }

            return new Element(oldElement.Id, newElement.Tag, attrs, children);
        }
        else if (oldNode is Text oldText && newNode is Text newText && oldText.Id == newText.Id)
        {
            // Preserve text node IDs only when they match
            return new Text(oldText.Id, newText.Value);
        }
        else if (newNode is Element newElem)
        {
            // No matching old element, recursively process children without preserving IDs
            var children = new Node[newElem.Children.Length];
            for (int i = 0; i < newElem.Children.Length; i++)
            {
                children[i] = PreserveIds(null, newElem.Children[i]);
            }
            return new Element(newElem.Id, newElem.Tag, newElem.Attributes, children);
        }

        return newNode;
    }

    private static void SetupInteropHandlers<TProgram, TArguments, TModel>()
        where TProgram : Program<TModel, TArguments>
    {
        Interop.OnUrlChange(newUrlString =>
        {
            var newUrl = Url.Create(new(newUrlString));
            var message = TProgram.OnUrlChanged(newUrl);
            Dispatch(message);
        });

        // Register link click and form submit handlers
        Interop.OnLinkClick(newUrlString =>
        {
            linkClickedHandler(new(newUrlString));
        });

        Interop.OnFormSubmit(newUrlString =>
        {
            linkClickedHandler(new(newUrlString));
        });
        return;

        // Handler for link clicks and form submissions
        static void linkClickedHandler(string newUrlString)
        {
            var currentUrl = Url.Create(Interop.GetCurrentUrl());
            var newUrl = Url.Create(newUrlString);
            Message message;

            if (!AreSameOrigin(currentUrl, newUrl))
            {
                message = TProgram.OnLinkClicked(new UrlRequest.External(newUrlString));
            }
            else
            {
                message = TProgram.OnLinkClicked(new UrlRequest.Internal(newUrl));
            }

            Dispatch(message);
        }

        static bool AreSameOrigin(Url currentUrl, Url newUrl)
        {
            if (newUrl.Scheme is null || newUrl.Host is null)
            {
                return true;
            }

            return currentUrl.Scheme?.GetType() == newUrl.Scheme.GetType()
                   && currentUrl.Host == newUrl.Host
                   && currentUrl.Port == newUrl.Port;
        }
    }

    /// <summary>
    /// Registers event handlers for a virtual DOM node.
    /// </summary>
    /// <param name="node"></param>
    /// <exception cref="InvalidOperationException"></exception>
    internal static void RegisterHandlers(Node node)
    {

        if (node is Element element)
        {
            // Register handlers in the current element
            foreach (var attribute in element.Attributes)
            {
                if (attribute is Handler handler)
                {
                    if (handler.Command is not null && !_handlers.TryAdd(handler.CommandId, handler.Command))
                    {
                        throw new InvalidOperationException("Command already exists");
                    }
                    if (handler.WithData is not null)
                    {
                        _dataHandlers[handler.CommandId] = (handler.WithData, handler.DataType ?? typeof(object));
                    }
                }
            }
            // Recursively register handlers in child nodes
            foreach (var child in element.Children)
            {
                RegisterHandlers(child);
            }
        }
    }

    internal static void RegisterHandler(Handler handler)
    {
        if (handler.Command is not null)
        {
            _handlers.TryAdd(handler.CommandId, handler.Command);
        }
        if (handler.WithData is not null)
        {
            _dataHandlers[handler.CommandId] = (handler.WithData, handler.DataType ?? typeof(object));
        }
    }

    internal static void UnregisterHandler(Handler handler)
    {
        if (handler.Command is not null)
        {
            _handlers.Remove(handler.CommandId);
        }
        if (handler.WithData is not null)
        {
            _dataHandlers.Remove(handler.CommandId);
        }
    }

    internal static void UnregisterHandlers(Node node)
    {
        if (node is Element element)
        {
            foreach (var attribute in element.Attributes)
            {
                if (attribute is Handler handler)
                {
                    if (handler.Command is not null)
                    {
                        _handlers.Remove(handler.CommandId);
                    }
                    if (handler.WithData is not null)
                    {
                        _dataHandlers.Remove(handler.CommandId);
                    }
                }
            }

            foreach (var child in element.Children)
            {
                UnregisterHandlers(child);
            }
        }
    }

    // Registers a subscription handler keyed by a stable subscription identifier.
    internal static void RegisterSubscriptionHandler(string key, Func<object?, Message> handler, Type dataType)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Subscription key must be non-empty.", nameof(key));
        }

        _subscriptionHandlers[key] = (handler ?? throw new ArgumentNullException(nameof(handler)), dataType);
    }

    // Removes a subscription handler keyed by its identifier.
    internal static void UnregisterSubscriptionHandler(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _subscriptionHandlers.Remove(key);
    }

    private static Unit Dispatch(Message message)
    {
        _messageChannel.Writer.TryWrite(message);
        return new();
    }

    [JSExport]
    public static void Dispatch(string messageId)
    {
        if (_handlers.TryGetValue(messageId, out var message))
        {
            var _ = Dispatch(message);
            return;
        }
        // Missing handler can occur during DOM replacement; ignore gracefully
        Debug.WriteLine($"[Abies] Missing handler for messageId={messageId}");
    }

}
