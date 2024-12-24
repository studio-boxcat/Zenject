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

namespace Zenject.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReadonlyInjectFieldFixProvider))]
    [Shared]
    public class ReadonlyInjectFieldFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(DiagnosticIds.ReadonlyInjectField);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null)
                return;

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the FieldDeclaration that was flagged.
            var fieldDeclaration = root.FindToken(diagnosticSpan.Start)
                .Parent?.AncestorsAndSelf()
                .OfType<FieldDeclarationSyntax>()
                .FirstOrDefault();

            if (fieldDeclaration == null)
                return;

            // Register a code action that will remove 'readonly'.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Remove 'readonly' modifier",
                    createChangedDocument: c => RemoveReadonlyModifierAsync(context.Document, fieldDeclaration, c),
                    equivalenceKey: "RemoveReadonlyModifier"
                ),
                diagnostic
            );
        }

        private static async Task<Document> RemoveReadonlyModifierAsync(
            Document document,
            FieldDeclarationSyntax fieldDeclaration,
            CancellationToken cancellationToken)
        {
            var filteredModifiers = fieldDeclaration.Modifiers
                .Where(mod => !mod.IsKind(SyntaxKind.ReadOnlyKeyword));

            var newModifiers = SyntaxFactory.TokenList(filteredModifiers);
            var newFieldDeclaration = fieldDeclaration.WithModifiers(newModifiers);

            // Reuse the common helper
            return await document.ReplaceNodeAsync(
                oldNode: fieldDeclaration,
                newNode: newFieldDeclaration,
                cancellationToken
            );
        }
    }
}