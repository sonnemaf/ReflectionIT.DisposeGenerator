using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReflectionIT.DisposeGenerator.Attributes;

namespace ReflectionIT.DisposeGenerator;

internal class DisposableInfo {

    public ITypeSymbol TypeSymbol { get; }

    public bool IsThreadSafe { get; }
    public bool OverrideDispose { get; }
    public bool ExplicitInterfaceImplementation { get; }
    public bool HasUnmanagedResources { get; }

    public bool IsSealed { get; }
    public bool IsValueType { get; }


    public DisposableInfo(ITypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclarationSyntax) {
        TypeSymbol = typeSymbol;

        IsSealed = typeSymbol.IsSealed;
        IsValueType = typeSymbol.IsValueType;

        var attribute = typeSymbol.GetAttributes().First(a => a.AttributeClass?.ToDisplayString() == typeof(DisposableAttribute).FullName);

        IsThreadSafe = ReadBoolean(attribute, nameof(DisposableAttribute.IsThreadSafe));
        OverrideDispose = ReadBoolean(attribute, nameof(DisposableAttribute.OverrideDispose));
        ExplicitInterfaceImplementation = ReadBoolean(attribute, nameof(DisposableAttribute.ExplicitInterfaceImplementation));
        HasUnmanagedResources = ReadBoolean(attribute, nameof(DisposableAttribute.HasUnmanagedResources));

        static bool ReadBoolean(AttributeData attribute, string propertyName) {
            return attribute.NamedArguments.FirstOrDefault(n => n.Key == propertyName).Value.ToCSharpString() == "true";
        }
    }
}