using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReflectionIT.DisposeGenerator.Attributes;

namespace ReflectionIT.DisposeGenerator;

internal class DisposableInfo {

    public ITypeSymbol TypeSymbol { get; }

    public bool IsThreadSafe { get; }
    
    public bool OverrideDispose { get; }

    public bool IsSealed { get; }
    public bool IsValueType { get; }


    public DisposableInfo(ITypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclarationSyntax) {
        
        TypeSymbol = typeSymbol;

        IsSealed = typeSymbol.IsSealed;
        IsValueType = typeSymbol.IsValueType;

        var attribute = typeSymbol.GetAttributes()
             .First(a => a.AttributeClass?.ToDisplayString() == typeof(DisposableAttribute).FullName);

        IsThreadSafe = attribute.NamedArguments.FirstOrDefault(n => n.Key == nameof(DisposableAttribute.IsThreadSafe)).Value.ToCSharpString() == "true";
        OverrideDispose = attribute.NamedArguments.FirstOrDefault(n => n.Key == nameof(DisposableAttribute.OverrideDispose)).Value.ToCSharpString() == "true";
    }
}