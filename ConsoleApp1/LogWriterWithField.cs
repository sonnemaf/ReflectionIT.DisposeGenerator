//namespace ConsoleApp1;

//[Disposable]
//public partial class LogWriterWithField(string path) : IDisposable, IAsyncDisposable {

//    [Dispose(SetToNull = true)]
//    [AsyncDispose]
//    private StreamWriter _streamWriter = new StreamWriter(path);

//    [AsyncDispose]
//    private StreamWriter? _streamWriter2 = new StreamWriter(path);

//    [Dispose]
//    private StreamWriter? _streamWriter3 = new StreamWriter(path);

//    public void WriteLine(string text) {
//        _streamWriter.WriteLine($"{DateTime.Now}\t{text}");
//        _streamWriter3?.WriteLine($"{DateTime.Now}\t{text}");
//    }

//    public async Task WriteLineAsync(string text) {
//        if (_streamWriter2 is not null) {
//            await _streamWriter2.WriteLineAsync($"{DateTime.Now}\t{text}");
//        }
//    }
//}
