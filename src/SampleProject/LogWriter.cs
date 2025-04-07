using ReflectionIT.DisposeGenerator.Attributes;

namespace SampleProject;

[Disposable(Mode = DisposeMode.Auto, GenerateDisposeAsync = true, ConfigureAwait = true, GenerateOnDisposedAsync = true, HasUnmangedResources = true)]
public partial class LogWriter {

    //[Dispose(SetToNull = true)]
    private readonly string _path;

    //[Dispose(SetToNull = true)]
    private StreamWriter? _streamWriter1;

    //[Dispose(SetToNull = false, Ignore = true)]
    //[Dispose(SetToNull = false)]
    public StreamWriter StreamWriter2 { get; }

    //[Dispose]
    //private StreamWriter? _streamWriter3;

    public LogWriter(string path) {
        _path = path;
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


    partial void ReleaseUnmangedResources() {
        //throw new NotImplementedException();
    }

}