using ReflectionIT.DisposeGenerator.Attributes;

namespace Project;

[Disposable]
//[Disposable(HasUnmangedResources = true)]
[AsyncDisposable]
//[AsyncDisposable(GenerateOnDisposingAsync = true, GenerateOnDisposedAsync = true)]
public partial class LogWriter {

    [CascadeDispose()]
    //[CascadeDispose(SetToNull = true)]
    //[CascadeDispose(SetToNull = true, Ignore = true)]
    private readonly StreamWriter _streamWriter;

    public LogWriter(string path) {
        _streamWriter = new StreamWriter(path);
    }

    public void Write(string text) => _streamWriter.WriteLine(text.ToUpper());

    //partial void OnDisposing(bool disposing) {

    //}

    //private partial ValueTask OnDisposedAsync() {
    //    return ValueTask.CompletedTask;
    //}

    //private partial ValueTask OnDisposingAsync() {
    //    return ValueTask.CompletedTask;
    //}

}