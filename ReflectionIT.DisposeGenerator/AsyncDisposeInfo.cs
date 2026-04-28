using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ReflectionIT.DisposeGenerator.Attributes;

namespace ReflectionIT.DisposeGenerator;

internal class AsyncDisposeInfo : DisposeInfo {

    public AsyncDisposeInfo(ISymbol symbol) : base(symbol, typeof(AsyncDisposeAttribute).FullName) {
        var attribute = symbol.GetAttributes()
             .First(a => a.AttributeClass?.ToDisplayString() == typeof(AsyncDisposeAttribute).FullName);

        ConfigureAwait = attribute.NamedArguments.FirstOrDefault(n => n.Key == nameof(AsyncDisposeAttribute.ConfigureAwait)).Value.ToCSharpString() == "true";
    }

    public bool ConfigureAwait { get; }

}