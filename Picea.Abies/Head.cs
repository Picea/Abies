namespace Picea.Abies;

public interface HeadContent
{
    string Key { get; }
    string ToHtml();

    public sealed record Meta(string Name, string Content) : HeadContent
    {
        public string Key => $"meta:{Name}";

        public string ToHtml() =>
            $"""<meta name="{HtmlSpec.Encode(Name)}" content="{HtmlSpec.Encode(Content)}" data-abies-head="{HtmlSpec.Encode(Key)}">""";
    }

    public sealed record MetaProperty(string Property, string Content) : HeadContent
    {
        public string Key => $"property:{Property}";

        public string ToHtml() =>
            $"""<meta property="{HtmlSpec.Encode(Property)}" content="{HtmlSpec.Encode(Content)}" data-abies-head="{HtmlSpec.Encode(Key)}">""";
    }

    public sealed record Link(string Rel, string Href, string? As = null) : HeadContent
    {
        public string Key => $"link:{Rel}:{Href}";

        public string ToHtml()
        {
            var asAttr = As is not null ? $""" as="{HtmlSpec.Encode(As)}"""" : "";
            return $"""<link rel="{HtmlSpec.Encode(Rel)}" href="{HtmlSpec.Encode(Href)}"{asAttr} data-abies-head="{HtmlSpec.Encode(Key)}">""";
        }
    }

    public sealed record Script(string Type, string Content) : HeadContent
    {
        public string Key => $"script:{Type}";

        public string ToHtml() =>
            $"""<script type="{HtmlSpec.Encode(Type)}" data-abies-head="{HtmlSpec.Encode(Key)}">{Content}</script>""";
    }

    public sealed record Base(string Href) : HeadContent
    {
        public string Key => "base";

        public string ToHtml() =>
            $"""<base href="{HtmlSpec.Encode(Href)}" data-abies-head="{HtmlSpec.Encode(Key)}">""";
    }
}

public static class Head
{
    public static HeadContent meta(string name, string content) =>
        new HeadContent.Meta(name, content);

    public static HeadContent og(string property, string content) =>
        new HeadContent.MetaProperty($"og:{property}", content);

    public static HeadContent twitter(string name, string content) =>
        new HeadContent.MetaProperty($"twitter:{name}", content);

    public static HeadContent property(string property, string content) =>
        new HeadContent.MetaProperty(property, content);

    public static HeadContent canonical(string href) =>
        new HeadContent.Link("canonical", href);

    public static HeadContent stylesheet(string href) =>
        new HeadContent.Link("stylesheet", href);

    public static HeadContent preload(string href, string @as) =>
        new HeadContent.Link("preload", href, @as);

    public static HeadContent link(string rel, string href) =>
        new HeadContent.Link(rel, href);

    public static HeadContent @base(string href) =>
        new HeadContent.Base(href);

    public static HeadContent jsonLd(object data) =>
        new HeadContent.Script("application/ld+json",
            System.Text.Json.JsonSerializer.Serialize(data));
}
