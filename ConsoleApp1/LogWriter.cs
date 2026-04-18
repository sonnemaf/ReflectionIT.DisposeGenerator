namespace ConsoleApp1;

[Disposable]
public partial class LogWriter : IDisposable {

    [Dispose]
    private StreamWriter StreamWriter { get; }

    public LogWriter(string path) => StreamWriter = new StreamWriter(path);

    public void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");
}
