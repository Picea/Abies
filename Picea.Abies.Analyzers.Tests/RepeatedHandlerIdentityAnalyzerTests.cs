using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Picea.Abies.Analyzers.Tests;

public class RepeatedHandlerIdentityAnalyzerTests
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

    private static CSharpAnalyzerTest<RepeatedHandlerIdentityAnalyzer, DefaultVerifier> CreateTest(string testCode)
    {
        var test = new CSharpAnalyzerTest<RepeatedHandlerIdentityAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };
        test.TestState.Sources.Add(("AbiesStubs.cs", AbiesStubs.Source));
        return test;
    }

    [Test]
    public async Task OnclickInsideSelectWithoutExplicitId_ReportsABIES006()
    {
        var test = CreateTest(Wrap("""
                public static Node View(string[] items) =>
                    div([], items.Select(item =>
                        button([{|#0:onclick(new Clicked(item))|}, type("button")], [text(item)])
                    ).ToArray());
            """));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES006", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("onclick"));

        await test.RunAsync();
    }

    [Test]
    public async Task OnclickInsideSelectWithExplicitId_NoDiagnostic()
    {
        var test = CreateTest(Wrap("""
                public static Node View(string[] items) =>
                    div([], items.Select(item =>
                        button([onclick(new Clicked(item), $"click:{item}"), type("button")], [text(item)])
                    ).ToArray());
            """));

        await test.RunAsync();
    }

    [Test]
    public async Task OnclickOutsideRepeatedPath_NoDiagnostic()
    {
        var test = CreateTest(Wrap("""
                public static Node View() =>
                    button([onclick(new Clicked("one")), type("button")], [text("Single")]);
            """));

        await test.RunAsync();
    }

    [Test]
    public async Task OninputInsideForeachWithoutExplicitId_ReportsABIES006()
    {
        var test = CreateTest(Wrap("""
                public static Node View(string[] items)
                {
                    var nodes = new System.Collections.Generic.List<Node>();
                    foreach (var item in items)
                    {
                        nodes.Add(input([{|#0:oninput(_ => new Clicked(item))|}]));
                    }

                    return div([], nodes.ToArray());
                }
            """));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES006", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("oninput"));

        await test.RunAsync();
    }

    [Test]
    public async Task OnclickInsideForLoopWithoutExplicitId_ReportsABIES006()
    {
        var test = CreateTest(Wrap("""
                public static Node View(string[] items)
                {
                    var nodes = new System.Collections.Generic.List<Node>();
                    for (var i = 0; i < items.Length; i++)
                    {
                        nodes.Add(button([{|#0:onclick(new Clicked(items[i]))|}, type("button")], [text(items[i])]));
                    }

                    return div([], nodes.ToArray());
                }
            """));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES006", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("onclick"));

        await test.RunAsync();
    }
}
