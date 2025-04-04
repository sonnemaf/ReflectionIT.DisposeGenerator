namespace ReflectionIT.DisposeGenerator.Attributes;

/// <summary>
/// Implementing <see cref="IDisposable"/>System.IDisposable</see> interface in the recommended way.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class DisposableAttribute : Attribute
{
    public bool HasUnmangedResources { get; set; }
}