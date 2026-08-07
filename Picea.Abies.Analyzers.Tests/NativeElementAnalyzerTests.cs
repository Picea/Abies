using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Picea.Abies.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="NativeElementAnalyzer"/> — ABIES008 (reserved void
/// element tag name) and ABIES009 (non-Element node in a native tree).
/// </summary>
public class NativeElementAnalyzerTests
{
    private const string Preamble = """
        using Picea.Abies.DOM;
        using Picea.Abies.Native;
        using static Picea.Abies.Native.Elements;

        namespace TestApp;

        public static class TestView
        {
        """;

    private const string Postamble = """
        }
        """;

    private static string WrapInView(string code) => Preamble + code + Postamble;

    private static CSharpAnalyzerTest<NativeElementAnalyzer, DefaultVerifier> CreateTest(string testCode)
    {
        var test = new CSharpAnalyzerTest<NativeElementAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };
        test.TestState.Sources.Add(("AbiesStubs.cs", AbiesStubs.Source));
        return test;
    }

    // =========================================================================
    // ABIES008: reserved void element tag names
    // =========================================================================

    [Test]
    public async Task NativeTagNamedInput_ReportsABIES008()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    element({|#0:"Input"|}, [], []);
        """));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES008", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("Input"));
        await test.RunAsync();
    }

    /// <summary>The diff matches void element names case-insensitively.</summary>
    [Test]
    public async Task NativeTagNamedLink_LowerCase_ReportsABIES008()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    element({|#0:"link"|}, [], []);
        """));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES008", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("link"));
        await test.RunAsync();
    }

    [Test]
    public async Task NativeTagNamedSource_ReportsABIES008()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    element({|#0:"Source"|}, [], []);
        """));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES008", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("Source"));
        await test.RunAsync();
    }

    /// <summary>"Image" is a WinUI control and is not an HTML void element.</summary>
    [Test]
    public async Task NativeTagNamedImage_IsClean()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    element("Image", [], []);
        """));
        await test.RunAsync();
    }

    [Test]
    public async Task OrdinaryNativeTag_IsClean()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    element("StackPanel", [], []);
        """));
        await test.RunAsync();
    }

    /// <summary>A computed tag cannot be checked statically; no false positive.</summary>
    [Test]
    public async Task NonLiteralTag_IsNotReported()
    {
        var test = CreateTest(WrapInView("""
                public static Node View(string tag) =>
                    element(tag, [], []);
        """));
        await test.RunAsync();
    }

    // =========================================================================
    // ABIES009: non-Element nodes in a native tree
    // =========================================================================

    [Test]
    public async Task TextNodeInsideNativeElement_ReportsABIES009()
    {
        var test = CreateTest("""
        using Picea.Abies.DOM;
        using Picea.Abies.Native;
        using static Picea.Abies.Native.Elements;
        using static Picea.Abies.Html.Elements;

        namespace TestApp;

        public static class TestView
        {
                public static Node View() =>
                    StackPanel([], [{|#0:text("oops")|}]);
        }
        """);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES009", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("text"));
        await test.RunAsync();
    }

    [Test]
    public async Task RawNodeInsideNativeElement_ReportsABIES009()
    {
        var test = CreateTest("""
        using Picea.Abies.DOM;
        using Picea.Abies.Native;
        using static Picea.Abies.Native.Elements;
        using static Picea.Abies.Html.Elements;

        namespace TestApp;

        public static class TestView
        {
                public static Node View() =>
                    Border([], {|#0:raw("<b>no</b>")|});
        }
        """);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES009", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("raw"));
        await test.RunAsync();
    }

    /// <summary>
    /// Nesting must produce exactly one diagnostic, at the offending node —
    /// not one per enclosing native element.
    /// </summary>
    [Test]
    public async Task TextNestedTwoDeep_ReportsOnceAtTheNode()
    {
        var test = CreateTest("""
        using Picea.Abies.DOM;
        using Picea.Abies.Native;
        using static Picea.Abies.Native.Elements;
        using static Picea.Abies.Html.Elements;

        namespace TestApp;

        public static class TestView
        {
                public static Node View() =>
                    StackPanel([], [StackPanel([], [{|#0:text("oops")|}])]);
        }
        """);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES009", DiagnosticSeverity.Error)
                .WithLocation(0)
                .WithArguments("text"));
        await test.RunAsync();
    }

    /// <summary>text() in an ordinary HTML tree is entirely normal.</summary>
    [Test]
    public async Task TextInsideHtmlElement_IsClean()
    {
        var test = CreateTest("""
        using Picea.Abies.DOM;
        using static Picea.Abies.Html.Elements;

        namespace TestApp;

        public static class TestView
        {
                public static Node View() =>
                    div([], [text("fine")]);
        }
        """);
        await test.RunAsync();
    }

    /// <summary>A native tree carrying text as a property is the correct shape.</summary>
    [Test]
    public async Task NativeTreeWithTextBlock_IsClean()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    StackPanel([], [TextBlock([], "fine")]);
        """));
        await test.RunAsync();
    }
}
