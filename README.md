# ReflectionIT.DisposeGenerator

A source generator package that implements the dispose and async dispose patterns.

- https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
- https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose
- https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync

## NuGet package

| Package | Version |
| ------ | ------ |
| ReflectionIT.DisposeGenerator | [![NuGet](https://img.shields.io/nuget/v/ReflectionIT.DisposeGenerator)](https://www.nuget.org/packages/ReflectionIT.DisposeGenerator/) |

## Installation

```xml
<PackageReference Include="ReflectionIT.DisposeGenerator" Version="*" />
```

## Quick start

Annotate a **partial** class or struct with the `Disposable` attribute and mark disposable fields or properties with `Dispose`.

```cs
using System;
using System.IO;
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

The generator creates the dispose members for the annotated type, including `_isDisposed`, an `IsDisposed` property, and `ThrowIfDisposed()` by default.

## Requirements and diagnostics

- The annotated type must be `partial`.
- `[Disposable]` can be applied to classes and structs.
- `[Dispose]` and `[AsyncDispose]` can be applied to fields and properties.
- `Order` can be used on `[Dispose]` and `[AsyncDispose]` to control disposal order. Members without an explicit `Order` are disposed last.
- The generator emits `RITDG001` when a type annotated with `[Disposable]` is not declared `partial`.
- Members annotated with `[Dispose]` or `[AsyncDispose]` must support the generated dispose call pattern. Otherwise the generated code can produce compiler errors.

### RITDG001

`RITDG001`: Type `'{typeName}'` is annotated with `[Disposable]` and must be declared partial for ReflectionIT.DisposeGenerator to generate code.

## Attribute reference

### `DisposableAttribute`

| Property | Default | Description |
| --- | --- | --- |
| `OverrideDispose` | `false` | Generates `Dispose(bool)` as an override instead of generating a public `Dispose()` method. |
| `OverrideDisposeAsyncCore` | `false` | Generates `DisposeAsyncCore()` as an override instead of generating a public `DisposeAsync()` method. |
| `GenerateThrowIfDisposed` | `true` | Generates `ThrowIfDisposed()` for guarding public instance members. |
| `ExplicitInterfaceImplementation` | `false` | Generates explicit `IDisposable.Dispose()` and `IAsyncDisposable.DisposeAsync()` implementations when applicable. |
| `IsThreadSafe` | `false` | Uses thread-safe disposal state transitions via `Interlocked.CompareExchange`. |
| `HasUnmanagedResources` | `false` | Adds a finalizer and `ReleaseUnmanagedResources()` partial method support. |

### `DisposeAttribute`

| Property | Default | Description |
| --- | --- | --- |
| `SetToNull` | `false` | Sets the annotated field or property to `null` after disposal. |
| `Order` | `0` | Controls the order in which annotated members are disposed. Members with an explicit `Order` are disposed from lowest to highest order. Members without an explicit `Order` are disposed last. |

### `AsyncDisposeAttribute`

| Property | Default | Description |
| --- | --- | --- |
| `SetToNull` | `false` | Sets the annotated field or property to `null` after asynchronous disposal. |
| `ConfigureAwait` | `true` | Controls the `ConfigureAwait(...)` value used for generated async disposal calls. |
| `Order` | `0` | Controls the order in which annotated members are asynchronously disposed. Members with an explicit `Order` are disposed from lowest to highest order. Members without an explicit `Order` are disposed last. |

## What gets generated

Depending on the options and the annotated members, the generator can create:

- `Dispose()`
- `Dispose(bool)`
- `DisposeAsync()`
- `DisposeAsyncCore()`
- `IsDisposed`
- `ThrowIfDisposed()`
- `_isDisposed`
- a finalizer
- `ReleaseUnmanagedResources()`

## Recommended use of `ThrowIfDisposed`

Call `ThrowIfDisposed()` at the start of public instance members that depend on resources managed by the generated dispose pattern. The generated method uses the generated `IsDisposed` property, so derived types can customize disposed-state checks through `IsDisposed` instead of overriding `ThrowIfDisposed()`.

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

This helps fail fast with an `ObjectDisposedException` when the instance is used after it has been disposed.

## `SetToNull` usage

Use `SetToNull` when the property or field should be set to `null` after disposal.

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

This generates the following partial class, which disposes the `StreamWriter` property and sets it to `null`.

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
    /// Gets a value indicating whether the current instance has been disposed.
    /// </summary>
    protected virtual bool IsDisposed => _isDisposed;

    /// <summary>
    /// Throws an exception if the current instance has been disposed.
    /// </summary>
    protected void ThrowIfDisposed() {
        if (IsDisposed) {
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

## Dispose order

Use `Order` when disposable members must be disposed in a specific order.

Members with an explicit `Order` are disposed from the lowest order value to the highest order value. Members without an explicit `Order` are disposed last.

```cs
using System;
using System.IO;
using ReflectionIT.DisposeGenerator.Attributes;

[Disposable]
public partial class LogWriterWithField : IDisposable, IAsyncDisposable {

    [Dispose(SetToNull = true, Order = 0)]
    [AsyncDispose]
    private StreamWriter _streamWriter;

    [AsyncDispose(Order = 2)]
    private StreamWriter? _streamWriter2;

    [Dispose(Order = 1)]
    private StreamWriter? _streamWriter3;

    public LogWriterWithField(string path) => _streamWriter = new StreamWriter(path);

    public void WriteLine(string text) => _streamWriter.WriteLine($"{DateTime.Now}\t{text}");
}
```

In this example:

- `_streamWriter` is disposed first during synchronous disposal because `[Dispose]` has `Order = 0`.
- `_streamWriter3` is disposed after `_streamWriter` during synchronous disposal because `[Dispose]` has `Order = 1`.
- `_streamWriter2` is disposed asynchronously with `Order = 2`.
- `_streamWriter` has `[AsyncDispose]` without an explicit `Order`, so it is asynchronously disposed after explicitly ordered async members.

`Order` is evaluated separately for `[Dispose]` and `[AsyncDispose]`. A member can have both attributes, and each attribute can define its own disposal order.

```cs
[Dispose(Order = 1)]
[AsyncDispose(Order = 0)]
private SomeResource _resource;
```

`Order` is an `int` attribute property. To detect whether the user explicitly specified an order, the generator checks whether `Order` is present in the attribute's named arguments.

```cs
public int Order { get; set; }
```

## Async dispose

Use `AsyncDispose` for fields or properties that support `DisposeAsync()`.

```cs
using System;
using System.IO;
using ReflectionIT.DisposeGenerator.Attributes;

[Disposable]
public partial class LogWriter : IAsyncDisposable {

    [AsyncDispose]
    private readonly StreamWriter _streamWriter;

    public LogWriter(string path) => _streamWriter = new StreamWriter(path);
}
```

This generates `DisposeAsync()` and `DisposeAsyncCore()`. If the same member also has `[Dispose]`, the generator supports both sync and async cleanup patterns.

```cs
partial class LogWriter
{
    /// <summary>
    /// Asynchronously releases all resources used by the current instance.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous dispose operation.
    /// </returns>
    public async global::System.Threading.Tasks.ValueTask DisposeAsync() {
        await DisposeAsyncCore().ConfigureAwait(false);
        global::System.GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tracks whether the current instance has been disposed. This field must not be modified manually.
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// Gets a value indicating whether the current instance has been disposed.
    /// </summary>
    protected virtual bool IsDisposed => _isDisposed;

    /// <summary>
    /// Throws an exception if the current instance has been disposed.
    /// </summary>
    protected void ThrowIfDisposed() {
        if (IsDisposed) {
            throw new global::System.ObjectDisposedException(nameof(LogWriter));
        }
    }

    /// <summary>
    /// Asynchronously releases the resources used by the current instance.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous dispose operation.
    /// </returns>
    protected virtual async global::System.Threading.Tasks.ValueTask DisposeAsyncCore() {
        if (_isDisposed) {
            return;
        }
        _isDisposed = true;
        if (_streamWriter != null) {
            await _streamWriter.DisposeAsync().ConfigureAwait(false);
        }
    }

}
```

## Implement the dispose pattern for a derived class

A class derived from a class that already implements `IDisposable` should not implement `IDisposable` again, because the base implementation of `IDisposable.Dispose` is inherited by derived classes.

Set `OverrideDispose = true` for the derived partial class. In that case, the public parameterless `Dispose()` method is not generated, and the protected `Dispose(bool)` method is generated as an override instead.

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

This generates the following partial class, which disposes the `SecondStreamWriter` property.

```cs
partial class SecondLogWriter
{
    /// <summary>
    /// Tracks whether the current instance has been disposed. This field must not be modified manually.
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// Gets a value indicating whether the current instance has been disposed.
    /// </summary>
    protected override bool IsDisposed => _isDisposed || base.IsDisposed;

    /// <summary>
    /// Throws an exception if the current instance has been disposed.
    /// </summary>
    protected void ThrowIfDisposed() {
        if (IsDisposed) {
            throw new global::System.ObjectDisposedException(nameof(SecondLogWriter));
        }
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

Set `HasUnmanagedResources = true` to include unmanaged resource cleanup support.
Then implement the partial method `ReleaseUnmanagedResources()`, which releases the unmanaged resource.

If you need to work with unmanaged resources, it is strongly recommended to wrap the unmanaged `IntPtr` handle in a [SafeHandle](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#safe-handles).

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

    protected virtual partial void ReleaseUnmanagedResources() => Marshal.FreeHGlobal(_pointer);
}
```

This generates the following partial class with a finalizer and a partial method named `ReleaseUnmanagedResources()` that you must implement.

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
    /// Gets a value indicating whether the current instance has been disposed.
    /// </summary>
    protected virtual bool IsDisposed => _isDisposed;

    /// <summary>
    /// Throws an exception if the current instance has been disposed.
    /// </summary>
    protected void ThrowIfDisposed() {
        if (IsDisposed) {
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

Use `IsThreadSafe = true` to ensure thread-safe disposal via `Interlocked.CompareExchange`.

```cs
[Disposable(IsThreadSafe = true)]
public partial class LogWriter : IDisposable {

    [Dispose]
    private readonly StreamWriter _streamWriter;

    public LogWriter(string path) => _streamWriter = new StreamWriter(path);

    public void WriteLine(string text) => _streamWriter.WriteLine($"{DateTime.Now}\t{text}");
}
```

This generates the following partial class, which uses `Interlocked.CompareExchange` to ensure thread-safe disposal.

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
    /// Gets a value indicating whether the current instance has been disposed.
    /// </summary>
    protected virtual bool IsDisposed => _isDisposed != 0;

    /// <summary>
    /// Throws an exception if the current instance has been disposed.
    /// </summary>
    protected void ThrowIfDisposed() {
        if (IsDisposed) {
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

## Troubleshooting

### Why do I get `RITDG001`?

The type marked with `[Disposable]` is not declared `partial`. Add the `partial` keyword to the class or struct declaration.

### Why do I get compiler errors for `Dispose()` or `DisposeAsync()` on an annotated member?

The generator emits calls to `Dispose()` for `[Dispose]` members and `DisposeAsync()` for `[AsyncDispose]` members. Make sure the annotated member supports the corresponding API.

### Why are members without `Order` disposed last?

`Order` is an `int` attribute property, because nullable attribute parameters such as `int?` are not supported by C# attributes.

The generator detects whether `Order` was explicitly specified in the attribute usage. Members with an explicit `Order` are sorted first. Members without an explicit `Order` are disposed last.

### Why was no code generated?

Common reasons:

- the type is not `partial`
- no fields or properties were annotated with `[Dispose]` or `[AsyncDispose]`
- the type only has invalid attribute usage that prevents successful compilation

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) file for details.