using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zenject.Analyzers;

internal static class Utils
{
    public static bool EndsWithOrdinal(string str, string value)
    {
        return str.EndsWith(value, System.StringComparison.Ordinal);
    }

    public static bool HasInjectAttribute(MemberDeclarationSyntax syntax)
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

    public static bool HasFieldInjectAttribute(FieldDeclarationSyntax syntax)
    {
        return HasAttribute2(syntax.AttributeLists, "Inject", "InjectOptional");
    }

    private static bool HasAttribute1(SyntaxList<AttributeListSyntax> syntaxList, string attr1)
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

    private static bool HasAttribute2(SyntaxList<AttributeListSyntax> syntaxList, string attr1, string attr2)
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


    public static bool ShouldIgnoreAssembly(IAssemblySymbol assembly)
    {
        return assembly.GetAttributes()
            .Any(a => a.AttributeClass!.Name == "NoReflectionBakingAttribute");
    }

    public static bool ShouldIgnoreClass(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetAttributes()
            .Any(a => a.AttributeClass!.Name == "NoReflectionBakingAttribute");
    }
}