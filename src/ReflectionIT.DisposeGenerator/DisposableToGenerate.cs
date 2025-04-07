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
    public readonly bool ConfigureAwait;

    public DisposableToGenerate(
        string name,
        string ns,
        bool hasUnmangedResources,
        bool isSealed,
        bool implementDisposable,
        bool implementIAsyncDisposable,
        FieldOrPropertyToDispose[] fieldsOrProperties,
        bool generateOnDisposingAsync,
        bool generateOnDisposedAsync,
        bool configureAwait)
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
        ConfigureAwait = configureAwait;
    }
}
