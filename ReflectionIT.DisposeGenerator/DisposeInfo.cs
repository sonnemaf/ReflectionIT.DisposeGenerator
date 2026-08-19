using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReflectionIT.DisposeGenerator;

internal class DisposeInfo : IEquatable<DisposeInfo?> {

    public Location? Location { get; }
    public string MemberName { get; }
    public string ContainingTypeKey { get; }

    public bool SetToNull { get; }
    public bool IsStatic { get; }
    public bool CanSetToNull { get; }
    public bool SupportsDispose { get; }
    public bool SupportsAsyncDispose { get; }

    public DisposeInfo(ISymbol symbol, AttributeData attribute, Compilation compilation) {

        Location = symbol.Locations.FirstOrDefault();
        MemberName = symbol.Name;
        ITypeSymbol memberType = symbol switch {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => throw new ArgumentException($"Unsupported disposable member kind '{symbol.Kind}'.", nameof(symbol)),
        };

        ContainingTypeKey = symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        IsStatic = symbol.IsStatic;

        SetToNull = ReadBoolean(attribute, AttributeMetadata.SetToNullPropertyName);
        CanSetToNull = CanAssignNull(memberType) && symbol switch {
            IFieldSymbol field => !field.IsConst && !field.IsReadOnly,
            IPropertySymbol property => property.SetMethod is { IsInitOnly: false },
            _ => false,
        };
        SupportsDispose = ImplementsInterface(memberType, compilation.GetTypeByMetadataName("System.IDisposable"));
        SupportsAsyncDispose = ImplementsInterface(memberType, compilation.GetTypeByMetadataName("System.IAsyncDisposable"));

        static bool CanAssignNull(ITypeSymbol type) =>
            type.IsReferenceType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

        static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol? interfaceType) =>
            interfaceType is not null
            && (SymbolEqualityComparer.Default.Equals(type, interfaceType)
                || type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType)));
    }

    public override bool Equals(object? obj) => Equals(obj as DisposeInfo);

    public virtual bool Equals(DisposeInfo? other) =>
        other is not null
        && GetType() == other.GetType()
        && MemberName == other.MemberName
        && ContainingTypeKey == other.ContainingTypeKey
        && SetToNull == other.SetToNull
        && IsStatic == other.IsStatic
        && CanSetToNull == other.CanSetToNull
        && SupportsDispose == other.SupportsDispose
        && SupportsAsyncDispose == other.SupportsAsyncDispose
        && GetLocationKey(Location) == GetLocationKey(other.Location);

    public override int GetHashCode() {
        unchecked {
            int hashCode = EqualityComparer<string>.Default.GetHashCode(ContainingTypeKey);
            hashCode = (hashCode * 397) ^ EqualityComparer<string>.Default.GetHashCode(MemberName);
            hashCode = (hashCode * 397) ^ SetToNull.GetHashCode();
            hashCode = (hashCode * 397) ^ IsStatic.GetHashCode();
            hashCode = (hashCode * 397) ^ CanSetToNull.GetHashCode();
            hashCode = (hashCode * 397) ^ SupportsDispose.GetHashCode();
            hashCode = (hashCode * 397) ^ SupportsAsyncDispose.GetHashCode();
            return (hashCode * 397) ^ EqualityComparer<string>.Default.GetHashCode(GetLocationKey(Location));
        }
    }

    protected static bool ReadBoolean(AttributeData attribute, string propertyName, bool defaultValue = false) {
        KeyValuePair<string, TypedConstant> argument = attribute.NamedArguments.FirstOrDefault(n => n.Key == propertyName);
        return argument.Key is null ? defaultValue : argument.Value.Value is true;
    }

    private static string GetLocationKey(Location? location) =>
        location is null
            ? string.Empty
            : $"{location.SourceTree?.FilePath}|{location.SourceSpan.Start}|{location.SourceSpan.Length}";
}
