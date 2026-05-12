namespace ReflectionIT.DisposeGenerator.Attributes;

/// <summary>
/// Marks a field or property for synchronous disposal in generated code.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class DisposeAttribute : Attribute {

    /// <summary>
    /// Gets or sets a value indicating whether the member should be set to <see langword="null"/> after disposal.
    /// </summary>
    public bool SetToNull { get; set; }

    /// <summary>
    /// Specifies the order in which this member is disposed.
    /// </summary>
    public int Order { get; set; } = int.MaxValue;
}

