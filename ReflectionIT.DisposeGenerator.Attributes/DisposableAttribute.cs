namespace ReflectionIT.DisposeGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class DisposableAttribute : Attribute {

    public bool OverrideDispose { get; set; }
    public bool OverrideDisposeAsyncCore { get; set; }
    public bool GenerateThrowIfDisposed { get; set; } = true;

    public bool ExplicitInterfaceImplementation { get; set; }

    public bool IsThreadSafe { get; set; }

    public bool HasUnmanagedResources { get; set; }

}
