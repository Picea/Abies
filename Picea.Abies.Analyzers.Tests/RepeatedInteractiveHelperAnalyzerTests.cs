using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Picea.Abies.Analyzers.Tests;

public class RepeatedInteractiveHelperAnalyzerTests
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

    private static CSharpAnalyzerTest<RepeatedInteractiveHelperAnalyzer, DefaultVerifier> CreateTest(string testCode)
    {
        var test = new CSharpAnalyzerTest<RepeatedInteractiveHelperAnalyzer, DefaultVerifier>
        {
            TestCode = testCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };
        test.TestState.Sources.Add(("AbiesStubs.cs", AbiesStubs.Source));
        return test;
    }

    [Test]
    public async Task HelperWithImplicitEventIdInsideSelect_ReportsABIES007()
    {
        var test = CreateTest(Wrap("""
                private static Node Row(string item) =>
                    button([onclick(new Clicked(item)), type("button")], [text(item)]);

                public static Node View(string[] items) =>
                    div([], items.Select(item => {|#0:Row(item)|}).ToArray());
            """));

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult("ABIES007", DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Row"));

        await test.RunAsync();
    }

    [Test]
    public async Task HelperWithExplicitEventIdInsideSelect_NoDiagnostic()
    {
        var test = CreateTest(Wrap("""
                private static Node Row(string item) =>
                    button([onclick(new Clicked(item), id: $"click:{item}"), type("button")], [text(item)]);

                public static Node View(string[] items) =>
                    div([], items.Select(item => Row(item)).ToArray());
            """));

        await test.RunAsync();
    }
}
