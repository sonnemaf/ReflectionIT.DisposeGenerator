namespace ConsoleApp1;

[Disposable]
public partial class LogWriter : IDisposable {

    [Dispose]
    private readonly StreamWriter _streamWriter;

    public LogWriter(string path) => this._streamWriter = new StreamWriter(path);

    public void WriteLine(string text) {
        ThrowIfDisposed();
        ThrowIfDisposed();
        ThrowIfDisposed();

        Console.WriteLine(557);
        Console.WriteLine(557);
        Console.WriteLine(557);
        Console.WriteLine(557);
        Console.WriteLine(557);

        _streamWriter.WriteLine($"{DateTime.Now}\t{text}");
    }
}
