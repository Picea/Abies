using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Abies;
using Abies.DOM;

namespace Abies
{
    public interface Element<TModel, TArgument>
    {
        public static abstract Node View(TModel model);
        public static abstract (TModel model, IEnumerable<Command> commands) Update(Message message, TModel model);
        public static abstract TModel Initialize(TArgument argument);
        public static abstract Subscription Subscriptions(TModel model);
    }

    public interface Application<TModel, TArgument> 
    {
        public static abstract TModel Initialize(Url url, TArgument argument);
        public static abstract (TModel model, IEnumerable<Command> commands) Update(Message message, TModel model);
        public static abstract Document View(TModel model);
        public static abstract Message OnUrlChanged(Url url);
        public static abstract Message OnLinkClicked(UrlRequest urlRequest);
        public static abstract Subscription Subscriptions(TModel model);
    }

    public interface UrlRequest : Message
    {
        public sealed record Internal(Url Url) : UrlRequest;
        public sealed record External(string Url) : UrlRequest;
    }


    public record Subscription
    {

    }

    public interface Message;

    public interface Command
    {
        public record struct None : Command;

                
    }

    public static class Commands
    {
        public static Command.None None = new(); 
    }

    public static class Browser
    {
        public static Program<TApplication, TArguments, TModel> Application<TApplication, TArguments, TModel>()
            where TApplication : Application<TModel, TArguments>
        {
            var currentUrl = Url.Create(Interop.GetCurrentUrl());

            var program = new Program<TApplication, TArguments, TModel>();

            // Register the URL change handler
            Interop.OnUrlChange(async newUrlString =>
            {
                var newUrl = Url.Create(new(newUrlString));
                var message = TApplication.OnUrlChanged(newUrl);
                await program.Dispatch(message);
            });

            // Handler for link clicks and form submissions
            async void linkClickedHandler(string newUrlString)
            {
                var currentUrl = Url.Create(Interop.GetCurrentUrl());
                var newUrl = Url.Create(newUrlString);
                Message message;

                if (!AreSameOrigin(currentUrl, newUrl))
                {
                    message = TApplication.OnLinkClicked(new UrlRequest.External(newUrlString));
                }
                else
                {
                    message = TApplication.OnLinkClicked(new UrlRequest.Internal(newUrl));
                }

                await program.Dispatch(message);
            }


            // Register link click and form submit handlers
            Interop.OnLinkClick(newUrlString =>
            {
                linkClickedHandler(new(newUrlString));
            });

            Interop.OnFormSubmit(newUrlString =>
            {
                linkClickedHandler(new(newUrlString));
            });

            return program;
        }

        private static bool AreSameOrigin(Url currentUrl, Url newUrl)
        {
            return currentUrl.Scheme.GetType() == newUrl.Scheme.GetType()
                && currentUrl.Host == newUrl.Host
                && currentUrl.Port == newUrl.Port;
        }
    }
}

    public record Document(string Title, Node Body);

    /// <summary>
    /// The runtime for the Abies framework
    /// </summary>
    /// <remarks>
    /// The runtime is responsible for running the program and dispatching commands
    /// This can not be a generic class because it needs to be called from JavaScript
    /// </remarks>
    public static partial class Runtime
    {
        /// <summary>
        /// The current program
        /// </summary>
        /// <remarks>
        /// This can not be a generic field because it needs to be called from JavaScript
        /// </remarks>
        private static Program? _program;

        public static async Task Run<TApplication, TArguments, TModel>(Program<TApplication, TArguments, TModel> program, TArguments arguments)
            where TApplication : Application<TModel, TArguments>
    {
            _program = program;
            await program.Run(arguments);
        }

        [JSExport]
        public static void Dispatch(string commandId)
        {
            if (_program == null)
            {
                throw new InvalidOperationException("Program not initialized");
            }
            Interop.WriteToConsole($"Command {commandId} arrived");
            _program.Dispatch(commandId);
        }
    }

    public interface Program
    {
        public Task Dispatch(string messageId);

        public Task Dispatch(Message message);
    }

    public record Program<TApplication, TArguments, TModel> : Program
        where TApplication : Application<TModel, TArguments>
    {
        private TModel? model;
        private Node? _dom;
        // todo: clean up handlers when they are no longer needed
        private readonly ConcurrentDictionary<string, Message> _handlers = new();
        private readonly SemaphoreSlim _dispatchLock = new(1, 1);

        public async Task Run(TArguments arguments)
        {
            var currentUrl = Url.Create(new(Interop.GetCurrentUrl()));

            // Initialize the state
            var initialModel = TApplication.Initialize(currentUrl, arguments);

            // Generate the virtual DOM
            var document = TApplication.View(initialModel);

            var html = Render.Html(document.Body);

            RegisterHandlers(document.Body);

            // Apply the patches
            await Interop.SetAppContent(html);

            model = initialModel;
            _dom = document.Body;
        }

        private void RegisterHandlers(Node node)
        {
            if (node is Element element)
            {
                // Register handlers in the current element
                foreach (var attribute in element.Attributes)
                {
                    if (attribute is Handler handler)
                    {
                        if (!_handlers.TryAdd(handler.CommandId, handler.Command))
                        {
                            throw new InvalidOperationException("Command already exists");
                        }
                        Interop.WriteToConsole($"Command {handler.Id} added");
                    }
                }
                // Recursively register handlers in child nodes
                foreach (var child in element.Children)
                {
                    RegisterHandlers(child);
                }
            }
        }

        private static Node PreserveIds(Node? oldNode, Node newNode)
        {
            if (oldNode is Element oldElement && newNode is Element newElement && oldElement.Tag == newElement.Tag)
            {
                var attrs = new Abies.DOM.Attribute[newElement.Attributes.Length];
                for (int i = 0; i < newElement.Attributes.Length; i++)
                {
                    var attr = newElement.Attributes[i];
                    attrs[i] = attr.Name == "id" ? attr with { Value = oldElement.Id } : attr;
                }

                var children = new Node[newElement.Children.Length];
                for (int i = 0; i < newElement.Children.Length; i++)
                {
                    var oldChild = i < oldElement.Children.Length ? oldElement.Children[i] : null;
                    children[i] = PreserveIds(oldChild, newElement.Children[i]);
                }

                return new Element(oldElement.Id, newElement.Tag, attrs, children);
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

        public async Task Dispatch(Message message)
        {
            await _dispatchLock.WaitAsync();
            try
            {
            if (model is null)
            {
                await Interop.WriteToConsole("Model not initialized");
                throw new InvalidOperationException("Model not initialized");
            }

            if (_dom is null)
            {
                await Interop.WriteToConsole("DOM not initialized");
                throw new InvalidOperationException("DOM not initialized");
            }

            // Update the state with the new message
            var (newModel, commands) = TApplication.Update(message, model);

            model = newModel;

            // Generate new virtual DOM
            var newDocument = TApplication.View(newModel);

            var alignedBody = PreserveIds(_dom, newDocument.Body);

            // Compute the patches
            var patches = Operations.Diff(_dom, alignedBody);

            // Apply patches and (de)register handlers
            foreach (var patch in patches)
            {
                if (patch is AddHandler addHandler)
                {
                    if (!_handlers.TryAdd(addHandler.Handler.CommandId, addHandler.Handler.Command))
                    {
                        await Interop.WriteToConsole("Command already exists");
                    }
                    await Interop.WriteToConsole($"Command {addHandler.Handler.Id} added");
                }
                else if (patch is RemoveHandler removeHandler)
                {
                    if (!_handlers.TryRemove(removeHandler.Handler.CommandId, out _))
                    {
                        await Interop.WriteToConsole("Command not found");
                    }
                }
                await Operations.Apply(patch);
            }

            // Update the current virtual DOM
            _dom = alignedBody;
            await Interop.SetTitle(newDocument.Title);

            foreach (var command in commands)
            {
                await HandleCommand(command);
            }
        }
        finally
        {
            _dispatchLock.Release();
        }
    }

        private static async Task HandleCommand(Command command)
        {
            switch(command)
            {                
                case Navigation.Command.PushState pushState:
                    await Interop.PushState(pushState.Url.ToString());
                    break;
                case Navigation.Command.Load load:
                    await Interop.Load(load.Url.ToString());
                    break;
                default:
                    // Check if the command has an ExecuteAsync method
                    var commandType = command.GetType();
                    var executeMethod = commandType.GetMethod("ExecuteAsync");
                    if (executeMethod != null)
                    {
                        try
                        {                            var result = executeMethod.Invoke(command, null);
                            if (result is Task<Message> messageTask)
                            {
                                var message = await messageTask;
                                // We can't directly access Runtime._program here, so just use reflection
                                await Interop.WriteToConsole($"Command executed: {command.GetType().Name}");
                                var programField = typeof(Runtime).GetField("_program", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                                if (programField == null)
                                {
                                    await Interop.WriteToConsole("Program field not found");
                                    throw new InvalidOperationException("Program field not found in Runtime class");
                                }
                                
                                var program = programField.GetValue(null);
                                if (program == null)
                                {
                                    await Interop.WriteToConsole("Program not initialized");
                                    throw new InvalidOperationException("Program not initialized");
                                }
                                var dispatchMethod = program.GetType().GetMethod("Dispatch", new[] { typeof(Message) });
                                if (dispatchMethod == null)
                                {
                                    await Interop.WriteToConsole("Dispatch method not found");
                                    throw new InvalidOperationException("Dispatch method not found");
                                }
                                
                                var dispatchResult = dispatchMethod.Invoke(program, new object[] { message });
                                if (dispatchResult is Task task)
                                {
                                    await task;
                                }
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            await Interop.WriteToConsole($"Error handling command: {ex.Message}");
                            return;
                        }
                    }                    throw new InvalidOperationException($"Unknown command: {command.GetType().Name}");
            }
        }

        public async Task Dispatch(string messageId)
        {
            if (!_handlers.TryGetValue(messageId, out var message))
            {
                await Interop.WriteToConsole($"Command {messageId} not found");
                throw new InvalidOperationException("Command not found");
            }

            await Dispatch(message);
        }
    }


namespace Abies.DOM
{
    public record Node(string Id);
    public record Attribute(string Id, string Name, string Value);

    public record Element(string Id, string Tag, Attribute[] Attributes, params Node[] Children) : Node(Id);

    public record Handler(string Name, string CommandId, Message Command, string Id) : Attribute(Id, $"data-event-{Name}", CommandId);

    public record Text(string Id, string Value) : Node(Id)
    {
        public static implicit operator string(Text text) => text.Value;
        public static implicit operator Text(string text) => new(text, text);
    };

    public record Empty() : Node("");

    public interface Patch { }

    public readonly struct AddRoot(Element element) : Patch
    {
        public readonly Element Element = element;
    }

    public readonly struct ReplaceChild(Element oldElement, Element newElement) : Patch
    {
        public readonly Element OldElement = oldElement;
        public readonly Element NewElement = newElement;
    }

    public readonly struct AddChild(Element parent, Element child) : Patch
    {
        public readonly Element Parent = parent;
        public readonly Element Child = child;
    }

    public readonly struct RemoveChild(Element parent, Element child) : Patch
    {
        public readonly Element Parent = parent;
        public readonly Element Child = child;
    }

    public readonly struct UpdateAttribute(Element element, Attribute attribute, string value) : Patch
    {
        public readonly Element Element = element;
        public readonly Attribute Attribute = attribute;
        public readonly string Value = value;
    }

    public readonly struct AddAttribute(Element element, Attribute attribute) : Patch
    {
        public readonly Element Element = element;
        public readonly Attribute Attribute = attribute;
    }

    public readonly struct RemoveAttribute(Element element, Attribute attribute) : Patch
    {
        public readonly Element Element = element;
        public readonly Attribute Attribute = attribute;
    }

    public readonly struct AddHandler(Element element, Handler handler) : Patch
    {
        public readonly Element Element = element;
        public readonly Handler Handler = handler;
    }

    public readonly struct RemoveHandler(Element element, Handler handler) : Patch
    {
        public readonly Element Element = element;
        public readonly Handler Handler = handler;
    }

    public readonly struct UpdateText(Text node, string text) : Patch
    {
        public readonly Text Node = node;
        public readonly string Text = text;
    }



    public static class Render
    {
        public static string Html(Node node)
        {
            var sb = new System.Text.StringBuilder();
            RenderNode(node, sb);
            return sb.ToString();
        }

        private static void RenderNode(Node node, System.Text.StringBuilder sb)
        {
            switch (node)
            {
                case Element element:
                    sb.Append($"<{element.Tag}");
                    foreach (var attr in element.Attributes)
                    {
                        if (attr is Handler handler)
                        {
                            sb.Append($" {handler.Name}=\"{handler.Value}\"");
                        }

                        sb.Append($" {attr.Name}=\"{System.Web.HttpUtility.HtmlEncode(attr.Value)}\"");
                    }
                    sb.Append('>');
                    foreach (var child in element.Children)
                    {
                        RenderNode(child, sb);
                    }
                    sb.Append($"</{element.Tag}>");
                    break;
                case Text text:
                    sb.Append($"<span id={text.Id}>{System.Web.HttpUtility.HtmlEncode(text.Value)}</span>");
                    break;
                // Handle other node types if necessary
                default:
                    break;
            }
        }
    }

    // Operations class moved to a dedicated file for clarity
}