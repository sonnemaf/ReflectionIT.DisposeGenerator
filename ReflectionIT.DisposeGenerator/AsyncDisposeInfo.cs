using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ReflectionIT.DisposeGenerator;

internal class AsyncDisposeInfo : DisposeInfo {

    public AsyncDisposeInfo(ISymbol symbol) : base(symbol, AttributeMetadata.AsyncDisposeAttributeName) {

        var attribute = symbol.GetAttributes()
             .First(a => a.AttributeClass?.ToDisplayString() == AttributeMetadata.AsyncDisposeAttributeName);

        ConfigureAwait = attribute.NamedArguments.FirstOrDefault(n => n.Key == AttributeMetadata.ConfigureAwaitPropertyName).Value.ToCSharpString() == "true";
    }

    public bool ConfigureAwait { get; }

}