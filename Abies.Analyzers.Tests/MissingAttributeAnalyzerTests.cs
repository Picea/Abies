using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Abies.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="MissingAttributeAnalyzer"/> — verifies that ABIES001, ABIES003-ABIES005 fire correctly.
/// </summary>
public class MissingAttributeAnalyzerTests
{
    // Preamble code that sets up the using directives and namespace context
    private const string Preamble = """
        using Abies.Html;
        using static Abies.Html.Elements;
        using static Abies.Html.Attributes;
        using Abies.DOM;
        
        namespace TestApp;
        
        public static class TestView
        {
        """;

    private const string Postamble = """
        }
        """;

    private static string WrapInView(string code) => Preamble + code + Postamble;

    private static CSharpAnalyzerTest<MissingAttributeAnalyzer, DefaultVerifier> CreateTest(string testCode)
    {
        var test = new CSharpAnalyzerTest<MissingAttributeAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };
        test.TestState.Sources.Add(("AbiesStubs.cs", AbiesStubs.Source));
        return test;
    }

    // =========================================================================
    // ABIES001: img() missing alt
    // =========================================================================

    [Fact]
    public async Task ImgWithoutAlt_ReportsABIES001()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    {|#0:img([src("image.jpg")])|};
            """));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ABIES001", DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task ImgWithAlt_NoDiagnostic()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    img([src("image.jpg"), alt("A photo")]);
            """));

        await test.RunAsync();
    }

    [Fact]
    public async Task ImgWithEmptyAlt_NoDiagnostic()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    img([src("image.jpg"), alt("")]);
            """));

        await test.RunAsync();
    }

    // =========================================================================
    // ABIES003: a() missing href
    // =========================================================================

    [Fact]
    public async Task AnchorWithoutHref_ReportsABIES003()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    {|#0:a([class_("nav-link")], [text("Click me")])|};
            """));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ABIES003", DiagnosticSeverity.Info).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task AnchorWithHref_NoDiagnostic()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    a([class_("nav-link"), href("/home")], [text("Home")]);
            """));

        await test.RunAsync();
    }

    // =========================================================================
    // ABIES004: button() missing type
    // =========================================================================

    [Fact]
    public async Task ButtonWithoutType_ReportsABIES004()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    {|#0:button([class_("btn")], [text("Click")])|};
            """));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ABIES004", DiagnosticSeverity.Info).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task ButtonWithType_NoDiagnostic()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    button([class_("btn"), type("button")], [text("Click")]);
            """));

        await test.RunAsync();
    }

    // =========================================================================
    // ABIES005: input() missing type
    // =========================================================================

    [Fact]
    public async Task InputWithoutType_ReportsABIES005()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    {|#0:input([placeholder("Enter text")])|};
            """));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("ABIES005", DiagnosticSeverity.Info).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task InputWithType_NoDiagnostic()
    {
        var test = CreateTest(WrapInView("""
                public static Node View() =>
                    input([type("email"), placeholder("Enter email")]);
            """));

        await test.RunAsync();
    }

    // =========================================================================
    // Edge cases
    // =========================================================================

    [Fact]
    public async Task NonAbiesCode_NoDiagnostic()
    {
        // Code that doesn't use Abies elements should produce no diagnostics
        var test = new CSharpAnalyzerTest<MissingAttributeAnalyzer, DefaultVerifier>
        {
            TestCode = """
                namespace TestApp;
                
                public static class Foo
                {
                    public static string img(string x) => x;
                    public static string Test() => img("test");
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        await test.RunAsync();
    }
}
