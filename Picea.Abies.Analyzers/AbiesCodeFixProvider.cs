using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Picea.Abies.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AbiesCodeFixProvider)), Shared]
public sealed class AbiesCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("ABIES001", "ABIES003", "ABIES004", "ABIES005", "ABIES006");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            switch (diagnostic.Id)
            {
                case "ABIES001":
                    RegisterAddAttributeFix(context, node, diagnostic, "Add alt(\"\")", "alt(\"\")");
                    break;
                case "ABIES003":
                    RegisterAddAttributeFix(context, node, diagnostic, "Add href(\"#\")", "href(\"#\")");
                    break;
                case "ABIES004":
                    RegisterAddAttributeFix(context, node, diagnostic, "Add type(\"button\")", "type(\"button\")");
                    break;
                case "ABIES005":
                    RegisterAddAttributeFix(context, node, diagnostic, "Add type(\"text\")", "type(\"text\")");
                    break;
                case "ABIES006":
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Add explicit id argument",
                            createChangedDocument: ct => AddEventIdArgumentAsync(context.Document, root, node, ct),
                            equivalenceKey: "ABIES006_AddId"),
                        diagnostic);
                    break;
            }
        }
    }

    private static void RegisterAddAttributeFix(
        CodeFixContext context,
        SyntaxNode node,
        Diagnostic diagnostic,
        string title,
        string attributeInvocation)
    {
        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: ct => AddMissingAttributeAsync(context.Document, node, attributeInvocation, ct),
                equivalenceKey: diagnostic.Id),
            diagnostic);
    }

    private static async Task<Document> AddMissingAttributeAsync(
        Document document,
        SyntaxNode node,
        string attributeInvocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation == null)
            return document;

        if (invocation.ArgumentList.Arguments.Count == 0)
            return document;

        var firstArg = invocation.ArgumentList.Arguments[0];
        if (!TryAppendCollectionExpression(
                firstArg.Expression,
                SyntaxFactory.ParseExpression(attributeInvocation),
                out var newAttributesExpression))
        {
            return document;
        }

        var newRoot = root.ReplaceNode(firstArg.Expression, newAttributesExpression);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool TryAppendCollectionExpression(
        ExpressionSyntax original,
        ExpressionSyntax item,
        out ExpressionSyntax updated)
    {
        if (original is CollectionExpressionSyntax collectionExpression)
        {
            updated = collectionExpression.WithElements(
                collectionExpression.Elements.Add(SyntaxFactory.ExpressionElement(item)));
            return true;
        }

        if (original is ImplicitArrayCreationExpressionSyntax implicitArray &&
            implicitArray.Initializer != null)
        {
            updated = implicitArray.WithInitializer(
                implicitArray.Initializer.WithExpressions(
                    implicitArray.Initializer.Expressions.Add(item)));
            return true;
        }

        if (original is ArrayCreationExpressionSyntax arrayCreation &&
            arrayCreation.Initializer != null)
        {
            updated = arrayCreation.WithInitializer(
                arrayCreation.Initializer.WithExpressions(
                    arrayCreation.Initializer.Expressions.Add(item)));
            return true;
        }

        if (original is InitializerExpressionSyntax initializer)
        {
            updated = initializer.WithExpressions(initializer.Expressions.Add(item));
            return true;
        }

        updated = original;
        return false;
    }

    private static async Task<Document> AddEventIdArgumentAsync(
        Document document,
        SyntaxNode root,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        if (!HandlerIdentityAnalysis.IsAbiesEventInvocation(invocation, semanticModel, out var method) ||
            HandlerIdentityAnalysis.HasExplicitIdArgument(invocation, method))
        {
            return document;
        }

        var repeatedKey = HandlerIdentityAnalysis.TryGetNearestRepeatKeyIdentifier(invocation);
        var idExpression = SyntaxFactory.ParseExpression(CreateIdExpressionText(method.Name, repeatedKey));
        var idArgument = SyntaxFactory.Argument(
            SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("id")),
            default,
            idExpression);

        var newInvocation = invocation.WithArgumentList(
            invocation.ArgumentList.WithArguments(invocation.ArgumentList.Arguments.Add(idArgument)));

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }

    private static string CreateIdExpressionText(string eventName, string? repeatedKey)
    {
        if (string.IsNullOrWhiteSpace(repeatedKey))
        {
            return $"\"{eventName}:stable-id\"";
        }

        return $"$\"{eventName}:{{{repeatedKey}}}\"";
    }
}
