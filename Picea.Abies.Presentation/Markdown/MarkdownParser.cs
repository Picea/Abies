using System.Text.RegularExpressions;

public static class MarkdownParser
{
    private static readonly Regex _fencedStartRegex = new("^[ ]{0,3}```[ ]*([A-Za-z0-9_+.#-]+)?[ ]*$", RegexOptions.Compiled);
    private static readonly Regex _fencedEndRegex = new("^[ ]{0,3}```[ ]*$", RegexOptions.Compiled);
    private static readonly Regex _bulletRegex = new("^( *)([-*]) +(.*)$", RegexOptions.Compiled);

    public static bool TryParse(string input, out IReadOnlyList<MarkdownBlock> blocks)
    {
        blocks = [];

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var normalized = input.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        var parsed = new List<MarkdownBlock>();

        var i = 0;
        while (i < lines.Length)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
                continue;
            }

            var fenced = TryConsumeFenced(lines, ref i, out var fencedBlock);
            if (fenced == ParseResult.Malformed)
            {
                return false;
            }

            if (fenced == ParseResult.Consumed)
            {
                parsed.Add(fencedBlock!);
                continue;
            }

            if (TryConsumeIndentedCode(lines, ref i, out var indentedBlock))
            {
                parsed.Add(indentedBlock!);
                continue;
            }

            if (TryConsumeList(lines, ref i, out var listBlock, 0))
            {
                parsed.Add(listBlock!);
                continue;
            }

            if (TryConsumeTable(lines, ref i, out var tableBlock))
            {
                parsed.Add(tableBlock!);
                continue;
            }

            if (TryConsumeParagraph(lines, ref i, out var paragraph))
            {
                parsed.Add(paragraph!);
                continue;
            }

            i++;
        }

        blocks = parsed;
        return true;
    }

    private static ParseResult TryConsumeFenced(string[] lines, ref int i, out MarkdownCodeBlock? block)
    {
        block = null;

        var match = _fencedStartRegex.Match(lines[i]);
        if (!match.Success)
        {
            return ParseResult.NotConsumed;
        }

        var language = match.Groups[1].Success ? match.Groups[1].Value : null;
        i++;

        var codeLines = new List<string>();
        while (i < lines.Length)
        {
            if (_fencedEndRegex.IsMatch(lines[i]))
            {
                i++;
                block = new MarkdownCodeBlock(string.Join("\n", codeLines), language, true);
                return ParseResult.Consumed;
            }

            codeLines.Add(lines[i]);
            i++;
        }

        return ParseResult.Malformed;
    }

    private static bool TryConsumeIndentedCode(string[] lines, ref int i, out MarkdownCodeBlock? block)
    {
        block = null;

        if (!IsIndentedCodeLine(lines[i]))
        {
            return false;
        }

        var codeLines = new List<string>();
        while (i < lines.Length)
        {
            var line = lines[i];
            if (line.Length == 0)
            {
                codeLines.Add(string.Empty);
                i++;
                continue;
            }

            if (!IsIndentedCodeLine(line))
            {
                break;
            }

            codeLines.Add(line.Length >= 4 ? line[4..] : string.Empty);
            i++;
        }

        while (codeLines.Count > 0 && codeLines[^1].Length == 0)
        {
            codeLines.RemoveAt(codeLines.Count - 1);
        }

        block = new MarkdownCodeBlock(string.Join("\n", codeLines), null, false);
        return true;
    }

    private static bool IsIndentedCodeLine(string line) => line.StartsWith("    ", StringComparison.Ordinal);

    private static bool TryConsumeTable(string[] lines, ref int i, out MarkdownTable? table)
    {
        table = null;

        if (i + 1 >= lines.Length)
        {
            return false;
        }

        var headerCells = SplitTableRow(lines[i]);
        if (headerCells is null || headerCells.Count == 0)
        {
            return false;
        }

        if (!IsTableSeparatorLine(lines[i + 1], headerCells.Count))
        {
            return false;
        }

        i += 2;
        var rows = new List<IReadOnlyList<string>>();

        while (i < lines.Length)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                break;
            }

            var cells = SplitTableRow(lines[i]);
            if (cells is null || cells.Count != headerCells.Count)
            {
                break;
            }

            rows.Add(cells);
            i++;
        }

        table = new MarkdownTable(headerCells, rows);
        return true;
    }

    private static List<string>? SplitTableRow(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || !trimmed.Contains('|', StringComparison.Ordinal))
        {
            return null;
        }

        if (trimmed.StartsWith("|", StringComparison.Ordinal))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.EndsWith("|", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^1];
        }

        var cells = trimmed
            .Split('|')
            .Select(static cell => cell.Trim())
            .ToList();

        return cells.Count > 0 ? cells : null;
    }

    private static bool IsTableSeparatorLine(string line, int expectedColumns)
    {
        var cells = SplitTableRow(line);
        if (cells is null || cells.Count != expectedColumns)
        {
            return false;
        }

        return cells.All(static cell => IsTableSeparatorCell(cell));
    }

    private static bool IsTableSeparatorCell(string cell)
    {
        if (string.IsNullOrWhiteSpace(cell))
        {
            return false;
        }

        var core = cell.Trim();

        if (core.StartsWith(":", StringComparison.Ordinal))
        {
            core = core[1..];
        }

        if (core.EndsWith(":", StringComparison.Ordinal))
        {
            core = core[..^1];
        }

        return core.Length >= 3 && core.All(static c => c == '-');
    }

    private static bool TryConsumeList(string[] lines, ref int i, out MarkdownList? list, int expectedIndent)
    {
        list = null;

        if (!TryParseBulletLine(lines[i], out var indent, out var text) || indent != expectedIndent)
        {
            return false;
        }

        var items = new List<MarkdownListItem>();

        while (i < lines.Length)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
                break;
            }

            if (!TryParseBulletLine(lines[i], out indent, out text))
            {
                break;
            }

            if (indent < expectedIndent)
            {
                break;
            }

            if (indent > expectedIndent)
            {
                if (items.Count == 0)
                {
                    return false;
                }

                var nestedIndex = i;
                if (!TryConsumeList(lines, ref nestedIndex, out var nested, indent) || nested is null)
                {
                    return false;
                }

                var last = items[^1];
                var mergedChildren = last.Children.Concat(nested.Items).ToArray();
                items[^1] = last with { Children = mergedChildren };
                i = nestedIndex;
                continue;
            }

            items.Add(new MarkdownListItem(text, []));
            i++;
        }

        list = new MarkdownList(items);
        return true;
    }

    private static bool TryParseBulletLine(string line, out int indent, out string text)
    {
        indent = 0;
        text = string.Empty;

        var match = _bulletRegex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        var spaces = match.Groups[1].Value.Length;
        if (spaces % 2 != 0)
        {
            return false;
        }

        indent = spaces;
        text = match.Groups[3].Value;
        return true;
    }

    private static bool TryConsumeParagraph(string[] lines, ref int i, out MarkdownParagraph? paragraph)
    {
        paragraph = null;

        var paragraphLines = new List<string>();

        while (i < lines.Length)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            if (_fencedStartRegex.IsMatch(line) || IsIndentedCodeLine(line) || TryParseBulletLine(line, out _, out _))
            {
                break;
            }

            // Keep standalone strong lines as their own paragraph, even without an empty separator.
            if (paragraphLines.Count > 0 && IsStandaloneStrongLine(line))
            {
                break;
            }

            paragraphLines.Add(line);
            i++;
        }

        if (paragraphLines.Count == 0)
        {
            return false;
        }

        paragraph = new MarkdownParagraph(string.Join("\n", paragraphLines));
        return true;
    }

    private static bool IsStandaloneStrongLine(string line)
    {
        var trimmed = line.Trim();
        return (trimmed.StartsWith("**", StringComparison.Ordinal) && trimmed.EndsWith("**", StringComparison.Ordinal) && trimmed.Length > 4)
            || (trimmed.StartsWith("__", StringComparison.Ordinal) && trimmed.EndsWith("__", StringComparison.Ordinal) && trimmed.Length > 4);
    }

    private enum ParseResult
    {
        NotConsumed,
        Consumed,
        Malformed
    }
}
