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
    public const string DiagnosticId = "PKA001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, // partial keyword analyzer
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
        if (ShouldIgnoreAssembly(context.Compilation.Assembly))
            return;

        // Ignore if the class has NoReflectionBaking attribute
        if (context.ContainingSymbol is INamedTypeSymbol classSymbol && ShouldIgnoreClass(classSymbol))
            return;

        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        if (classDeclaration.Members.Any(HasInjectAttribute) is false)
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
        return;

        static bool HasInjectAttribute(MemberDeclarationSyntax syntax)
        {
            var (attr1, attr2) = syntax switch
            {
                FieldDeclarationSyntax => ("Inject", "InjectOptional"),
                ConstructorDeclarationSyntax => ("InjectConstructor", null),
                MethodDeclarationSyntax => ("InjectMethod", null),
                _ => (null, null)
            };

            if (attr2 is not null) return HasAttribute2(syntax.AttributeLists, attr1!, attr2);
            if (attr1 is not null) return HasAttribute1(syntax.AttributeLists, attr1);
            return false;
        }

        static bool HasAttribute1(SyntaxList<AttributeListSyntax> syntaxList, string attr1)
        {
            foreach (var syntax in syntaxList)
            foreach (var attr in syntax.Attributes)
            {
                var name = attr.Name.ToString();
                if (EndsWithOrdinal(name, attr1))
                    return true;
            }

            return false;
        }

        static bool HasAttribute2(SyntaxList<AttributeListSyntax> syntaxList, string attr1, string attr2)
        {
            foreach (var syntax in syntaxList)
            foreach (var attr in syntax.Attributes)
            {
                var name = attr.Name.ToString();
                if (EndsWithOrdinal(name, attr1) || EndsWithOrdinal(name, attr2))
                    return true;
            }

            return false;
        }

        static bool ShouldIgnoreAssembly(IAssemblySymbol assembly)
        {
            return assembly.GetAttributes()
                .Any(a => a.AttributeClass!.Name == "NoReflectionBakingAttribute");
        }

        static bool ShouldIgnoreClass(INamedTypeSymbol classSymbol)
        {
            return classSymbol.GetAttributes()
                .Any(a => a.AttributeClass!.Name == "NoReflectionBakingAttribute");
        }

        static bool EndsWithOrdinal(string str, string value)
        {
            return str.EndsWith(value, System.StringComparison.Ordinal);
        }
    }
}