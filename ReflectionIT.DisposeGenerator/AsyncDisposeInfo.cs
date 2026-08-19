using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ReflectionIT.DisposeGenerator;

internal class AsyncDisposeInfo : DisposeInfo {

    public AsyncDisposeInfo(ISymbol symbol, AttributeData attribute, Compilation compilation) : base(symbol, attribute, compilation) {
        ConfigureAwait = ReadBoolean(attribute, AttributeMetadata.ConfigureAwaitPropertyName);
    }

    public bool ConfigureAwait { get; }

    public override bool Equals(DisposeInfo? other) =>
        other is AsyncDisposeInfo asyncDisposeInfo
        && base.Equals(other)
        && ConfigureAwait == asyncDisposeInfo.ConfigureAwait;

    public override int GetHashCode() {
        unchecked {
            return (base.GetHashCode() * 397) ^ ConfigureAwait.GetHashCode();
        }
    }
}