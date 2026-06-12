using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReflectionIT.DisposeGenerator;

internal class DisposableInfo {

    public ITypeSymbol TypeSymbol { get; }
    public string TypeKey { get; }
    public TypeDeclarationSyntax TypeDeclarationSyntax { get; }

    public bool IsThreadSafe { get; }
    public bool OverrideDispose { get; }
    public bool OverrideDisposeAsyncCore { get; set; }
    public bool GenerateThrowIfDisposed { get; } = true;
    public bool ExplicitInterfaceImplementation { get; }
    public bool HasUnmanagedResources { get; }

    public bool IsSealed { get; }
    public bool IsValueType { get; }
    public bool IsPartial { get; }


    public DisposableInfo(ITypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclarationSyntax) {
        TypeSymbol = typeSymbol;
        TypeKey = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        TypeDeclarationSyntax = typeDeclarationSyntax;

        IsSealed = typeSymbol.IsSealed;
        IsValueType = typeSymbol.IsValueType;
        IsPartial = typeDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);

        var attribute = typeSymbol.GetAttributes().First(a => a.AttributeClass?.ToDisplayString() == AttributeMetadata.DisposableAttributeName);

        IsThreadSafe = ReadBoolean(attribute, AttributeMetadata.IsThreadSafePropertyName);
        OverrideDispose = ReadBoolean(attribute, AttributeMetadata.OverrideDisposePropertyName);
        OverrideDisposeAsyncCore = ReadBoolean(attribute, AttributeMetadata.OverrideDisposeAsyncCorePropertyName);
        GenerateThrowIfDisposed = ReadBoolean(attribute, AttributeMetadata.GenerateThrowIfDisposedPropertyName, defaultValue: true);
        ExplicitInterfaceImplementation = ReadBoolean(attribute, AttributeMetadata.ExplicitInterfaceImplementationPropertyName);
        HasUnmanagedResources = ReadBoolean(attribute, AttributeMetadata.HasUnmanagedResourcesPropertyName);

        static bool ReadBoolean(AttributeData attribute, string propertyName, bool defaultValue = false) {
            var namedArgument = attribute.NamedArguments.FirstOrDefault(n => n.Key == propertyName);
            return namedArgument.Key is null ? defaultValue : namedArgument.Value.ToCSharpString() == "true";
        }
    }
}