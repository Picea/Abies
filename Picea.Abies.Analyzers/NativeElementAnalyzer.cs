using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Picea.Abies.Analyzers;

/// <summary>
/// Enforces the two structural rules that keep the HTML fallback paths out of
/// native element trees (ADR-027). Both were runtime tripwires in the patch
/// interpreter; this promotes them to compile time, per ADR-021's preference
/// for analyzers over a more restrictive typed DSL.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>ABIES008: a native tag that collides with an HTML void element name.</item>
/// <item>ABIES009: a Text or RawHtml node inside a native element tree.</item>
/// </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NativeElementAnalyzer : DiagnosticAnalyzer
{
    private const string NativeElementsType = "Picea.Abies.Native.Elements";
    private const string HtmlElementsType = "Picea.Abies.Html.Elements";

    /// <summary>
    /// The HTML void elements, matched case-insensitively exactly as
    /// <c>HtmlSpec.VoidElements</c> does in the diff.
    /// </summary>
    private static readonly ImmutableHashSet<string> VoidElementNames =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "area", "base", "br", "col", "embed",
            "hr", "img", "input", "link", "meta",
            "param", "source", "track", "wbr");

    /// <summary>Html factories that produce non-Element nodes.</summary>
    private static readonly ImmutableHashSet<string> NonElementFactories =
        ImmutableHashSet.Create(StringComparer.Ordinal, "text", "raw");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.NativeReservedTagName,
            DiagnosticDescriptors.NativeNonElementChild);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
        {
            return;
        }

        var containingType = method.ContainingType?.ToDisplayString();

        if (containingType == NativeElementsType)
        {
            AnalyzeReservedTagName(context, invocation, method);
        }
        else if (containingType == HtmlElementsType && NonElementFactories.Contains(method.Name))
        {
            AnalyzeNonElementChild(context, invocation, method);
        }
    }

    /// <summary>
    /// ABIES008 — <c>element("Input", ...)</c> and friends. The diff skips child
    /// diffing for void element tags, so such a subtree silently stops updating.
    /// </summary>
    private static void AnalyzeReservedTagName(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method)
    {
        // Only the general-purpose factory takes a caller-supplied tag; the
        // named factories hard-code safe tags.
        if (method.Name != "element")
        {
            return;
        }

        var tagArgument = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (tagArgument?.Expression is not LiteralExpressionSyntax literal ||
            !literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            // Non-literal tags cannot be checked here; the interpreter still
            // throws for unknown tags at runtime.
            return;
        }

        var tag = literal.Token.ValueText;
        if (VoidElementNames.Contains(tag))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.NativeReservedTagName,
                    tagArgument.GetLocation(),
                    tag));
        }
    }

    /// <summary>
    /// ABIES009 — a <c>text(...)</c> or <c>raw(...)</c> node inside a native
    /// tree. Reported at the offending node rather than at the enclosing
    /// element, so nesting produces exactly one diagnostic.
    /// </summary>
    private static void AnalyzeNonElementChild(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method)
    {
        if (!IsInsideNativeElement(invocation, context))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.NativeNonElementChild,
                invocation.GetLocation(),
                method.Name));
    }

    /// <summary>
    /// Walks outward to the nearest enclosing element factory. Native means a
    /// violation; HTML means this subtree is an ordinary HTML tree and the node
    /// is fine.
    /// </summary>
    private static bool IsInsideNativeElement(SyntaxNode node, SyntaxNodeAnalysisContext context)
    {
        foreach (var ancestor in node.Ancestors().OfType<InvocationExpressionSyntax>())
        {
            if (context.SemanticModel.GetSymbolInfo(ancestor, context.CancellationToken).Symbol
                is not IMethodSymbol ancestorMethod)
            {
                continue;
            }

            var type = ancestorMethod.ContainingType?.ToDisplayString();
            if (type == NativeElementsType)
            {
                return true;
            }

            if (type == HtmlElementsType)
            {
                return false;
            }
        }

        return false;
    }
}
