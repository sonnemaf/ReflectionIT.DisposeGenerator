﻿namespace ReflectionIT.DisposeGenerator.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class CascadeDisposeAttribute : Attribute {
    public bool Ignore { get; set; }
    public bool SetToNull { get; set; }
}