namespace ConsoleApp1;

[Disposable]
public partial class LogWriterWithField : IDisposable, IAsyncDisposable {

    [Dispose(SetToNull = true)]
    [AsyncDispose]
    private StreamWriter _streamWriter;

    public LogWriterWithField(string path) => _streamWriter = new StreamWriter(path);

    public void WriteLine(string text) => _streamWriter.WriteLine($"{DateTime.Now}\t{text}");

}
