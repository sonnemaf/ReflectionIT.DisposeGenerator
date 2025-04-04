namespace ReflectionIT.DisposeGenerator.Attributes;

/// <summary>
/// An attribute that indicates that for this type code is generated for the <see href="https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern">Dispose pattern</see>
/// <para>
/// For more info about the Dispose pattern please read:
/// <see href="https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose">Implement a Dispose method</see>
/// and
/// <see href="https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync">Implement a DisposeAsync method</see>
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class DisposableAttribute : Attribute
{
    /// <summary>
    /// Generates a Destructor which calls Dispose(false) if set to True
    /// </summary>
    public bool HasUnmangedResources { get; set; }

    /// <summary>
    /// Implements IAsyncDisposable, DisposeAsync() and DisposeCoreAsync()
    /// </summary>
    public bool GenerateDisposeAsync { get; set; }

    /// <summary>
    /// Generate OnDisposingAsyncCore() partial method which is called at the start of DisposeAsync().
    /// </summary>
    public bool GenerateOnDisposingAsync { get; set; }

    /// <summary>
    /// Generate GenerateOnDisposedAsyncCore partial method which is called at the end of DisposeAsync().
    /// </summary>
    public bool GenerateOnDisposedAsync { get; set; }

    /// <summary>
    /// Use ConfigureAwait(false or true) on async method calls
    /// </summary>
    public bool ConfigureAwait { get; set; }
}