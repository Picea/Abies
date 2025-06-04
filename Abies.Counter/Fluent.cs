using System.Runtime.CompilerServices;
using Abies.Html;
using static Abies.Html.Elements;
using Abies.DOM;
using Praefixum;

namespace Abies.Counter;

public static class Fluent
{
    public static Element button(DOM.Attribute[] attributes, Element[] children, [UniqueId(UniqueIdFormat.HtmlId)] string id = "") 
            => element("fluent-button", attributes, children, id);
}
