# ReflectionIT.DisposeGenerator

A source generator package that generates the Dispose pattern

- https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern 
- https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose 


# NuGet package

| Package | Version |
| ------ | ------ |
| ReflectionIT.DisposeGenerator | [![NuGet](https://img.shields.io/nuget/v/ReflectionIT.DisposeGenerator)](https://www.nuget.org/packages/ReflectionIT.DisposeGenerator/) |         

## Example

Install the NuGet package and write the following code:

```cs
using ReflectionIT.DisposeGenerator.Attributes;

[Disposable]
public partial class LogWriter : IDisposable {

    [Dispose]
    private StreamWriter StreamWriter { get; }

    public LogWriter(string path) => StreamWriter = new StreamWriter(path);

    public void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");
}
```

This generates the following partial class which disposes the StreamWriter property (or field)

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
            _isDisposed = true;
        }
    }
}
```

## Attributes

- DisposableAttribute
- DisposeAttribute

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.txt) file for details.