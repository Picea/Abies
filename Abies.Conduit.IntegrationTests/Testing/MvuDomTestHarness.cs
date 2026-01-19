using System;
using System.Collections.Generic;
using System.Linq;
using Abies.DOM;
using Abies.Html;

namespace Abies.Conduit.IntegrationTests.Testing;

/// <summary>
/// Small, deterministic MVU test harness:
/// - Render a Conduit page (Node)
/// - Find an element with a handler
/// - Dispatch the handler message into the page Update
/// - Re-render and assert on the resulting virtual DOM
///
/// This avoids Playwright/browser dependency while still validating view structure + interactions.
/// </summary>
internal static class MvuDomTestHarness
{
    public static Func<Element, bool> HasTag(string tag) => el => el.Tag == tag;

    public static Func<Element, bool> HasClassFragment(string fragment) =>
        el => el.Attributes.Any(a => a.Name == "class" && a.Value.Contains(fragment, StringComparison.Ordinal));

    public static Func<Element, bool> HasAttribute(string name, string value) =>
        el => el.Attributes.Any(a => a.Name == name && a.Value == value);

    public static Func<Element, bool> HasTestId(string testId) =>
        el => el.Attributes.Any(a => a.Name == "data-testid" && a.Value == testId);

    public static Func<Element, bool> HasDirectText(string text) =>
        el => el.Children.OfType<Text>().Any(t => t.Value == text);

    public static Func<Element, bool> And(this Func<Element, bool> left, Func<Element, bool> right) =>
        el => left(el) && right(el);

    public static Node Render<TModel, TMsg>(TModel model, Func<TModel, Node> view)
        where TMsg : Abies.Message
        => view(model);

    public static (TModel model, Abies.Command command) DispatchClick<TModel>(
        TModel model,
        Func<TModel, Node> view,
        Func<Abies.Message, TModel, (TModel model, Abies.Command command)> update,
        Func<Element, bool> elementPredicate)
    {
        var dom = view(model);
        var (element, handler) = FindFirstHandler(dom, "click", elementPredicate);

        // In Conduit most handlers use a pre-created message (handler.Command)
        if (handler.Command is not null)
        {
            return update(handler.Command, model);
        }

        // For WithData handlers we'd need event data; keep the harness minimal for now.
        throw new NotSupportedException(
            $"Found click handler on element id={element.Id}, but it requires event data (WithData) which isn't supported by this harness yet.");
    }

    public static (TModel model, Abies.Command command) DispatchInput<TModel>(
        TModel model,
        Func<TModel, Node> view,
        Func<Abies.Message, TModel, (TModel model, Abies.Command command)> update,
        Func<Element, bool> elementPredicate,
        string value)
    {
        var dom = view(model);
        var (element, handler) = FindFirstHandler(dom, "input", elementPredicate);

        if (handler.WithData is null)
            throw new InvalidOperationException($"Expected input handler on element id={element.Id} to have WithData.");

        var msg = handler.WithData(new InputEventData(value));
        return update(msg, model);
    }

    public static (TModel model, Abies.Command command) DispatchChange<TModel>(
        TModel model,
        Func<TModel, Node> view,
        Func<Abies.Message, TModel, (TModel model, Abies.Command command)> update,
        Func<Element, bool> elementPredicate,
        string value)
    {
        var dom = view(model);
        var (element, handler) = FindFirstHandler(dom, "change", elementPredicate);

        if (handler.WithData is null)
            throw new InvalidOperationException($"Expected change handler on element id={element.Id} to have WithData.");

        var msg = handler.WithData(new InputEventData(value));
        return update(msg, model);
    }

    public static (TModel model, Abies.Command command) DispatchSubmit<TModel>(
        TModel model,
        Func<TModel, Node> view,
        Func<Abies.Message, TModel, (TModel model, Abies.Command command)> update,
        Func<Element, bool> formPredicate)
    {
        var dom = view(model);
        // Abies uses onsubmit(...) which renders data-event-submit
        var (_, handler) = FindFirstHandler(dom, "submit", formPredicate);

        if (handler.Command is null)
            throw new InvalidOperationException("Submit handler must have a concrete Command message.");

        return update(handler.Command, model);
    }

    public static Element FindFirstElement(Node root, Func<Element, bool> predicate)
        => EnumerateElements(root).First(predicate);

    public static IEnumerable<Element> EnumerateElements(Node root)
    {
        if (root is Element el)
        {
            yield return el;
            foreach (var child in el.Children)
            {
                foreach (var descendant in EnumerateElements(child))
                    yield return descendant;
            }
        }
    }

    public static IEnumerable<Text> EnumerateTextNodes(Node root)
    {
        switch (root)
        {
            case Text t:
                yield return t;
                yield break;
            case Element el:
                foreach (var child in el.Children)
                {
                    foreach (var desc in EnumerateTextNodes(child))
                        yield return desc;
                }
                yield break;
            default:
                yield break;
        }
    }

    public static (Element element, Handler handler) FindFirstHandler(Node root, string eventName, Func<Element, bool> elementPredicate)
    {
        var attributeName = $"data-event-{eventName}";

        foreach (var el in EnumerateElements(root))
        {
            if (!elementPredicate(el))
                continue;

            // In Abies.DOM, Handler.Name is the raw event name (e.g. "click"),
            // while the rendered HTML attribute name is "data-event-click".
            var handler = el.Attributes.OfType<Handler>().FirstOrDefault(h => h.Name == eventName)
                       ?? el.Attributes.OfType<Handler>().FirstOrDefault(h => h.Name == attributeName);

            // Note: Handler inherits Attribute and stores the real attribute name in Attribute.Name.
            if (handler is null)
            {
                handler = el.Attributes.OfType<Handler>().FirstOrDefault(h => ((Abies.DOM.Attribute)h).Name == attributeName);
            }

            if (handler is not null)
                return (el, handler);
        }

        throw new InvalidOperationException($"No handler found for event '{eventName}'.");
    }

    public static string Html(Node node) => Abies.DOM.Render.Html(node);
}
