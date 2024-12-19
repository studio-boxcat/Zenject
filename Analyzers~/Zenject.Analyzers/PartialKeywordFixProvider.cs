using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zenject.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PartialKeywordFixProvider))]
[Shared]
public class PartialKeywordFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(PartialKeywordAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the class declaration identified by the diagnostic.
        var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration == null)
            return;

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Make class partial",
                createChangedDocument: c => AddPartialKeywordAsync(context.Document, classDeclaration, c),
                equivalenceKey: "Make class partial"),
            diagnostic);
    }

    private static async Task<Document> AddPartialKeywordAsync(Document document,
        ClassDeclarationSyntax classDeclaration,
        CancellationToken cancellationToken)
    {
        // Add the partial modifier to the class.
        var partialModifier = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
        var newModifiers = classDeclaration.Modifiers.Add(partialModifier);
        var newClassDeclaration = classDeclaration.WithModifiers(newModifiers);

        // Replace the old class declaration with the new one.
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}