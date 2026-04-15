using System.Collections.Generic;

public abstract record MarkdownBlock;

public sealed record MarkdownParagraph(string Text) : MarkdownBlock;

public sealed record MarkdownCodeBlock(string Content, string? Language, bool IsFenced) : MarkdownBlock;

public sealed record MarkdownList(IReadOnlyList<MarkdownListItem> Items) : MarkdownBlock;

public sealed record MarkdownListItem(string Text, IReadOnlyList<MarkdownListItem> Children);

public sealed record MarkdownTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows) : MarkdownBlock;
