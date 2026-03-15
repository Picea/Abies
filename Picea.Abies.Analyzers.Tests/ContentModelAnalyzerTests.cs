using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Picea.Abies.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="ContentModelAnalyzer"/> — verifies that ABIES002 fires correctly.
/// </summary>
public class ContentModelAnalyzerTests
{
    private const string Preamble = """
        using Picea.Abies.Html;
        using static Picea.Abies.Html.Elements;
        using static Picea.Abies.Html.Attributes;
        using Picea.Abies.DOM;
        
        namespace TestApp;
        
        public static class TestView
        {
        """;

    private const string Postamble = """
        }
        """;

    private static string WrapInView(string code) => Preamble + code + Postamble;

    private static CSharpAnalyzerTest<ContentModelAnalyzer, DefaultVerifier> CreateTest(string testCode)
    {
        var test = new CSharpAnalyzerTest<ContentModelAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };
        test.TestState.Sources.Add(("AbiesStubs.cs", AbiesStubs.Source));
        return test;
    }

    // =========================================================================
    // ABIES002: Block inside inline
    // =========================================================================

    [Test]
    public async Task DivInsideSpan_ReportsABIES002()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    span([], [{|#0:div([], [text("oops")])|} ]);
            """));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES002", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("div", "span"));

        await test.RunAsync();
    }

    [Test]
    public async Task SectionInsideStrong_ReportsABIES002()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    strong([], [{|#0:section([], [text("wrong")])|} ]);
            """));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES002", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("section", "strong"));

        await test.RunAsync();
    }

    [Test]
    public async Task TableInsideH1_ReportsABIES002()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    h1([], [{|#0:table([], [tr([], [td([], [text("data")])])])|} ]);
            """));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES002", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("table", "h1"));

        await test.RunAsync();
    }

    // =========================================================================
    // Valid nesting — should NOT report
    // =========================================================================

    [Test]
    public async Task SpanInsideDiv_NoDiagnostic()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    div([], [span([], [text("ok")])]);
            """));

        await test.RunAsync();
    }

    [Test]
    public async Task DivInsideDiv_NoDiagnostic()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    div([], [div([], [text("ok")])]);
            """));

        await test.RunAsync();
    }

    [Test]
    public async Task SpanInsideSpan_NoDiagnostic()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    span([], [strong([], [em([], [text("ok")])])]);
            """));

        await test.RunAsync();
    }

    [Test]
    public async Task TextInsideParagraph_NoDiagnostic()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    p([], [text("hello")]);
            """));

        await test.RunAsync();
    }
}