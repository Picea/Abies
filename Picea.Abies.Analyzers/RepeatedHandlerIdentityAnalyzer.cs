using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Picea.Abies.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RepeatedHandlerIdentityAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.RepeatedHandlerMissingStableId);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!HandlerIdentityAnalysis.IsAbiesEventInvocation(invocation, context.SemanticModel, out var method))
            return;

        if (HandlerIdentityAnalysis.HasExplicitIdArgument(invocation, method))
            return;

        if (!HandlerIdentityAnalysis.IsInsideRepeatedRenderPath(invocation, context.SemanticModel))
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.RepeatedHandlerMissingStableId,
                invocation.GetLocation(),
                method.Name));
    }
}
