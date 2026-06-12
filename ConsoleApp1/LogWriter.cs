namespace ConsoleApp1;

[Disposable]
public partial class LogWriter : IDisposable {

    [Dispose]
    private readonly StreamWriter _streamWriter;

    public LogWriter(string path) => _streamWriter = new StreamWriter(path);

    public void WriteLine(string text) {
        ThrowIfDisposed();
        Console.WriteLine(17);
        _streamWriter.WriteLine($"{DateTime.Now}\t{text}");
    }
}
