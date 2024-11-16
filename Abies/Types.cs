using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using Abies.DOM;
using Abies.Html;

namespace Abies
{

    public delegate TModel Update<TModel>(Command command, TModel model);

    public delegate TModel Initialize<TArguments, TModel>(TArguments arguments);

    public delegate Document View<TModel>(TModel model);

    public delegate Subscription Subscriptions<TModel>(TModel model);

    public delegate Command OnUrlChange(Url url);

    public delegate Command OnUrlRequest(UrlRequest urlRequest);

    public interface UrlRequest
    {
        public sealed record Internal(Url url) : UrlRequest;
        public sealed record External(string url) : UrlRequest;
    }

 

    public record Subscription
    {

    }

    public interface Command;

    public record Application;

    public static class Browser
    {
        public static Program<TArguments, TModel> Document<TArguments, TModel>(
            Initialize<TArguments, TModel> initialize,
            View<TModel> view,
            Update<TModel> update,
            Subscriptions<TModel> subscriptions)
        {
            return new Program<TArguments, TModel>
            {
                Update = update,
                Initialize = initialize,
                View = view,
                Subscriptions = subscriptions
            };
        }

        public static Program<TArguments, TModel> Application<TArguments, TModel>(
            Initialize<TArguments, TModel> initialize,
            View<TModel> view,
            Update<TModel> update,
            Subscriptions<TModel> subscriptions,
            OnUrlRequest onUrlRequest,
            OnUrlChange onUrlChanged)
        {
            // Register the URL change handler
            Interop.OnUrlChange(newUrlString => {

                onUrlChanged(Url.FromString(newUrlString));
            });

            var urlRequesHandler = (string newUrlString) => 
            {
                var currentUrlString = Interop.GetCurrentUrl();
                var currentUrl = Url.FromString(currentUrlString);
                var newUrl = Url.FromString(newUrlString);

                if(currentUrl.Scheme.GetType() != newUrl.Scheme.GetType() || currentUrl.Host != newUrl.Host || currentUrl.Port != newUrl.Port)
                {
                    onUrlRequest(new UrlRequest.External(newUrlString));
                }
                else
                {
                    onUrlRequest(new UrlRequest.Internal(newUrl));
                }
            };

            // On form submit a link click, dispatch the URL request
            Interop.OnLinkClick(newUrlString => {
                urlRequesHandler(newUrlString);
            });

            Interop.OnFormSubmit(newUrlString => {
                urlRequesHandler(newUrlString);
            });

            return Document(initialize, view, update, subscriptions);
        }
    }

    public record Document(string Title, Node Body);

    internal static partial class Interop
    {

        [JSImport("setAppContent", "abies.js")]
        public static partial Task SetAppContent(string html);

        [JSImport("addChildHtml", "abies.js")]
        public static partial Task AddChildHtml(string parentId, string childHtml);

        [JSImport("removeChild", "abies.js")]
        public static partial Task RemoveChild(string parentId, string childId);

        [JSImport("replaceChildHtml", "abies.js")]
        public static partial Task ReplaceChildHtml(string oldNodeId, string newHtml);

        [JSImport("updateTextContent", "abies.js")]
        public static partial Task UpdateTextContent(string nodeId, string newText);

        [JSImport("updateAttribute", "abies.js")]
        public static partial Task UpdateAttribute(string id, string name, string value);

        [JSImport("addAttribute", "abies.js")]
        public static partial Task AddAttribute(string id, string name, string value);

        [JSImport("removeAttribute", "abies.js")]
        public static partial Task RemoveAttribute(string id, string name);

        [JSImport("setTitle", "abies.js")]
        public static partial Task SetTitle(string title);

        [JSImport("writeToConsole", "abies.js")]
        public static partial Task WriteToConsole(string message);

        [JSImport("onUrlChange", "abies.js")]
        public static partial void OnUrlChange([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> handler);

        [JSImport("getCurrentUrl", "abies.js")]
        public static partial string GetCurrentUrl();

        [JSImport("onLinkClick", "abies.js")]
        internal static partial void OnLinkClick([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> value);

        [JSImport("onFormSubmit", "abies.js")]
        internal static partial void  OnFormSubmit([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> value);
    }

    public static partial class Runtime
    {
        private static Program? _program;

        public static async Task Run<TArguments, TModel>(TArguments arguments, Program<TArguments, TModel> program)
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

        public static string RenderToHtml(Node node)
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
                    sb.Append($"<span id={text.Id} {System.Web.HttpUtility.HtmlEncode(text.Value)}</span>");
                    break;
                // Handle other node types if necessary
                default:
                    break;
            }
        }
    }

    public interface Program
    {
        public Task Dispatch(string commandId);
    }

    public record Program<TArguments, TModel> : Program
    {
        private TModel? model;
        private Node? _dom;
        private readonly ConcurrentDictionary<string, Command> _handlers = new();
        public required Update<TModel> Update { get; init; }
        public required Initialize<TArguments, TModel> Initialize { get; init; }
        public required View<TModel> View { get; init; }
        public required Subscriptions<TModel> Subscriptions { get; init; }

        public async Task Run(TArguments arguments)
        {
            // Initialize the state
            var initialModel = Initialize(arguments);

            // Generate the virtual DOM
            var document = View(initialModel);

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
                        if (!_handlers.TryAdd(handler.Id, handler.Command))
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

        public async Task Dispatch(string commandId)
        {
            if (!_handlers.TryGetValue(commandId, out var command))
            {
                await Interop.WriteToConsole($"Command {commandId} not found");
                throw new InvalidOperationException("Command not found");
            }

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

            await Interop.WriteToConsole($"Command {commandId} dispatched");
            await Interop.WriteToConsole($"Command {command} found");

            // Update the state
            model = Update(command, model);

            // Generate new virtual DOM
            var newDom = View(model);

            // Compute the patches
            var patches = Operations.Diff(_dom, newDom.Body);

            // Apply patches and (de)register handlers
            foreach (var patch in patches)
            {
                if (patch is AddHandler addHandler)
                {
                    if (!_handlers.TryAdd(addHandler.Handler.Id, addHandler.Handler.Command))
                    {
                        // todo: log
                        await Interop.WriteToConsole("Command already exists");
                    }
                    await Interop.WriteToConsole($"Command {addHandler.Handler.Id} added");
                }
                else if (patch is RemoveHandler removeHandler)
                {
                    if (!_handlers.TryRemove(removeHandler.Handler.Id, out _))
                    {
                        // todo: log
                        await Interop.WriteToConsole("Command not found");
                    }
                }
                await Operations.Apply(patch);
            }
            // Update the current virtual DOM
            _dom = newDom.Body;
            await Interop.SetTitle(newDom.Title);
        }
    }
}

namespace Abies.DOM
{
    public record Node(string Id);
    public record Attribute(string Name, string Value);

    public record Element(string Id, string Tag, Attribute[] Attributes, Node[] Children) : Node(Id);

    public record Handler(string Name, string Id, Command Command) : Attribute($"data-event-{Name}", Id);

    public record Text(string Id, string Value) : Node(Id);

    public interface Patch { }

    public readonly record struct AddRoot : Patch
    {
        public required Element Element { get; init; }
    }

    public readonly record struct ReplaceChild : Patch
    {
        public required Element OldElement { get; init; }
        public required Element NewElement { get; init; }
    }

    public readonly record struct AddChild : Patch
    {
        public required Element Parent { get; init; }
        public required Element Child { get; init; }
    }

    public readonly record struct RemoveChild : Patch
    {
        public required Element Parent { get; init; }
        public required Element Child { get; init; }
    }

    public readonly record struct UpdateAttribute : Patch
    {
        public required Element Element { get; init; }
        public required Attribute Attribute { get; init; }
        public required string Value { get; init; }
    }

    public readonly record struct AddAttribute : Patch
    {
        public required Element Element { get; init; }
        public required Attribute Attribute { get; init; }
    }

    public readonly record struct RemoveAttribute : Patch
    {
        public required Element Element { get; init; }
        public required Attribute Attribute { get; init; }
    }

    public readonly record struct AddHandler : Patch
    {
        public required Element Element { get; init; }
        public required Handler Handler { get; init; }
    }

    public readonly record struct RemoveHandler : Patch
    {
        public required Element Element { get; init; }
        public required Handler Handler { get; init; }
    }

    public readonly record struct UpdateText : Patch
    {
        public required Text Node { get; init; }
        public required string Text { get; init; }
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

    public static class Operations
    {
        public static async Task Apply(Patch patch)
        {
            switch (patch)
            {
                case AddRoot addRoot:
                    await Interop.SetAppContent(Render.Html(addRoot.Element));
                    break;
                case ReplaceChild replaceChild:
                    await Interop.ReplaceChildHtml(replaceChild.OldElement.Id, Render.Html(replaceChild.NewElement));
                    break;
                case AddChild addChild:
                    await Interop.AddChildHtml(addChild.Parent.Id, Render.Html(addChild.Child));
                    break;
                case RemoveChild removeChild:
                    await Interop.RemoveChild(removeChild.Parent.Id, removeChild.Child.Id);
                    break;
                case UpdateAttribute updateAttribute:
                    await Interop.UpdateAttribute(updateAttribute.Element.Id, updateAttribute.Attribute.Name, updateAttribute.Value);
                    break;
                case AddAttribute addAttribute:
                    await Interop.AddAttribute(addAttribute.Element.Id, addAttribute.Attribute.Name, addAttribute.Attribute.Value);
                    break;
                case RemoveAttribute removeAttribute:
                    await Interop.RemoveAttribute(removeAttribute.Element.Id, removeAttribute.Attribute.Name);
                    break;
                case AddHandler addHandler:
                    await Interop.AddAttribute(addHandler.Element.Id, addHandler.Handler.Name, addHandler.Handler.Value);
                    break;
                case RemoveHandler removeHandler:
                    await Interop.RemoveAttribute(removeHandler.Element.Id, removeHandler.Handler.Name);
                    break;
                case UpdateText updateText:
                    await Interop.UpdateTextContent(updateText.Node.Id, updateText.Text);
                    break;
                default:
                    throw new Exception("Unknown patch type");
            }
        }

        public static List<Patch> Diff(Node oldNode, Node newNode)
        {
            if (oldNode is null)
            {
                // Generate patches to create the newNode from scratch
                return [new AddRoot { Element = (Element)newNode }];
            }

            List<Patch> patches = (oldNode, newNode) switch
            {
                (Text o, Text n) => o.Value == n.Value
                    ? []
                    : [new UpdateText { Node = o, Text = n.Value }],
                (Element o, Element n) when o.Tag != n.Tag => [new ReplaceChild { OldElement = o, NewElement = n }],
                (Element o, Element n) => DiffElements(o, n),

                _ => throw new Exception("Unknown node type")
            };
            return patches;
        }

        private static List<Patch> DiffElements(Element oldElement, Element newElement)
        {
            var patches = new List<Patch>();

            if (oldElement.Tag != newElement.Tag)
            {
                return [new ReplaceChild { OldElement = oldElement, NewElement = newElement }];
            }

            // Compare Attributes
            var oldAttributes = oldElement.Attributes.ToDictionary(a => a.Name);
            var newAttributes = newElement.Attributes.ToDictionary(a => a.Name);


            foreach (var attr in newAttributes)
            {
                if (oldAttributes.TryGetValue(attr.Key, out var oldAttr))
                {
                    if (!attr.Value.Equals(oldAttr))
                    {
                        if (attr.Value is Handler handler)
                        {
                            patches.Add(new RemoveHandler
                            {
                                Element = oldElement,
                                Handler = (Handler)oldAttr
                            });
                            patches.Add(new AddHandler
                            {
                                Element = oldElement,
                                Handler = handler
                            });
                        }
                        else
                        {
                            patches.Add(new UpdateAttribute
                            {
                                Element = oldElement,
                                Attribute = attr.Value, // Use the attribute key
                                Value = attr.Value.Value
                            });
                        }
                    }
                }
                else
                {
                    // Handle attribute addition
                    if (attr.Value is Handler handler)
                    {
                        patches.Add(new AddHandler
                        {
                            Element = oldElement,
                            Handler = handler
                        });
                    }
                    else
                    {
                        patches.Add(new AddAttribute
                        {
                            Element = oldElement,
                            Attribute = attr.Value,
                        });
                    }
                }
            }

            // Handle attribute removals
            foreach (var oldAttr in oldAttributes)
            {
                if (!newAttributes.ContainsKey(oldAttr.Key))
                {
                    if (oldAttr.Value is Handler handler)
                    {
                        patches.Add(new RemoveHandler
                        {
                            Element = oldElement,
                            Handler = handler
                        });
                    }
                    else
                    {
                        patches.Add(new RemoveAttribute
                        {
                            Element = oldElement,
                            Attribute = oldAttr.Value
                        });
                    }
                }
            }

            // Compare Children
            int maxChildren = Math.Max(oldElement.Children.Length, newElement.Children.Length);
            for (int i = 0; i < maxChildren; i++)
            {
                var oldChild = i < oldElement.Children.Length ? oldElement.Children[i] : null;
                var newChild = i < newElement.Children.Length ? newElement.Children[i] : null;

                if (oldChild == null && newChild != null)
                {
                    if (newChild is Element elementChild)
                    {
                        patches.Add(new AddChild { Parent = oldElement, Child = elementChild });
                    }
                    continue;
                }

                if (newChild == null && oldChild != null)
                {
                    patches.Add(new RemoveChild { Parent = oldElement, Child = (Element)oldChild });
                    continue;
                }

                if (oldChild != null && newChild != null)
                {
                    var childPatches = Diff(oldChild, newChild);
                    patches.AddRange(childPatches);
                }
            }

            return patches;
        }
    }
}







namespace Abies.Html
{


    public static class Elements
    {
        public static Element element(string tag, DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
            => new(id.ToString(), tag, [Attributes.id(id.ToString()), .. attributes], children);

        public static Node a(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
            => element("a", attributes, children, id);

        public static Node div(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
            => element("div", attributes, children, id);

        public static Node button(DOM.Attribute[] attributes, Node[] children, [CallerLineNumber] int id = 0)
            => element("button", attributes, children, id);

        public static Node text(string value, [CallerLineNumber] int id = 0)
            => new Text(id.ToString(), value);
    }

    public static class Attributes
    {
        public static DOM.Attribute attribute(string name, string value, [CallerLineNumber] int id = 0)
            => new(name, value);
        public static DOM.Attribute id(string value, [CallerLineNumber] int id = 0)
            => attribute("id", value, id);

        public static DOM.Attribute type(string value, [CallerLineNumber] int id = 0)
            => attribute("type", value, id);

        public static DOM.Attribute href(string value, [CallerLineNumber] int id = 0)
            => attribute("href", value, id);
    }

    public static class Events
    {
        public static Handler on(string name, Command command, [CallerLineNumber] int id = 0)
            => new(name, id.ToString(), command);
        public static Handler onclick(Command command, [CallerLineNumber] int id = 0)
            => on("click", command, id);

        public static Handler onchange(Command command, [CallerLineNumber] int id = 0)
            => on("change", command, id);
    }
}
