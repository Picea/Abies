using Abies.DOM;
using Praefixum;
using static Abies.Html.Elements;

namespace Abies.Counter;

public static class Fluent
{
    public static Element button(DOM.Attribute[] attributes, Element[] children, [UniqueId(UniqueIdFormat.HtmlId)] string id = "")
            => element("fluent-button", attributes, children, id);
}
