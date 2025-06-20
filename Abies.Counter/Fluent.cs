using System.Runtime.CompilerServices;
using Abies.Html;
using static Abies.Html.Elements;
using Abies.DOM;

namespace Abies.Counter;

public static class Fluent
{
    public static Element button(Attribute[] attributes, Element[] children, [CallerLineNumber] int id = 0) 
            => element("fluent-button", attributes, children, id);
}
