namespace ReflectionIT.DisposeGenerator.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class DisposeAttribute : Attribute {

    /// <summary>
    /// Set the large object to null in the Dispose() method
    /// </summary>
    public bool SetToNull { get; set; }
}