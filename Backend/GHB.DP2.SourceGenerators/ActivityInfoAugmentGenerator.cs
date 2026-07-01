using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GHB.DP2.SourceGenerators;

using Templates;

[Generator]
public class ActivityInfoAugmentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations =
            context.SyntaxProvider
                   .CreateSyntaxProvider(
                       predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                       transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                   .Where(static m => m is not null)!;

        context.RegisterSourceOutput(classDeclarations, static (spc, classDeclaration) =>
            Execute(spc, classDeclaration));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclaration
               && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        if (symbol == null)
            return null;

        var hasActivityInterface = symbol.AllInterfaces.FirstOrDefault(i =>
            i.ToDisplayString() == "GHB.DP2.Domain.Common.IHasActivityInfo");

        if (hasActivityInterface == null)
            return null;

        // Check if this class inherits from a base class that already implements IHasActivityInfo
        // If so, skip generation to avoid CS0108 warnings
        if (HasBaseClassWithActivityInfo(symbol))
            return null;

        return classDeclaration;
    }

    /// <summary>
    /// Checks if the given class symbol has a base class that implements IHasActivityInfo.
    /// This prevents generating duplicate activity info members in derived classes.
    /// </summary>
    /// <param name="classSymbol">The class symbol to check</param>
    /// <returns>True if a base class implements IHasActivityInfo, false otherwise</returns>
    private static bool HasBaseClassWithActivityInfo(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;

        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            // Check if this base class implements IHasActivityInfo
            var baseHasActivityInfo = baseType.AllInterfaces.Any(i =>
                i.ToDisplayString() == "GHB.DP2.Domain.Common.IHasActivityInfo");

            if (baseHasActivityInfo)
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static void Execute(SourceProductionContext context, ClassDeclarationSyntax classDeclaration)
    {
        var namespaceName = GetNamespace(classDeclaration);
        var className = classDeclaration.Identifier.ToString();

        var source = GenerateAugmentedPartialClass(namespaceName, className);

        context.AddSource($"{className}.ActivityInfo.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string? GetNamespace(SyntaxNode syntaxNode)
    {
        if (syntaxNode.Parent is NamespaceDeclarationSyntax namespaceSyntax)
        {
            return namespaceSyntax.Name.ToString();
        }

        if (syntaxNode.Parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceSyntax)
        {
            return fileScopedNamespaceSyntax.Name.ToString();
        }

        // For nested classes, recurse up to find the namespace
        if (syntaxNode.Parent is ClassDeclarationSyntax parentClass)
        {
            return GetNamespace(parentClass);
        }

        return null;
    }

    private static string GenerateAugmentedPartialClass(string? namespaceName, string className)
    {
        return ActivityInfoTemplate.Generate(namespaceName, className);
    }
}
