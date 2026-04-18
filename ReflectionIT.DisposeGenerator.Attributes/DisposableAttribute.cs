namespace ReflectionIT.DisposeGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class DisposableAttribute : Attribute {

    public bool IsThreadSafe { get; set; }
}
