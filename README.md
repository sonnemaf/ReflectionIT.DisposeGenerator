# ReflectionIT.DisposeGenerator

A source generator package that implements the Dispose and Async Dispose pattern.

- https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern 
- https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose 
- https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync

## NuGet package

| Package | Version |
| ------ | ------ |
| ReflectionIT.DisposeGenerator | [![NuGet](https://img.shields.io/nuget/v/ReflectionIT.DisposeGenerator)](https://www.nuget.org/packages/ReflectionIT.DisposeGenerator/) |         

## Usage

Install the NuGet package, then annotate a **partial** class or struct with the ```Disposable``` attribute.

Annotate properties or fields with the ```Dispose``` attribute. Use ```SetToNull``` when the property or field holds a large object and should be set to ```null``` after disposal.

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

This generates the following **partial** class, which disposes the ```StreamWriter``` property and sets it to ```null```.

```cs
partial class LogWriter
{
    public void Dispose() {
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

A class derived from a class that already implements ```IDisposable``` should not implement ```IDisposable``` again, because the base implementation of ```IDisposable.Dispose``` is inherited by derived classes.

Set for this derived **partial** class the ```OverrideDispose``` property of the ```Disposable``` attribute to ```true```. In that case, the public parameterless ```Dispose``` method is not generated, and the protected ```Dispose(bool)``` method is generated as an override instead.

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

This generates the following **partial** class, which disposes the ```SecondStreamWriter``` property.

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

## Unmanaged resources

You can also release unmanaged resources. Set the ```HasUnmanagedResources``` property of the ```Disposable``` attribute to ```true```.
Then implement the partial method ```ReleaseUnmanagedResources```, which releases the unmanaged resource.

If you need to work with unmanaged resources, we strongly recommend wrapping the unmanaged ```IntPtr``` handle in a [SafeHandle](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#safe-handles).

```cs
[Disposable(HasUnmanagedResources = true)]
public partial class LogWriterWithAnExtraIntPtr : IDisposable {

    private readonly IntPtr _pointer;

    [Dispose]
    private StreamWriter StreamWriter { get; }

    public LogWriterWithAnExtraIntPtr(string path) {
        StreamWriter = new StreamWriter(path);
        _pointer = Marshal.AllocHGlobal(cb: 128);
    }

    public void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");

    // Implement this partial method to release the unmanaged resources.
    protected virtual partial void ReleaseUnmanagedResources() => Marshal.FreeHGlobal(_pointer);
}
```

This generates the following partial class with a finalizer and a partial method named ```ReleaseUnmanagedResources``` that you must implement.

```cs
partial class LogWriterWithAnExtraIntPtr
{
    public void Dispose() {
        Dispose(disposing: true);
        global::System.GC.SuppressFinalize(this);
    }

    ~LogWriterWithAnExtraIntPtr() {
        Dispose(disposing: false);
    }

    protected virtual partial void ReleaseUnmanagedResources();

    private bool _isDisposed;

    protected virtual void Dispose(bool disposing) {
        if (!_isDisposed) {
            if (disposing) {
                StreamWriter?.Dispose();
            }
            ReleaseUnmanagedResources();
            _isDisposed = true;
        }
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

This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) file for details.
