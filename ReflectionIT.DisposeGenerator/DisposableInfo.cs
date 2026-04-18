using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReflectionIT.DisposeGenerator.Attributes;

namespace ReflectionIT.DisposeGenerator;

internal class DisposableInfo {

    public ITypeSymbol TypeSymbol { get; }

    public bool IsThreadSafe { get; }
    
    public bool IsSealedOrStruct { get; }


    public DisposableInfo(ITypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclarationSyntax) {
        
        TypeSymbol = typeSymbol;

        IsSealedOrStruct = typeDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.SealedKeyword)) || typeDeclarationSyntax.Keyword.IsKind(SyntaxKind.StructKeyword);

        var attribute = typeSymbol.GetAttributes()
             .First(a => a.AttributeClass?.ToDisplayString() == typeof(DisposableAttribute).FullName);

        IsThreadSafe = attribute.NamedArguments.FirstOrDefault(n => n.Key == nameof(DisposableAttribute.IsThreadSafe)).Value.ToCSharpString() == "true";
    }
}