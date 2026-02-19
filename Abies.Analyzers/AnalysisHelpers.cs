using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Abies.Analyzers;

/// <summary>
/// Shared helper methods for analyzing Abies.Html element and attribute invocations.
/// </summary>
internal static class AnalysisHelpers
{
    private const string _elementsTypeName = "Abies.Html.Elements";
    private const string _attributesTypeName = "Abies.Html.Attributes";

    /// <summary>
    /// Attempts to determine the HTML element name from an invocation expression
    /// that calls a method on Abies.Html.Elements.
    /// </summary>
    /// <returns>The element name (e.g., "div", "img", "span") or null if not an Elements call.</returns>
    public static string? GetElementName(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol method)
        {
            return null;
        }

        var containingType = method.ContainingType?.ToDisplayString();
        if (containingType != _elementsTypeName)
        {
            return null;
        }

        // The method name IS the element name (e.g., Elements.div → "div", Elements.img → "img")
        // Exception: element() is the generic factory, skip it
        var name = method.Name;
        if (name == "element" || name == "text" || name == "raw" || name == "memo" || name == "lazy")
        {
            return null;
        }

        // Handle C# escaped names like @base → "base", object_ → "object"
        if (name.EndsWith("_"))
        {
            name = name.Substring(0, name.Length - 1);
        }

        return name;
    }

    /// <summary>
    /// Extracts the set of attribute names used in the first argument (attributes array)
    /// of an element invocation.
    /// </summary>
    /// <returns>A set of HTML attribute names found in the attributes argument.</returns>
    public static ImmutableHashSet<string> GetAttributeNames(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>();

        // The first argument is the attributes array: DOM.Attribute[]
        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0)
        {
            return builder.ToImmutable();
        }

        var firstArg = args[0].Expression;
        var attributeExpressions = GetCollectionElements(firstArg);

        foreach (var expr in attributeExpressions)
        {
            var attrName = GetAttributeName(expr, semanticModel);
            if (attrName != null)
            {
                builder.Add(attrName);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Extracts the element names used in the children argument (second argument)
    /// of an element invocation.
    /// </summary>
    /// <returns>A list of (childElementName, childInvocationLocation) tuples.</returns>
    public static ImmutableArray<(string ElementName, Location Location)> GetChildElementNames(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        var builder = ImmutableArray.CreateBuilder<(string, Location)>();

        // Children are the second argument: Node[]
        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 2)
        {
            return builder.ToImmutable();
        }

        var secondArg = args[1].Expression;
        var childExpressions = GetCollectionElements(secondArg);

        foreach (var expr in childExpressions)
        {
            if (expr is InvocationExpressionSyntax childInvocation)
            {
                var childName = GetElementName(childInvocation, semanticModel);
                if (childName != null)
                {
                    builder.Add((childName, childInvocation.GetLocation()));
                }
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Determines the attribute name from an expression that is expected to be
    /// a call to an Attributes.* factory function.
    /// </summary>
    private static string? GetAttributeName(
        ExpressionSyntax expression,
        SemanticModel semanticModel)
    {
        if (expression is not InvocationExpressionSyntax attrInvocation)
        {
            return null;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(attrInvocation);
        if (symbolInfo.Symbol is not IMethodSymbol method)
        {
            return null;
        }

        var containingType = method.ContainingType?.ToDisplayString();
        if (containingType != _attributesTypeName)
        {
            return null;
        }

        // The method name maps to the attribute name
        var name = method.Name;

        // Handle C# name escaping: class_ → "class", for_ → "for", etc.
        // But "attribute" is the generic factory — need to extract the name from the first arg
        if (name == "attribute")
        {
            // attribute("name", "value") — extract the first string literal arg
            if (attrInvocation.ArgumentList.Arguments.Count > 0)
            {
                var firstArg = attrInvocation.ArgumentList.Arguments[0].Expression;
                var constValue = semanticModel.GetConstantValue(firstArg);
                if (constValue.HasValue && constValue.Value is string strValue)
                {
                    return strValue;
                }
            }
            return null;
        }

        // Some attribute methods have trailing underscore: class_ → class, for_ → for
        if (name.EndsWith("_"))
        {
            name = name.Substring(0, name.Length - 1);
        }

        // Some attribute methods use camelCase for hyphenated: ariaLabel → aria-label
        // But the actual HTML attribute name is what matters for checking — and the 
        // attribute() constructor maps the name correctly. We just need the conceptual name.
        // For our purposes, the method name (after _ removal) is sufficient for matching
        // against our spec tables since they use the same naming convention.
        return name;
    }

    /// <summary>
    /// Extracts individual expressions from a collection expression, array creation,
    /// or implicit array creation syntax.
    /// Handles: [a, b, c], new[] { a, b, c }, new DOM.Attribute[] { a, b, c }
    /// </summary>
    private static IEnumerable<ExpressionSyntax> GetCollectionElements(ExpressionSyntax expression)
    {
        // C# 12 collection expression: [a, b, c]
        if (expression is CollectionExpressionSyntax collectionExpr)
        {
            foreach (var element in collectionExpr.Elements)
            {
                if (element is ExpressionElementSyntax exprElement)
                {
                    yield return exprElement.Expression;
                }
                else if (element is SpreadElementSyntax spread)
                {
                    yield return spread.Expression;
                }
            }
            yield break;
        }

        // Implicit array: new[] { a, b, c }
        if (expression is ImplicitArrayCreationExpressionSyntax implicitArray)
        {
            if (implicitArray.Initializer != null)
            {
                foreach (var elem in implicitArray.Initializer.Expressions)
                {
                    yield return elem;
                }
            }
            yield break;
        }

        // Explicit array: new DOM.Attribute[] { a, b, c }
        if (expression is ArrayCreationExpressionSyntax arrayCreation)
        {
            if (arrayCreation.Initializer != null)
            {
                foreach (var elem in arrayCreation.Initializer.Expressions)
                {
                    yield return elem;
                }
            }
            yield break;
        }

        // InitializerExpression (bare { a, b, c } used in some contexts)
        if (expression is InitializerExpressionSyntax initializer)
        {
            foreach (var elem in initializer.Expressions)
            {
                yield return elem;
            }

            yield break;
        }
    }
}
