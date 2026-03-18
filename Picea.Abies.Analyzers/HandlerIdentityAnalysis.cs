using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Picea.Abies.Analyzers;

internal static class HandlerIdentityAnalysis
{
    private const string EventsTypeName = "Picea.Abies.Html.Events";
    private const string NodeTypeName = "Picea.Abies.DOM.Node";

    public static bool IsAbiesEventInvocation(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out IMethodSymbol method)
    {
        method = null!;
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol candidate)
            return false;

        if (candidate.ContainingType?.ToDisplayString() != EventsTypeName)
            return false;

        method = candidate;
        return true;
    }

    public static bool HasExplicitIdArgument(InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
        if (!method.Parameters.Any() || method.Parameters[method.Parameters.Length - 1].Name != "id")
            return false;

        var args = invocation.ArgumentList.Arguments;
        if (args.Any(a => a.NameColon?.Name.Identifier.Text == "id"))
            return true;

        return args.Count >= method.Parameters.Length;
    }

    public static bool IsInsideRepeatedRenderPath(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        foreach (var ancestor in invocation.Ancestors())
        {
            if (ancestor is ForEachStatementSyntax or ForEachVariableStatementSyntax or ForStatementSyntax)
                return true;

            if (ancestor is QueryExpressionSyntax)
                return true;

            if (ancestor is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax)
            {
                var lambda = (LambdaExpressionSyntax)ancestor;
                if (lambda.Parent is ArgumentSyntax arg &&
                    arg.Parent?.Parent is InvocationExpressionSyntax outerInvocation &&
                    semanticModel.GetSymbolInfo(outerInvocation).Symbol is IMethodSymbol outerMethod &&
                    IsLikelyRepeatedLinqMethod(outerMethod))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string? TryGetNearestRepeatKeyIdentifier(InvocationExpressionSyntax invocation)
    {
        foreach (var ancestor in invocation.Ancestors())
        {
            if (ancestor is ForEachStatementSyntax forEach)
                return forEach.Identifier.ValueText;

            if (ancestor is ForStatementSyntax forStatement &&
                forStatement.Declaration?.Variables.Count > 0)
            {
                return forStatement.Declaration.Variables[0].Identifier.ValueText;
            }

            if (ancestor is SimpleLambdaExpressionSyntax simpleLambda)
                return simpleLambda.Parameter.Identifier.ValueText;

            if (ancestor is ParenthesizedLambdaExpressionSyntax parenthesizedLambda &&
                parenthesizedLambda.ParameterList.Parameters.Count > 0)
            {
                return parenthesizedLambda.ParameterList.Parameters[0].Identifier.ValueText;
            }
        }

        return null;
    }

    public static bool ReturnsNode(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        return returnType.ToDisplayString() == NodeTypeName ||
               InheritsFrom(returnType, NodeTypeName);
    }

    public static bool ContainsEventHandlerWithoutExplicitId(
        IMethodSymbol method,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var declaration = syntaxRef.GetSyntax(cancellationToken);
            var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);

            var invocations = declaration.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                if (IsAbiesEventInvocation(invocation, semanticModel, out var eventMethod) &&
                    !HasExplicitIdArgument(invocation, eventMethod))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsLikelyRepeatedLinqMethod(IMethodSymbol method)
    {
        if (method.ContainingNamespace?.ToDisplayString() != "System.Linq")
            return false;

        return method.Name is "Select" or "SelectMany" or "Where" or "Join" or "GroupJoin";
    }

    private static bool InheritsFrom(ITypeSymbol type, string baseTypeDisplayName)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == baseTypeDisplayName)
                return true;

            current = current.BaseType;
        }

        return false;
    }
}
