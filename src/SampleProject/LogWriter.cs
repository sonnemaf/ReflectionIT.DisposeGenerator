using ReflectionIT.DisposeGenerator.Attributes;

namespace SampleProject;

[Disposable(GenerateDisposeAsync = true, ConfigureAwait = true, GenerateOnDisposedAsync = true)]
public partial class LogWriter {

    [Dispose(SetToNull = true)]
    private StreamWriter? _streamWriter1;

    [Dispose(SetToNull = false)]
    private StreamWriter StreamWriter2 { get; }

    //[Dispose]
    //private StreamWriter? _streamWriter3;

    public LogWriter(string path) {
        _streamWriter1 = new StreamWriter(path);
        StreamWriter2 = new StreamWriter($"path{2}");
        //_streamWriter3 = new StreamWriter(path);
    }

    public void Write(string text) {
        _streamWriter1?.WriteLine(text.ToUpper());
        StreamWriter2.WriteLine(text.ToLower());
    }

    //partial void OnDisposing(bool disposing) {

    //}

    //partial void OnDisposed(bool disposing) {

    //}

    protected virtual partial ValueTask OnDisposedAsyncCore() {
        return ValueTask.CompletedTask;
    }


}