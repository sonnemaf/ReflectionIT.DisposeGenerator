using Microsoft.CodeAnalysis;

namespace ReflectionIT.DisposeGenerator;

public readonly struct FieldOrPropertyToDispose {

    public readonly string Name;
    public readonly bool IsProperty;
    public readonly Location? Location;
    public readonly ITypeSymbol Type;
    public readonly bool ImplementDisposable;
    public readonly bool ImplementIAsyncDisposable;
    public readonly bool SetToNull;

    public FieldOrPropertyToDispose(
        string name,
        bool isProperty,
        Location? location,
        ITypeSymbol type,
        bool implementDisposable,
        bool implementIAsyncDisposable,
        bool setToNull) {
        Name = name;
        IsProperty = isProperty;
        Location = location;
        Type = type;
        ImplementDisposable = implementDisposable;
        ImplementIAsyncDisposable = implementIAsyncDisposable;
        SetToNull = setToNull;
    }
}
