namespace ReflectionIT.DisposeGenerator;

internal static class AttributeMetadata {

    public const string AttributesNamespace = "ReflectionIT.DisposeGenerator.Attributes";

    public const string DisposableAttributeName = AttributesNamespace + ".DisposableAttribute";
    public const string DisposeAttributeName = AttributesNamespace + ".DisposeAttribute";
    public const string AsyncDisposeAttributeName = AttributesNamespace + ".AsyncDisposeAttribute";

    public const string ConfigureAwaitPropertyName = "ConfigureAwait";
    public const string ExplicitInterfaceImplementationPropertyName = "ExplicitInterfaceImplementation";
    public const string GenerateThrowIfDisposedPropertyName = "GenerateThrowIfDisposed";
    public const string HasUnmanagedResourcesPropertyName = "HasUnmanagedResources";
    public const string IsThreadSafePropertyName = "IsThreadSafe";
    public const string OverrideDisposePropertyName = "OverrideDispose";
    public const string OverrideDisposeAsyncCorePropertyName = "OverrideDisposeAsyncCore";
    public const string SetToNullPropertyName = "SetToNull";
}
