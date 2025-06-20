using System.Runtime.InteropServices.JavaScript;

namespace Abies
{
    internal static partial class Interop
    {
        [JSImport("forward", "abies.js")]
        public static partial Task Forward(int steps);

        [JSImport("back", "abies.js")]
        public static partial Task Back(int steps);

        [JSImport("go", "abies.js")]
        public static partial Task Go(int steps);

        [JSImport("reload", "abies.js")]
        public static partial Task Reload();

        [JSImport("load", "abies.js")]
        public static partial Task Load(string url);

        [JSImport("pushState", "abies.js")]
        public static partial Task PushState(string url);

        [JSImport("replaceState", "abies.js")]
        public static partial Task ReplaceState(string url);

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
        internal static partial void OnFormSubmit([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> value);
    }
}
