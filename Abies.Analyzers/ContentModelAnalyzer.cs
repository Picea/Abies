using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Abies.Analyzers;

/// <summary>
/// Roslyn analyzer that detects HTML content model violations in Abies.Html element trees.
/// </summary>
/// <remarks>
/// Detected issues:
/// <list type="bullet">
/// <item>ABIES002: Block (flow) element nested inside an inline (phrasing-only) element</item>
/// </list>
/// 
/// This analyzer walks the invocation tree to detect when flow content elements
/// (like div, section, table) are placed as children of phrasing-only elements
/// (like span, strong, em, h1-h6, p).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContentModelAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.BlockInsideInline);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Resolve the parent element name
        var parentElementName = AnalysisHelpers.GetElementName(invocation, context.SemanticModel);
        if (parentElementName == null)
        {
            return;
        }

        // Only check elements that are phrasing-only parents
        if (!HtmlSpec.PhrasingOnlyParents.Contains(parentElementName))
        {
            return;
        }

        // Get the child element names from the children argument
        var children = AnalysisHelpers.GetChildElementNames(invocation, context.SemanticModel);

        foreach (var (childName, childLocation) in children)
        {
            // Check if this child is a flow content element (block-level)
            if (HtmlSpec.FlowContentElements.Contains(childName))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.BlockInsideInline,
                        childLocation,
                        childName,
                        parentElementName));
            }
        }
    }
}
