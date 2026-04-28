namespace ReflectionIT.DisposeGenerator.Attributes;

/// <summary>
/// Marks a field or property for asynchronous disposal in generated code.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class AsyncDisposeAttribute : DisposeAttribute {

    /// <summary>
    /// Gets or sets a value indicating whether generated asynchronous disposal calls should use <c>ConfigureAwait(true)</c>.
    /// </summary>
    public bool ConfigureAwait { get; set; } = true;
}

