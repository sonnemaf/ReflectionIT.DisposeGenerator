using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReflectionIT.DisposeGenerator.Attributes;

namespace ReflectionIT.DisposeGenerator;

internal class DisposableInfo {

    public ITypeSymbol TypeSymbol { get; }
    public TypeDeclarationSyntax TypeDeclarationSyntax { get; }

    public bool IsThreadSafe { get; }
    public bool OverrideDispose { get; }
    public bool OverrideDisposeAsyncCore { get; set; }
    public bool GenerateThrowIfDisposed { get; }
    public bool ExplicitInterfaceImplementation { get; }
    public bool HasUnmanagedResources { get; }

    public bool IsSealed { get; }
    public bool IsValueType { get; }
    public bool IsPartial { get; }


    public DisposableInfo(ITypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclarationSyntax) {
        TypeSymbol = typeSymbol;
        TypeDeclarationSyntax = typeDeclarationSyntax;

        IsSealed = typeSymbol.IsSealed;
        IsValueType = typeSymbol.IsValueType;
        IsPartial = typeDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);

        var attribute = typeSymbol.GetAttributes().First(a => a.AttributeClass?.ToDisplayString() == typeof(DisposableAttribute).FullName);

        IsThreadSafe = ReadBoolean(attribute, nameof(DisposableAttribute.IsThreadSafe));
        OverrideDispose = ReadBoolean(attribute, nameof(DisposableAttribute.OverrideDispose));
        OverrideDisposeAsyncCore = ReadBoolean(attribute, nameof(DisposableAttribute.OverrideDisposeAsyncCore));
        GenerateThrowIfDisposed = ReadBoolean(attribute, nameof(DisposableAttribute.GenerateThrowIfDisposed), defaultValue: true);
        ExplicitInterfaceImplementation = ReadBoolean(attribute, nameof(DisposableAttribute.ExplicitInterfaceImplementation));
        HasUnmanagedResources = ReadBoolean(attribute, nameof(DisposableAttribute.HasUnmanagedResources));

        static bool ReadBoolean(AttributeData attribute, string propertyName, bool defaultValue = false) {
            var namedArgument = attribute.NamedArguments.FirstOrDefault(n => n.Key == propertyName);
            return namedArgument.Key is null ? defaultValue : namedArgument.Value.ToCSharpString() == "true";
        }
    }
}