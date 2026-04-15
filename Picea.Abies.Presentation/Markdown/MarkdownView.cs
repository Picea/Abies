using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;

public static class MarkdownView
{
    public static Node Render(string markdown, string keyPrefix)
    {
        var prefix = string.IsNullOrWhiteSpace(keyPrefix) ? "md" : $"md-{keyPrefix}";

        if (!MarkdownParser.TryParse(markdown, out var blocks))
        {
            return div([class_("markdown markdown-fallback")],
            [
                p([], [text(markdown)])
            ]);
        }

        var nodes = new List<Node>();
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            nodes.Add(RenderBlock(block, i, prefix));
        }

        return div([class_("markdown")], nodes.ToArray());
    }

    private static Node RenderBlock(MarkdownBlock block, int index, string prefix) => block switch
    {
        MarkdownParagraph paragraph => p([], RenderInline(paragraph.Text), id: $"{prefix}-p-{index}"),
        MarkdownCodeBlock code => RenderCodeBlock(code, index, prefix),
        MarkdownList list => RenderList(list.Items, index, prefix),
        MarkdownTable table => RenderTable(table, index, prefix),
        _ => p([], [text(string.Empty)], id: $"{prefix}-unknown-{index}")
    };

    private static Node RenderCodeBlock(MarkdownCodeBlock block, int index, string prefix)
    {
        var className = block.Language is not null && block.Language.Length > 0
            ? $"markdown-code language-{block.Language.ToLowerInvariant()}"
            : "markdown-code";

        var children = new List<Node>();
        if (block.Language is not null && block.Language.Length > 0)
        {
            children.Add(div([class_("markdown-code-language")], [text(block.Language)]));
        }

        children.Add(pre([class_(className)], [text(block.Content)]));

        return div([class_("markdown-code-block")], children.ToArray(), id: $"{prefix}-code-{index}");
    }

    private static Node RenderList(IReadOnlyList<MarkdownListItem> items, int index, string prefix)
        => ul([class_("markdown-list")], items.Select((item, i) => RenderListItem(item, $"{index}-{i}", prefix)).ToArray(), id: $"{prefix}-list-{index}");

    private static Node RenderListItem(MarkdownListItem item, string idSuffix, string prefix)
    {
        var children = new List<Node>
        {
            span([], RenderInline(item.Text), id: $"{prefix}-li-text-{idSuffix}")
        };

        if (item.Children.Count > 0)
        {
            children.Add(ul([class_("markdown-list markdown-list-nested")], item.Children.Select((child, i) => RenderListItem(child, $"{idSuffix}-{i}", prefix)).ToArray()));
        }

        return li([], children.ToArray(), id: $"{prefix}-li-{idSuffix}");
    }

    private static Node RenderTable(MarkdownTable markdownTable, int index, string prefix)
        => div([class_("markdown-table-wrapper")],
        [
            table([class_("markdown-table")],
            [
                thead([], [
                    tr([], markdownTable.Headers.Select((header, i) => th([], RenderInline(header), id: $"{prefix}-th-{index}-{i}")).ToArray())
                ]),
                tbody([], markdownTable.Rows.Select((row, rowIndex) =>
                    tr([], row.Select((cell, colIndex) => td([], RenderInline(cell), id: $"{prefix}-td-{index}-{rowIndex}-{colIndex}")).ToArray())
                ).ToArray())
            ], id: $"{prefix}-table-{index}")
        ]);

    private static Node[] RenderInline(string textContent)
    {
        var nodes = new List<Node>();
        var source = textContent ?? string.Empty;

        var i = 0;
        while (i < source.Length)
        {
            if (i + 1 < source.Length && ((source[i] == '*' && source[i + 1] == '*') || (source[i] == '_' && source[i + 1] == '_')))
            {
                var token = source.Substring(i, 2);
                var end = source.IndexOf(token, i + 2, StringComparison.Ordinal);
                if (end > i + 2)
                {
                    var strongText = source[(i + 2)..end];
                    nodes.Add(strong([], [text(strongText)]));
                    i = end + 2;
                    continue;
                }

                nodes.Add(text(source[i].ToString()));
                i++;
                continue;
            }

            var c = source[i];

            if (c == '!' && TryParseImageToken(source, i, out var altText, out var imageSource, out var consumedLength))
            {
                nodes.Add(img([class_("markdown-image"), src(imageSource), alt(altText), loading("lazy"), decoding("async")]));
                i += consumedLength;
                continue;
            }

            if (c == '`')
            {
                var end = source.IndexOf('`', i + 1);
                if (end < 0)
                {
                    nodes.Add(text(source[i..]));
                    break;
                }

                var codeText = source[(i + 1)..end];
                nodes.Add(code([], [text(codeText)]));
                i = end + 1;
                continue;
            }

            if (c is '*' or '_')
            {
                var end = source.IndexOf(c, i + 1);
                if (end > i + 1)
                {
                    var emphasis = source[(i + 1)..end];
                    nodes.Add(em([], [text(emphasis)]));
                    i = end + 1;
                    continue;
                }

                nodes.Add(text(c.ToString()));
                i++;
                continue;
            }

            var next = FindNextSpecial(source, i + 1);
            var chunk = next < 0 ? source[i..] : source[i..next];
            nodes.Add(text(chunk));
            if (next < 0)
            {
                break;
            }

            i = next;
        }

        if (nodes.Count == 0)
        {
            nodes.Add(text(string.Empty));
        }

        return nodes.ToArray();
    }

    private static int FindNextSpecial(string input, int start)
    {
        var bang = input.IndexOf('!', start);
        var tick = input.IndexOf('`', start);
        var star = input.IndexOf('*', start);
        var underscore = input.IndexOf('_', start);

        var candidates = new[] { bang, tick, star, underscore };
        var min = -1;
        foreach (var candidate in candidates)
        {
            if (candidate < 0)
            {
                continue;
            }

            if (min < 0 || candidate < min)
            {
                min = candidate;
            }
        }

        return min;
    }

    private static bool TryParseImageToken(string source, int startIndex, out string altText, out string imageSource, out int consumedLength)
    {
        altText = string.Empty;
        imageSource = string.Empty;
        consumedLength = 0;

        if (startIndex + 3 >= source.Length || source[startIndex] != '!' || source[startIndex + 1] != '[')
        {
            return false;
        }

        var altEnd = source.IndexOf(']', startIndex + 2);
        if (altEnd < 0 || altEnd + 1 >= source.Length || source[altEnd + 1] != '(')
        {
            return false;
        }

        var srcEnd = source.IndexOf(')', altEnd + 2);
        if (srcEnd < 0)
        {
            return false;
        }

        var parsedSource = source[(altEnd + 2)..srcEnd].Trim();
        if (parsedSource.Length == 0)
        {
            return false;
        }

        altText = source[(startIndex + 2)..altEnd];
        imageSource = parsedSource;
        consumedLength = srcEnd - startIndex + 1;
        return true;
    }
}
