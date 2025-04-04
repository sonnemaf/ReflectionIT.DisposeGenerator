using ReflectionIT.DisposeGenerator.Attributes;

namespace Project;

[Disposable(GenerateDisposeAsync = true, ConfigureAwait = true, GenerateOnDisposedAsync = true)]
public partial class LogWriter {

    [Dispose]
    //[Dispose(SetToNull = true)]
    private readonly StreamWriter _streamWriter;

    public LogWriter(string path) {
        _streamWriter = new StreamWriter(path);
    }

    public void Write(string text) => _streamWriter.WriteLine(text.ToUpper());

    partial void OnDisposing(bool disposing) {

    }

    partial void OnDisposed(bool disposing) {
        
    }

    protected virtual partial ValueTask OnDisposedAsyncCore() {
        return ValueTask.CompletedTask;
    }

}