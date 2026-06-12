using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReflectionIT.DisposeGenerator;

internal class DisposeInfo : IEquatable<DisposeInfo?> {

    public string MemberName { get; }
    public ITypeSymbol ContainingType { get; }
    public string ContainingTypeKey { get; }

    public bool SetToNull { get; }

    public DisposeInfo(ISymbol symbol, string typeName) {

        MemberName = symbol.Name;

        ContainingType = symbol.ContainingType;
        ContainingTypeKey = ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var attribute = symbol.GetAttributes()
             .First(a => a.AttributeClass?.ToDisplayString() == typeName);

        SetToNull = attribute.NamedArguments.FirstOrDefault(n => n.Key == AttributeMetadata.SetToNullPropertyName).Value.ToCSharpString() == "true";
    }

    public override bool Equals(object? obj) => Equals(obj as DisposeInfo);

    public bool Equals(DisposeInfo? other) => other is not null && MemberName == other.MemberName;

    public override int GetHashCode() => 30165064 + EqualityComparer<string>.Default.GetHashCode(MemberName);
}
