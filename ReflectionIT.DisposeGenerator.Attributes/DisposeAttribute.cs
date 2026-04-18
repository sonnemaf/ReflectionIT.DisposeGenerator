namespace ReflectionIT.DisposeGenerator.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class DisposeAttribute : Attribute {

    public bool SetToNull { get; }

    public DisposeAttribute(bool setToNull = false) {
        SetToNull = setToNull;
    }

}