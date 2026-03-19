using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Picea.Abies.Analyzers.Tests;

public class AbiesCodeFixProviderTests
{
    private const string Preamble = """
        using System.Linq;
        using Picea.Abies.Html;
        using static Picea.Abies.Html.Elements;
        using static Picea.Abies.Html.Attributes;
        using static Picea.Abies.Html.Events;
        using Picea.Abies.DOM;

        namespace TestApp;

        public sealed record Clicked(string Id) : Message;

        public static class TestView
        {
        """;

    private const string Postamble = """
        }
        """;

    private static string Wrap(string code) => Preamble + code + Postamble;

    private static CSharpCodeFixTest<MissingAttributeAnalyzer, AbiesCodeFixProvider, DefaultVerifier>
        CreateMissingAttributeFixTest(string testCode, string fixedCode)
    {
        var test = new CSharpCodeFixTest<MissingAttributeAnalyzer, AbiesCodeFixProvider, DefaultVerifier>
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.Sources.Add(("AbiesStubs.cs", AbiesStubs.Source));
        test.FixedState.Sources.Add(("AbiesStubs.cs", AbiesStubs.Source));
        return test;
    }

    private static CSharpCodeFixTest<RepeatedHandlerIdentityAnalyzer, AbiesCodeFixProvider, DefaultVerifier>
        CreateRepeatedHandlerFixTest(string testCode, string fixedCode)
    {
        var test = new CSharpCodeFixTest<RepeatedHandlerIdentityAnalyzer, AbiesCodeFixProvider, DefaultVerifier>
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.Sources.Add(("AbiesStubs.cs", AbiesStubs.Source));
        test.FixedState.Sources.Add(("AbiesStubs.cs", AbiesStubs.Source));
        return test;
    }

    [Test]
    public async Task AnchorWithoutHref_CodeFixAddsHref()
    {
        var testCode = Wrap("""
                public static Node View() =>
                    {|#0:a([class_("nav-link")], [text("Click")])|};
            """);

        var fixedCode = Wrap("""
                public static Node View() =>
                    a([class_("nav-link"), href("#")], [text("Click")]);
            """);

        var test = CreateMissingAttributeFixTest(testCode, fixedCode);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("ABIES003", Microsoft.CodeAnalysis.DiagnosticSeverity.Info).WithLocation(0));
        test.CodeActionEquivalenceKey = "ABIES003";
        await test.RunAsync();
    }

    [Test]
    public async Task ImgWithoutAlt_CodeFixAddsAlt()
    {
        var testCode = Wrap("""
                public static Node View() =>
                    {|#0:img([src("image.jpg")])|};
            """);

        var fixedCode = Wrap("""
                public static Node View() =>
                    img([src("image.jpg"), alt("")]);
            """);

        var test = CreateMissingAttributeFixTest(testCode, fixedCode);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("ABIES001", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning).WithLocation(0));
        test.CodeActionEquivalenceKey = "ABIES001";
        await test.RunAsync();
    }

    [Test]
    public async Task ButtonWithoutType_CodeFixAddsTypeButton()
    {
        var testCode = Wrap("""
                public static Node View() =>
                    {|#0:button([class_("btn")], [text("Click")])|};
            """);

        var fixedCode = Wrap("""
                public static Node View() =>
                    button([class_("btn"), type("button")], [text("Click")]);
            """);

        var test = CreateMissingAttributeFixTest(testCode, fixedCode);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("ABIES004", Microsoft.CodeAnalysis.DiagnosticSeverity.Info).WithLocation(0));
        test.CodeActionEquivalenceKey = "ABIES004";
        await test.RunAsync();
    }

    [Test]
    public async Task InputWithoutType_CodeFixAddsTypeText()
    {
        var testCode = Wrap("""
                public static Node View() =>
                    {|#0:input([placeholder("Email")])|};
            """);

        var fixedCode = Wrap("""
                public static Node View() =>
                    input([placeholder("Email"), type("text")]);
            """);

        var test = CreateMissingAttributeFixTest(testCode, fixedCode);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("ABIES005", Microsoft.CodeAnalysis.DiagnosticSeverity.Info).WithLocation(0));
        test.CodeActionEquivalenceKey = "ABIES005";
        await test.RunAsync();
    }

    [Test]
    public async Task RepeatedOnclickWithoutId_CodeFixAddsNamedIdArgument()
    {
        var testCode = Wrap("""
                public static Node View(string[] items) =>
                    div([], items.Select(item =>
                        button([{|#0:onclick(new Clicked(item))|}, type("button")], [text(item)])
                    ).ToArray());
            """);

        var fixedCode = Wrap("""
                public static Node View(string[] items) =>
                    div([], items.Select(item =>
                        button([onclick(new Clicked(item), id: $"onclick:{item}"), type("button")], [text(item)])
                    ).ToArray());
            """);

        var test = CreateRepeatedHandlerFixTest(testCode, fixedCode);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES006", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("onclick"));
        test.CodeActionEquivalenceKey = "ABIES006_AddId";
        await test.RunAsync();
    }
}
