using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Zenject.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ReadonlyInjectFieldAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticIds.ReadonlyInjectField,
            "Readonly field cannot have [Inject] or [InjectOptional]",
            "Field '{0}' is readonly and cannot be marked with [Inject] or [InjectOptional]",
            category: "CodeStyle",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description:
            "Readonly fields should not be marked with injection attributes because injection frameworks typically need to set these fields post-construction."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // We don't want to analyze auto-generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            // Cast to FieldDeclarationSyntax
            if (context.Node is not FieldDeclarationSyntax fieldDeclaration)
                return;

            // Check if the field is "readonly"
            var isReadOnly = fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
            if (!isReadOnly)
                return;

            // Check if the field has [Inject] or [InjectOptional] attribute
            var hasInjectAttribute = Utils.HasFieldInjectAttribute(fieldDeclaration);
            if (!hasInjectAttribute)
                return;

            // For each variable declared (in case of multiple fields in one declaration)
            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                // Report a diagnostic for each offending field
                var diagnostic = Diagnostic.Create(
                    Rule,
                    variable.Identifier.GetLocation(),
                    variable.Identifier.Text
                );
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}