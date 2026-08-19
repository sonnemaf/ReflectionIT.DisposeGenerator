using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReflectionIT.DisposeGenerator;

internal class DisposeInfo : IEquatable<DisposeInfo?> {

    public ISymbol Symbol { get; }
    public string MemberName { get; }
    public ITypeSymbol MemberType { get; }
    public ITypeSymbol ContainingType { get; }
    public string ContainingTypeKey { get; }

    public bool SetToNull { get; }
    public bool IsStatic { get; }
    public bool CanSetToNull { get; }

    public DisposeInfo(ISymbol symbol, string typeName) {

        Symbol = symbol;
        MemberName = symbol.Name;
        MemberType = symbol switch {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => throw new ArgumentException($"Unsupported disposable member kind '{symbol.Kind}'.", nameof(symbol)),
        };

        ContainingType = symbol.ContainingType;
        ContainingTypeKey = ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        IsStatic = symbol.IsStatic;

        var attribute = symbol.GetAttributes()
             .First(a => a.AttributeClass?.ToDisplayString() == typeName);

        SetToNull = attribute.NamedArguments.FirstOrDefault(n => n.Key == AttributeMetadata.SetToNullPropertyName).Value.ToCSharpString() == "true";
        CanSetToNull = CanAssignNull(MemberType) && symbol switch {
            IFieldSymbol field => !field.IsConst && !field.IsReadOnly,
            IPropertySymbol property => property.SetMethod is { IsInitOnly: false },
            _ => false,
        };

        static bool CanAssignNull(ITypeSymbol type) =>
            type.IsReferenceType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    public override bool Equals(object? obj) => Equals(obj as DisposeInfo);

    public bool Equals(DisposeInfo? other) =>
        other is not null
        && MemberName == other.MemberName
        && ContainingTypeKey == other.ContainingTypeKey;

    public override int GetHashCode() {
        unchecked {
            return (EqualityComparer<string>.Default.GetHashCode(ContainingTypeKey) * 397)
                ^ EqualityComparer<string>.Default.GetHashCode(MemberName);
        }
    }
}
