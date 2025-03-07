using Microsoft.CodeAnalysis;

namespace ReflectionIT.DisposeGenerator;

public readonly struct DisposableToGenerate
{
    public readonly string Name;
    public readonly string Namespace;
    public readonly bool HasUnmangedResources;
    public readonly bool IsSealed;
    public readonly bool ImplementDisposable;
    public readonly bool ImplementIAsyncDisposable;
    public readonly FieldOrPropertyToDispose[] FieldsOrProperties;
    public readonly bool GenerateOnDisposingAsync;
    public readonly bool GenerateOnDisposedAsync;

    public DisposableToGenerate(
        string name,
        string ns,
        bool hasUnmangedResources,
        bool isSealed,
        bool implementDisposable,
        bool implementIAsyncDisposable,
        FieldOrPropertyToDispose[] fieldsOrProperties,
        bool generateOnDisposingAsync,
        bool generateOnDisposedAsync)
    {
        Name = name;
        Namespace = ns;
        HasUnmangedResources = hasUnmangedResources;
        IsSealed = isSealed;
        ImplementDisposable = implementDisposable;
        ImplementIAsyncDisposable = implementIAsyncDisposable;
        FieldsOrProperties = fieldsOrProperties;
        GenerateOnDisposingAsync = generateOnDisposingAsync;
        GenerateOnDisposedAsync = generateOnDisposedAsync;
    }
}

public readonly struct FieldOrPropertyToDispose
{
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
        bool setToNull)
    {
        Name = name;
        IsProperty = isProperty;
        Location = location;
        Type = type;
        ImplementDisposable = implementDisposable;
        ImplementIAsyncDisposable = implementIAsyncDisposable;
        SetToNull = setToNull;
    }
}
