namespace ReflectionIT.DisposeGenerator.Attributes;

/// <summary>
/// Marks a class or struct for dispose-pattern source generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class DisposableAttribute : Attribute {

    /// <summary>
    /// Gets or sets a value indicating whether <c>Dispose(bool)</c> should be generated as an override.
    /// </summary>
    public bool OverrideDispose { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <c>DisposeAsyncCore()</c> should be generated as an override.
    /// </summary>
    public bool OverrideDisposeAsyncCore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a <c>ThrowIfDisposed()</c> helper method should be generated.
    /// </summary>
    public bool GenerateThrowIfDisposed { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether generated dispose methods should use explicit interface implementation.
    /// </summary>
    public bool ExplicitInterfaceImplementation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether disposal should use thread-safe state transitions.
    /// </summary>
    public bool IsThreadSafe { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether unmanaged resource cleanup support should be generated.
    /// </summary>
    public bool HasUnmanagedResources { get; set; }

}
