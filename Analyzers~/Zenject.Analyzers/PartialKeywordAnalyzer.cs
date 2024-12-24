// ReSharper disable ClassNeverInstantiated.Global

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Zenject.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PartialKeywordAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.PartialKeyword, // partial keyword analyzer
        "Class must be partial",
        "Class '{0}' must be marked as partial because it contains fields or methods with injection attributes",
        "CodeStyle",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
        "Classes with injection attributes must be marked as partial to allow for code generation or extension."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        // Ignore if the assembly containing class has NoReflectionBaking attribute
        if (Utils.ShouldIgnoreAssembly(context.Compilation.Assembly))
            return;

        // Ignore if the class has NoReflectionBaking attribute
        if (context.ContainingSymbol is INamedTypeSymbol classSymbol && Utils.ShouldIgnoreClass(classSymbol))
            return;

        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        if (classDeclaration.Members.Any(Utils.HasInjectAttribute) is false)
            return;

        // Check for partial keyword
        var isPartial = classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
        if (isPartial) return;

        // Check if the class is a partial class
        var diagnostic = Diagnostic.Create(
            Rule,
            classDeclaration.Identifier.GetLocation(),
            classDeclaration.Identifier.Text
        );
        context.ReportDiagnostic(diagnostic);
    }
}