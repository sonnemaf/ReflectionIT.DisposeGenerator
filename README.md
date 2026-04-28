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

By default, the generator also creates a ```ThrowIfDisposed``` method that you can call from members such as ```WriteLine``` to guard against use after disposal. Set ```GenerateThrowIfDisposed = false``` to skip generating this helper.

## Recommended use of ThrowIfDisposed

Call ```ThrowIfDisposed()``` at the start of public instance members that depend on resources managed by the generated dispose pattern.

```cs
using ReflectionIT.DisposeGenerator.Attributes;

[Disposable]
public partial class LogWriter : IDisposable {

    [Dispose]
    private readonly StreamWriter _streamWriter;

    public LogWriter(string path) => _streamWriter = new StreamWriter(path);

    public void WriteLine(string text) {
        ThrowIfDisposed();
        _streamWriter.WriteLine($"{DateTime.Now}\t{text}");
    }
}
```

This helps fail fast with an ```ObjectDisposedException``` when the instance is used after it has been disposed.

The generated ```_isDisposed``` field tracks the disposal state and should not be modified manually. By default it is generated as a ```bool```. When ```IsThreadSafe = true``` is used, it is generated as an ```int``` so ```Interlocked.CompareExchange``` can be used safely.

Annotate properties or fields with the ```Dispose``` attribute. Use ```SetToNull``` when the property or field holds a large object and should be set to ```null``` after disposal.

```cs
using ReflectionIT.DisposeGenerator.Attributes;

[Disposable]
public partial class LogWriter : IDisposable {

    [Dispose(SetToNull = true)]
    private StreamWriter StreamWriter { get; set; }

    public LogWriter(string path) => StreamWriter = new StreamWriter(path);

    public void WriteLine(string text) {
        ThrowIfDisposed();
        StreamWriter.WriteLine($"{DateTime.Now}\t{text}");
    }
}
```

This generates the following **partial** class, which disposes the ```StreamWriter``` property and sets it to ```null```.

```cs
partial class LogWriter
{
    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    public void Dispose() {
        Dispose(disposing: true);
        global::System.GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tracks whether the current instance has been disposed. This field must not be modified manually.
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// Throws an exception if the current instance has been disposed.
    /// </summary>
    protected virtual void ThrowIfDisposed() {
        if (_isDisposed) {
            throw new global::System.ObjectDisposedException(nameof(LogWriter));
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
    protected virtual void Dispose(bool disposing) {
        if (_isDisposed) {
            return;
        }
        _isDisposed = true;
        if (disposing) {
            StreamWriter?.Dispose();
        }
        StreamWriter = null;
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
    /// <summary>
    /// Tracks whether the current instance has been disposed. This field must not be modified manually.
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// Throws an exception if the current instance has been disposed.
    /// </summary>
    protected override void ThrowIfDisposed() {
        if (_isDisposed) {
            throw new global::System.ObjectDisposedException(nameof(SecondLogWriter));
        }
        base.ThrowIfDisposed();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
    protected override void Dispose(bool disposing) {
        if (_isDisposed) {
            return;
        }
        _isDisposed = true;
        if (disposing) {
            SecondStreamWriter?.Dispose();
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
    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    public void Dispose() {
        Dispose(disposing: true);
        global::System.GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged resources held by the current instance.
    /// </summary>
    ~LogWriterWithAnExtraIntPtr() {
        Dispose(disposing: false);
    }

    /// <summary>
    /// Releases unmanaged resources held by the current instance.
    /// </summary>
    protected virtual partial void ReleaseUnmanagedResources();

    /// <summary>
    /// Tracks whether the current instance has been disposed. This field must not be modified manually.
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// Throws an exception if the current instance has been disposed.
    /// </summary>
    protected virtual void ThrowIfDisposed() {
        if (_isDisposed) {
            throw new global::System.ObjectDisposedException(nameof(LogWriterWithAnExtraIntPtr));
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
    protected virtual void Dispose(bool disposing) {
        if (_isDisposed) {
            return;
        }
        _isDisposed = true;
        if (disposing) {
            StreamWriter?.Dispose();
        }
        ReleaseUnmanagedResources();
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
    /// <summary>
    /// Releases all resources used by the current instance.
    /// </summary>
    public void Dispose() {
        Dispose(disposing: true);
        global::System.GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Detects redundant Dispose() calls in a thread-safe manner. _isDisposed == 0 means Dispose(bool) has not been called yet, and _isDisposed == 1 means Dispose(bool) has already been called. This field must not be modified manually.
    /// </summary>
    private int _isDisposed;

    /// <summary>
    /// Throws an exception if the current instance has been disposed.
    /// </summary>
    protected virtual void ThrowIfDisposed() {
        if (_isDisposed != 0) {
            throw new global::System.ObjectDisposedException(nameof(LogWriter));
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
    protected virtual void Dispose(bool disposing) {
        if (global::System.Threading.Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) {
            return;
        }
        if (disposing) {
            _streamWriter?.Dispose();
        }
    }

}
```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) file for details.
