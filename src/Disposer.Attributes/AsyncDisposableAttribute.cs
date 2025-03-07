namespace ReflectionIT.DisposeGenerator.Attributes;

/// <summary>
/// Implementing <see cref="System.IAsyncDisposable"/>System.IAsyncDisposable</see> interface in the recommended way.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class AsyncDisposableAttribute : Attribute {
    /// <summary>
    /// Generate OnDisposingAsync() partial method which is called at the start of DisposeAsync().
    /// </summary>
    public bool GenerateOnDisposingAsync { get; set; }

    /// <summary>
    /// Generate GenerateOnDisposedAsync partial method which is called at the end of DisposeAsync().
    /// </summary>
    public bool GenerateOnDisposedAsync { get; set; }
}