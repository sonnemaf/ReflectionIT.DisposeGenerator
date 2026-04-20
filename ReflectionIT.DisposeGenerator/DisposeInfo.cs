using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReflectionIT.DisposeGenerator.Attributes;

namespace ReflectionIT.DisposeGenerator;

internal class DisposeInfo {

    public string MemberName { get; }
    public ITypeSymbol ContainingType { get; }

    public bool SetToNull { get; }

    public DisposeInfo(ISymbol symbol) {

        MemberName = symbol.Name;

        ContainingType = symbol.ContainingType;

        var attribute = symbol.GetAttributes()
             .First(a => a.AttributeClass?.ToDisplayString() == typeof(DisposeAttribute).FullName);

        SetToNull = attribute.NamedArguments.FirstOrDefault(n => n.Key == nameof(DisposeAttribute.SetToNull)).Value.ToCSharpString() == "true";
    }
}