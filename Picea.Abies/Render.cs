using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text;
using Picea.Abies.DOM;

namespace Picea.Abies;

internal static class HtmlSpec
{
    internal static readonly FrozenSet<string> VoidElements = new[]
    {
        "area", "base", "br", "col", "embed",
        "hr", "img", "input", "link", "meta",
        "param", "source", "track", "wbr"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    internal static readonly FrozenSet<string> BooleanAttributes = new[]
    {
        "allowfullscreen", "async", "autofocus", "autoplay", "checked",
        "controls", "default", "defer", "disabled", "formnovalidate",
        "hidden", "inert", "ismap", "itemscope", "loop", "multiple",
        "muted", "nomodule", "novalidate", "open", "playsinline",
        "readonly", "required", "reversed", "selected"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly SearchValues<char> HtmlEncodeChars =
        SearchValues.Create("&<>\"'");

    internal static string Encode(string value) =>
        value.AsSpan().ContainsAny(HtmlEncodeChars)
            ? System.Web.HttpUtility.HtmlEncode(value)
            : value;
}

public static class Render
{
    private static readonly ConcurrentStack<StringBuilder> _stringBuilderPool = new();
    private const int MaxPooledStringBuilderCapacity = 8192;

    private static readonly SearchValues<char> HtmlSpecialChars =
        SearchValues.Create("&<>\"'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendHtmlEncoded(StringBuilder sb, string value)
    {
        if (!value.AsSpan().ContainsAny(HtmlSpecialChars))
        {
            sb.Append(value);
            return;
        }

        sb.Append(System.Web.HttpUtility.HtmlEncode(value));
    }

    private static StringBuilder RentStringBuilder()
    {
        if (_stringBuilderPool.TryPop(out var sb))
        {
            sb.Clear();
            return sb;
        }

        return new StringBuilder(256);
    }

    private static void ReturnStringBuilder(StringBuilder sb)
    {
        if (sb.Capacity <= MaxPooledStringBuilderCapacity)
        {
            _stringBuilderPool.Push(sb);
        }
    }

    public static string Html(Node node)
    {
        var sb = RentStringBuilder();
        try
        {
            RenderNode(node, sb);
            return sb.ToString();
        }
        finally
        {
            ReturnStringBuilder(sb);
        }
    }

    public static string HtmlChildren(Node[] children)
    {
        var sb = RentStringBuilder();
        try
        {
            foreach (var child in children)
            {
                RenderNode(child, sb);
            }

            return sb.ToString();
        }
        finally
        {
            ReturnStringBuilder(sb);
        }
    }

    private static void RenderNode(Node node, StringBuilder sb)
    {
        switch (node)
        {
            case Element element:
                sb.Append('<').Append(element.Tag)
                  .Append(" id=\"").Append(element.Id).Append('"');

                foreach (var attr in element.Attributes)
                {
                    if (HtmlSpec.BooleanAttributes.Contains(attr.Name) &&
                        attr.Value is "true" or "")
                    {
                        sb.Append(' ').Append(attr.Name);
                        continue;
                    }

                    sb.Append(' ').Append(attr.Name).Append("=\"");
                    AppendHtmlEncoded(sb, attr.Value);
                    sb.Append('"');
                }

                if (HtmlSpec.VoidElements.Contains(element.Tag))
                {
                    sb.Append('>');
                    break;
                }

                sb.Append('>');
                foreach (var child in element.Children)
                {
                    RenderNode(child, sb);
                }

                sb.Append("</").Append(element.Tag).Append('>');
                break;

            case Text text:
                AppendHtmlEncoded(sb, text.Value);
                break;

            case RawHtml raw:
                sb.Append("<span id=\"").Append(raw.Id).Append("\">")
                  .Append(raw.Html).Append("</span>");
                break;

            case LazyMemoNode lazyMemo:
                RenderNode(lazyMemo.CachedNode ?? lazyMemo.Evaluate(), sb);
                break;

            case MemoNode memo:
                RenderNode(memo.CachedNode, sb);
                break;
        }
    }
}
