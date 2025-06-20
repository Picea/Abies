using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Channels;
using Abies.DOM;

namespace Abies;

public static partial class Runtime
{
    private static readonly Channel<Message> _messageChannel = Channel.CreateUnbounded<Message>();
    private static readonly ConcurrentDictionary<string, Message> _handlers = new();
    private static readonly ConcurrentDictionary<string, (Func<object?, Message> handler, Type dataType)> _dataHandlers = new();
    
    public static async Task Run<TProgram, TArguments, TModel>(TArguments arguments)
        where TProgram : Program<TModel, TArguments>
    {
        using var runActivity = Instrumentation.ActivitySource.StartActivity("Run");

        // Register the URL change handler
        SetupInteropHandlers<TProgram, TArguments, TModel>();

        var currentUrl = Url.Create(new(Interop.GetCurrentUrl()));

        // Initialize the state
        var (initialModel, initialCommand) = TProgram.Initialize(currentUrl, arguments);

        // Generate the virtual DOM
        var document = TProgram.View(initialModel);

        var html = Render.Html(document.Body);

        RegisterHandlers(document.Body);

        // Apply the patches
        await Interop.SetAppContent(html);

        var model = initialModel;
        var dom = document.Body;

        using (Instrumentation.ActivitySource.StartActivity("HandleCommand"))
        {
            await TProgram.HandleCommand(initialCommand, Dispatch);
        }
        
        await foreach(var message in _messageChannel.Reader.ReadAllAsync())
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

            // Apply patches and (de)register handlers
            foreach (var patch in patches)
            {
                if (patch is AddHandler addHandler)
                {
                    if (addHandler.Handler.Command is not null && !_handlers.TryAdd(addHandler.Handler.CommandId, addHandler.Handler.Command))
                    {
                        continue;
                    }
                    if (addHandler.Handler.WithData is not null)
                    {
                        _dataHandlers[addHandler.Handler.CommandId] = (addHandler.Handler.WithData, addHandler.Handler.DataType ?? typeof(object));
                    }
                }
                else if (patch is RemoveHandler removeHandler)
                {
                    if (removeHandler.Handler.Command is not null)
                    {
                        _handlers.TryRemove(removeHandler.Handler.CommandId, out _);
                    }
                    if (removeHandler.Handler.WithData is not null)
                    {
                        _dataHandlers.TryRemove(removeHandler.Handler.CommandId, out _);
                    }
                }
                await Operations.Apply(patch);
            }

            dom = alignedBody;
            await Interop.SetTitle(newDom.Title);

            // Handle the command
            switch (command)
            {
                case Navigation.Command.PushState pushState:
                    await Interop.PushState(pushState.Url.ToString());
                    break;
                case Navigation.Command.Load load:
                    await Interop.Load(load.Url.ToString());
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
            if (oldNode is Element oldElement && newNode is Element newElement && oldElement.Tag == newElement.Tag)
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

                var children = new Node[newElement.Children.Length];
                for (int i = 0; i < newElement.Children.Length; i++)
                {
                    var oldChild = i < oldElement.Children.Length ? oldElement.Children[i] : null;
                    children[i] = PreserveIds(oldChild, newElement.Children[i]);
                }

                return new Element(oldElement.Id, newElement.Tag, attrs, children);
            }
            else if (oldNode is Text oldText && newNode is Text newText)
            {
                // Preserve text node IDs so they can be properly updated
                return new Text(oldText.Id, newText.Value);
            }
            else if (newNode is Element newElem)
            {
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
        void linkClickedHandler(string newUrlString)
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
            return currentUrl.Scheme.GetType() == newUrl.Scheme.GetType()
                   && currentUrl.Host == newUrl.Host
                   && currentUrl.Port == newUrl.Port;
        }
    }

    private static void RegisterHandlers(Node node)
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
    
    private static System.ValueTuple Dispatch(Message message)
    {
        _messageChannel.Writer.TryWrite(message);
        return new();
    }
    
    [JSExport]
    public static void Dispatch(string messageId)
    {
        if (!_handlers.TryGetValue(messageId, out var message))
        {
            throw new InvalidOperationException("Message to dispatch not found");
        }

        var _ = Dispatch(message);
    }

    [JSExport]
    public static void DispatchData(string messageId, string? json)
    {
        if (_dataHandlers.TryGetValue(messageId, out var entry))
        {
            object? data = json is null ? null : System.Text.Json.JsonSerializer.Deserialize(json, entry.dataType);
            var message = entry.handler(data);
            Dispatch(message);
            return;
        }

        if (_handlers.TryGetValue(messageId, out var message2))
        {
            Dispatch(message2);
            return;
        }

        throw new InvalidOperationException("Message to dispatch not found");
    }
}