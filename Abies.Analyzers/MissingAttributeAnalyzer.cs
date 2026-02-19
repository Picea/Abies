using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Abies.Analyzers;

/// <summary>
/// Roslyn analyzer that detects missing required and recommended HTML attributes
/// on Abies.Html.Elements factory function calls.
/// </summary>
/// <remarks>
/// Detected issues:
/// <list type="bullet">
/// <item>ABIES001: img() missing alt attribute (Warning)</item>
/// <item>ABIES003: a() missing href attribute (Info)</item>
/// <item>ABIES004: button() missing type attribute (Info)</item>
/// <item>ABIES005: input() missing type attribute (Info)</item>
/// </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingAttributeAnalyzer : DiagnosticAnalyzer
{
    // Map diagnostic IDs to their descriptors for quick lookup
    private static readonly ImmutableDictionary<string, DiagnosticDescriptor> _descriptorMap =
        ImmutableDictionary.CreateRange(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, DiagnosticDescriptor>(
                "ABIES001", DiagnosticDescriptors.ImgMissingAlt),
            new System.Collections.Generic.KeyValuePair<string, DiagnosticDescriptor>(
                "ABIES003", DiagnosticDescriptors.AnchorMissingHref),
            new System.Collections.Generic.KeyValuePair<string, DiagnosticDescriptor>(
                "ABIES004", DiagnosticDescriptors.ButtonMissingType),
            new System.Collections.Generic.KeyValuePair<string, DiagnosticDescriptor>(
                "ABIES005", DiagnosticDescriptors.InputMissingType),
        });

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.ImgMissingAlt,
            DiagnosticDescriptors.AnchorMissingHref,
            DiagnosticDescriptors.ButtonMissingType,
            DiagnosticDescriptors.InputMissingType);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Resolve the element name from the invocation
        var elementName = AnalysisHelpers.GetElementName(invocation, context.SemanticModel);
        if (elementName == null)
        {
            return;
        }

        // Check required attributes (Warning severity)
        if (HtmlSpec.RequiredAttributes.TryGetValue(elementName, out var requiredAttrs))
        {
            var presentAttrs = AnalysisHelpers.GetAttributeNames(invocation, context.SemanticModel);

            foreach (var req in requiredAttrs)
            {
                if (!presentAttrs.Contains(req.AttributeName) &&
                    _descriptorMap.TryGetValue(req.DiagnosticId, out var descriptor))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(descriptor, invocation.GetLocation()));
                }
            }
        }

        // Check recommended attributes (Info severity)
        if (HtmlSpec.RecommendedAttributes.TryGetValue(elementName, out var recommendedAttrs))
        {
            var presentAttrs = AnalysisHelpers.GetAttributeNames(invocation, context.SemanticModel);

            foreach (var rec in recommendedAttrs)
            {
                if (!presentAttrs.Contains(rec.AttributeName) &&
                    _descriptorMap.TryGetValue(rec.DiagnosticId, out var descriptor))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(descriptor, invocation.GetLocation()));
                }
            }
        }
    }
}
