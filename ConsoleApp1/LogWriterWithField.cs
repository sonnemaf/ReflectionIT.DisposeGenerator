namespace ConsoleApp1;

[Disposable]
public partial class LogWriterWithField : IDisposable, IAsyncDisposable {

    [Dispose(SetToNull = true, Order = 0)]
    [AsyncDispose]
    private StreamWriter _streamWriter;

    [AsyncDispose(Order = 2)]
    private StreamWriter? _streamWriter2;

    [Dispose(Order = 1)]
    private StreamWriter? _streamWriter3;

    public LogWriterWithField(string path) => _streamWriter = new StreamWriter(path);

    public void WriteLine(string text) => _streamWriter.WriteLine($"{DateTime.Now}\t{text}");

}
