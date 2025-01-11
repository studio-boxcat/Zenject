using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Zenject.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class InjectMethodNameAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticIds.InjectNameMethod,
            title: "InjectMethod must be named 'Zenject_Constructor()'",
            messageFormat: "Method '{0}' has [InjectMethod], but must be named 'Zenject_Constructor()'",
            category: "CodeStyle",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Any method decorated with [InjectMethod] must be named 'Zenject_Constructor()'."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

            if (methodSymbol == null)
                return;

            // Check if the method has [InjectMethod] attribute
            var hasInjectMethodAttribute = methodSymbol
                .GetAttributes()
                .Any(a => a.AttributeClass?.Name == "InjectMethodAttribute");

            if (!hasInjectMethodAttribute)
                return;

            // If it has [InjectMethod], check the method name and parameter count.
            var methodName = methodSymbol.Name;
            var parameterCount = methodSymbol.Parameters.Length;

            // We want exactly "Zenject_Constructor"
            if (methodName != "Zenject_Constructor")
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    methodDeclaration.Identifier.GetLocation(),
                    methodName
                );
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}