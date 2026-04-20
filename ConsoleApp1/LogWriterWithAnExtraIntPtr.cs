namespace ConsoleApp1;

[Disposable(HasUnmanagedResources = true)]
public partial class LogWriterWithAnExtraIntPtr : IDisposable {

    private readonly IntPtr _pointer;

    [Dispose]
    private StreamWriter StreamWriter { get; }

    public LogWriterWithAnExtraIntPtr(string path) {
        StreamWriter = new StreamWriter(path);
        _pointer = global::System.Runtime.InteropServices.Marshal.AllocHGlobal(cb: 128);
    }

    public void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");

    // Implement this partial method to release the Unmanaged Resources
    partial void ReleaseUnmanagedResources() => global::System.Runtime.InteropServices.Marshal.FreeHGlobal(_pointer);
}