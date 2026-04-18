namespace ConsoleApp1;

[Disposable(IsThreadSafe = true)]
public sealed partial class LogWriterWithField : IDisposable {

    [Dispose(setToNull: true)]
    private StreamWriter _streamWriter;

    public LogWriterWithField(string path) => _streamWriter = new StreamWriter(path);

    public void WriteLine(string text) => _streamWriter.WriteLine($"{DateTime.Now}\t{text}");

}
