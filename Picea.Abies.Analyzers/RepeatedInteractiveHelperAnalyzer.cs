using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Picea.Abies.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RepeatedInteractiveHelperAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.RepeatedInteractiveHelperMissingStableIds);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (!HandlerIdentityAnalysis.IsInsideRepeatedRenderPath(invocation, context.SemanticModel))
            return;

        if (HandlerIdentityAnalysis.IsAbiesEventInvocation(invocation, context.SemanticModel, out _))
            return;

        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            return;

        if (!HandlerIdentityAnalysis.ReturnsNode(method))
            return;

        if (method.DeclaringSyntaxReferences.Length == 0)
            return;

        if (!HandlerIdentityAnalysis.ContainsEventHandlerWithoutExplicitId(
                method,
                context.Compilation,
                context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.RepeatedInteractiveHelperMissingStableIds,
                invocation.GetLocation(),
                method.Name));
    }
}
