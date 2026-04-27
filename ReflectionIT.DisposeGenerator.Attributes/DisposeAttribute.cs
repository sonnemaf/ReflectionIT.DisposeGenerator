namespace ReflectionIT.DisposeGenerator.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class DisposeAttribute : Attribute {

    public bool SetToNull { get; set; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class AsyncDisposeAttribute : DisposeAttribute {

    public bool ConfigureAwait { get; set; } = true;
}

