# ReflectionIT.DisposeGenerator

A source generator package that implements the Dispose pattern.

- https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern 
- https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose 

Planned future support includes the async dispose pattern and unmanaged resources.

https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync

## NuGet package

| Package | Version |
| ------ | ------ |
| ReflectionIT.DisposeGenerator | [![NuGet](https://img.shields.io/nuget/v/ReflectionIT.DisposeGenerator)](https://www.nuget.org/packages/ReflectionIT.DisposeGenerator/) |         

## Usage

Install the NuGet package, then annotate a class or struct with the ```Disposable``` attribute.

Annotate properties or fields with the ```Dispose``` attribute. Use the ```SetToNull``` when the property or field holds a large object and should be set to ```null``` after disposal.

```cs
using ReflectionIT.DisposeGenerator.Attributes;

[Disposable]
public partial class LogWriter : IDisposable {

    [Dispose(SetToNull = true)]
    private StreamWriter StreamWriter { get; set; }

    public LogWriter(string path) => StreamWriter = new StreamWriter(path);

    public void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");
}
```

This generates the following partial class, which disposes the ```StreamWriter``` property and sets it to ```null```.

```cs
partial class LogWriter
{
    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        global::System.GC.SuppressFinalize(this);
    }

    private bool _isDisposed;

    protected virtual void Dispose(bool disposing) {
        if (!_isDisposed) {
            if (disposing) {
                StreamWriter?.Dispose();
            }
            StreamWriter = null;
            _isDisposed = true;
        }
    }
}
```

## Implement the dispose pattern for a derived class

A class derived from a class that already implements ```IDisposable``` should not implement ```IDisposable``` again, because the base class implementation of ```IDisposable.Dispose``` is inherited by derived classes.

Set the ```OverrideDispose``` property of the ```Disposable``` attribute to ```true```. In that case, the public parameterless ```Dispose``` method is not generated, and the protected ```Dispose(bool)``` method is generated as an override.

```cs
[Disposable(OverrideDispose = true)]
public partial class SecondLogWriter : LogWriter {
                
    [Dispose]
    private StreamWriter SecondStreamWriter { get; }
                
    public SecondLogWriter(string path) : base(path) => SecondStreamWriter = new StreamWriter(path + "2");
                
    public override void WriteLine(string text) {
        base.WriteLine(text);
        SecondStreamWriter.WriteLine($"{DateTime.Now}\t{text.ToUpper()}");
    }
}
```

This generates the following partial class, which disposes the ```SecondStreamWriter``` property.

```cs
partial class SecondLogWriter
{
    private bool _isDisposed;

    protected override void Dispose(bool disposing) {
        if (!_isDisposed) {
            if (disposing) {
                SecondStreamWriter?.Dispose();
            }
            _isDisposed = true;
        }
        base.Dispose(disposing);
    }
}
```

## Thread-safe disposal

Use ```Interlocked.CompareExchange``` to ensure thread-safe disposal. Set the ```IsThreadSafe``` property of the ```Disposable``` attribute to ```true```.

```cs
[Disposable(IsThreadSafe = true)]
public partial class LogWriter : IDisposable {

    [Dispose]
    private readonly StreamWriter _streamWriter;

    public LogWriter(string path) => _streamWriter = new StreamWriter(path);

    public void WriteLine(string text) => _streamWriter.WriteLine($"{DateTime.Now}\t{text}");
}
```

This generates the following partial class, which uses ```Interlocked.CompareExchange``` to ensure thread-safe disposal.

```cs
partial class LogWriter
{
    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        global::System.GC.SuppressFinalize(this);
    }

    private int _isDisposed;

    protected virtual void Dispose(bool disposing) {
        if (global::System.Threading.Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0) {
            if (disposing) {
                _streamWriter?.Dispose();
            }
        }
    }
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.txt) file for details.