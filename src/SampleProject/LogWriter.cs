using ReflectionIT.DisposeGenerator.Attributes;

namespace Project;

[Disposable(GenerateDisposeAsync = true)]
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

    //private partial ValueTask OnDisposedAsync() {
    //    return ValueTask.CompletedTask;
    //}

    //private partial ValueTask OnDisposingAsync() {
    //    return ValueTask.CompletedTask;
    //}

}