using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace ReflectionIT.DisposeGenerator.Tests;

public class TestDisposeGenerator {

    [Fact]
    public async Task TestDisposableDerived() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;
                
                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable]
                    public partial class LogWriter : IDisposable {

                        [Dispose]
                        private StreamWriter StreamWriter { get; }

                        public LogWriter(string path) => StreamWriter = new StreamWriter(path);

                        public virtual void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");

                    }

                    [Disposable(OverrideDispose = true)]
                    public partial class SecondLogWriter : LogWriter {
                
                        [Dispose]
                        private StreamWriter SecondStreamWriter { get; }
                
                        public SecondLogWriter(string path) : base(path) => SecondStreamWriter = new StreamWriter(path);
                
                        public override void WriteLine(string text) {
                            base.WriteLine(text);
                            SecondStreamWriter.WriteLine($"{DateTime.Now}\t{text.ToUpper()}");
                        }
                    }
                }
                """,
        };

        // List of expected generated sources
        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected virtual void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                StreamWriter?.Dispose();
                            }
                        }


                    }
                }

                """)));

        // List of expected generated sources
        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.SecondLogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class SecondLogWriter
                    {
                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected override void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(SecondLogWriter));
                            }
                            base.ThrowIfDisposed();
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected override void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                SecondStreamWriter?.Dispose();
                            }
                            base.Dispose(disposing);
                        }

                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestDisposeProperty() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;
                
                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable]
                    public partial class LogWriter : IDisposable {

                        [Dispose]
                        private StreamWriter StreamWriter { get; }

                        public LogWriter(string path) => StreamWriter = new StreamWriter(path);

                        public void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");

                    }
                }
                """,
        };

        // List of expected generated sources
        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected virtual void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                StreamWriter?.Dispose();
                            }
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestDisposeField() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;
                
                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable]
                    public partial class LogWriter : IDisposable {

                        [Dispose(SetToNull = true)]
                        private StreamWriter _streamWriter;

                        public LogWriter(string path) => _streamWriter = new StreamWriter(path);

                        public void WriteLine(string text) => _streamWriter.WriteLine($"{DateTime.Now}\t{text}");

                    }
                }
                """,
        };

        // List of expected generated sources
        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected virtual void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                _streamWriter?.Dispose();
                            }
                            _streamWriter = null;
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestSyncAndAsyncDisposeField() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;
                
                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable]
                    public partial class LogWriter : IDisposable, IAsyncDisposable {

                        [Dispose]
                        [AsyncDispose]
                        private StreamWriter _streamWriter;

                        public LogWriter(string path) => _streamWriter = new StreamWriter(path);

                        public void WriteLine(string text) => _streamWriter.WriteLine($"{DateTime.Now}\t{text}");

                    }
                }
                """,
        };

        // List of expected generated sources
        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Asynchronously releases all resources used by the current instance.
                        /// </summary>
                        /// <returns>
                        /// A task that represents the asynchronous dispose operation.
                        /// </returns>
                        public async global::System.Threading.Tasks.ValueTask DisposeAsync() {
                            await DisposeAsyncCore().ConfigureAwait(false);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected virtual void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                _streamWriter?.Dispose();
                            }
                        }

                        /// <summary>
                        /// Asynchronously releases the resources used by the current instance.
                        /// </summary>
                        /// <returns>
                        /// A task that represents the asynchronous dispose operation.
                        /// </returns>
                        protected virtual async global::System.Threading.Tasks.ValueTask DisposeAsyncCore() {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (_streamWriter != null) {
                                await _streamWriter.DisposeAsync().ConfigureAwait(false);
                            }
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestExplicitInterfaceImplementation() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;
                
                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable(ExplicitInterfaceImplementation = true)]
                    public partial class LogWriter : IDisposable {

                        [Dispose]
                        private StreamWriter StreamWriter { get; }

                        public LogWriter(string path) => StreamWriter = new StreamWriter(path);

                        public void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");

                    }
                }
                """,
        };

        // List of expected generated sources
        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        void global::System.IDisposable.Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected virtual void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                StreamWriter?.Dispose();
                            }
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestHasUnmanagedResources() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;
                
                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

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

                        protected virtual partial void ReleaseUnmanagedResources() => global::System.Runtime.InteropServices.Marshal.FreeHGlobal(_pointer);
                    }
                }
                """,
        };

        // List of expected generated sources
        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriterWithAnExtraIntPtr.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriterWithAnExtraIntPtr
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Releases unmanaged resources held by the current instance.
                        /// </summary>
                        ~LogWriterWithAnExtraIntPtr() {
                            Dispose(disposing: false);
                        }

                        /// <summary>
                        /// Releases unmanaged resources held by the current instance.
                        /// </summary>
                        protected virtual partial void ReleaseUnmanagedResources();

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriterWithAnExtraIntPtr));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected virtual void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                StreamWriter?.Dispose();
                            }
                            ReleaseUnmanagedResources();
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestThreadSafeDispose() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;
                
                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable(IsThreadSafe = true)]
                    public partial class LogWriter : IDisposable {

                        [Dispose]
                        private StreamWriter StreamWriter { get; }

                        public LogWriter(string path) => StreamWriter = new StreamWriter(path);

                        public void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");
                    }
                }
                """,
        };

        // List of expected generated sources
        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Detects redundant Dispose() calls in a thread-safe manner. _isDisposed == 0 means Dispose(bool) has not been called yet, and _isDisposed == 1 means Dispose(bool) has already been called. This field must not be modified manually.
                        /// </summary>
                        private int _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed != 0) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected virtual void Dispose(bool disposing) {
                            if (global::System.Threading.Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) {
                                return;
                            }
                            if (disposing) {
                                StreamWriter?.Dispose();
                            }
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestNoThrowIfDisposedWhenDisabled() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;

                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable(GenerateThrowIfDisposed = false)]
                    public partial class LogWriter : IDisposable {

                        [Dispose]
                        private StreamWriter StreamWriter { get; }

                        public LogWriter(string path) => StreamWriter = new StreamWriter(path);

                        public void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");
                    }
                }
                """,
        };

        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected virtual void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                StreamWriter?.Dispose();
                            }
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestAsyncDisposableDerivedOverride() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;

                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable]
                    public partial class LogWriter : IAsyncDisposable {

                        [AsyncDispose]
                        private StreamWriter StreamWriter { get; }

                        public LogWriter(string path) => StreamWriter = new StreamWriter(path);

                        public virtual void WriteLine(string text) => StreamWriter.WriteLine($"{DateTime.Now}\t{text}");
                    }

                    [Disposable(OverrideDisposeAsyncCore = true)]
                    public partial class SecondLogWriter : LogWriter {

                        [AsyncDispose]
                        private StreamWriter SecondStreamWriter { get; }

                        public SecondLogWriter(string path) : base(path) => SecondStreamWriter = new StreamWriter(path + "2");

                        public override void WriteLine(string text) {
                            base.WriteLine(text);
                            SecondStreamWriter.WriteLine($"{DateTime.Now}\t{text.ToUpper()}");
                        }
                    }
                }
                """,
        };

        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Asynchronously releases all resources used by the current instance.
                        /// </summary>
                        /// <returns>
                        /// A task that represents the asynchronous dispose operation.
                        /// </returns>
                        public async global::System.Threading.Tasks.ValueTask DisposeAsync() {
                            await DisposeAsyncCore().ConfigureAwait(false);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Asynchronously releases the resources used by the current instance.
                        /// </summary>
                        /// <returns>
                        /// A task that represents the asynchronous dispose operation.
                        /// </returns>
                        protected virtual async global::System.Threading.Tasks.ValueTask DisposeAsyncCore() {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (StreamWriter != null) {
                                await StreamWriter.DisposeAsync().ConfigureAwait(false);
                            }
                        }


                    }
                }

                """)));

        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.SecondLogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class SecondLogWriter
                    {
                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected override void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(SecondLogWriter));
                            }
                            base.ThrowIfDisposed();
                        }

                        /// <summary>
                        /// Asynchronously releases the resources used by the current instance.
                        /// </summary>
                        /// <returns>
                        /// A task that represents the asynchronous dispose operation.
                        /// </returns>
                        protected override async global::System.Threading.Tasks.ValueTask DisposeAsyncCore() {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (SecondStreamWriter != null) {
                                await SecondStreamWriter.DisposeAsync().ConfigureAwait(false);
                            }
                            await base.DisposeAsyncCore().ConfigureAwait(false);
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestDisposeStructField() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;

                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable]
                    public partial struct LogWriter : IDisposable {

                        [Dispose]
                        private StreamWriter _streamWriter;

                        public LogWriter(string path) => _streamWriter = new StreamWriter(path);

                        public void WriteLine(string text) => _streamWriter.WriteLine($"{DateTime.Now}\t{text}");
                    }
                }
                """,
        };

        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial struct LogWriter
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        private void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        private void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                _streamWriter?.Dispose();
                            }
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestAsyncDisposeStructField() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;

                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable]
                    public partial struct LogWriter : IAsyncDisposable {

                        [AsyncDispose]
                        private StreamWriter _streamWriter;

                        public LogWriter(string path) => _streamWriter = new StreamWriter(path);

                        public void WriteLine(string text) => _streamWriter.WriteLine($"{DateTime.Now}\t{text}");
                    }
                }
                """,
        };

        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial struct LogWriter
                    {
                        /// <summary>
                        /// Asynchronously releases all resources used by the current instance.
                        /// </summary>
                        /// <returns>
                        /// A task that represents the asynchronous dispose operation.
                        /// </returns>
                        public async global::System.Threading.Tasks.ValueTask DisposeAsync() {
                            await DisposeAsyncCore().ConfigureAwait(false);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        private void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Asynchronously releases the resources used by the current instance.
                        /// </summary>
                        /// <returns>
                        /// A task that represents the asynchronous dispose operation.
                        /// </returns>
                        private async global::System.Threading.Tasks.ValueTask DisposeAsyncCore() {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (_streamWriter != null) {
                                await _streamWriter.DisposeAsync().ConfigureAwait(false);
                            }
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestThreadSafeDisposeStruct() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;

                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable(IsThreadSafe = true)]
                    public partial struct LogWriter : IDisposable {

                        [Dispose]
                        private StreamWriter StreamWriter { get; }

                        public LogWriter(string path) => StreamWriter = new StreamWriter(path);
                    }
                }
                """,
        };

        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial struct LogWriter
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Detects redundant Dispose() calls in a thread-safe manner. _isDisposed == 0 means Dispose(bool) has not been called yet, and _isDisposed == 1 means Dispose(bool) has already been called. This field must not be modified manually.
                        /// </summary>
                        private int _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        private void ThrowIfDisposed() {
                            if (_isDisposed != 0) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        private void Dispose(bool disposing) {
                            if (global::System.Threading.Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) {
                                return;
                            }
                            if (disposing) {
                                StreamWriter?.Dispose();
                            }
                        }


                    }
                }

                """)));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestDisposeAttributeOnNonDisposableMember() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;

                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable]
                    public partial class LogWriter : IDisposable {

                        [Dispose]
                        private string Text { get; } = string.Empty;
                    }
                }
                """,
        };

        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Releases all resources used by the current instance.
                        /// </summary>
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected virtual void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                Text?.Dispose();
                            }
                        }


                    }
                }

                """)));

        context.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1061").WithSpan(@"ReflectionIT.DisposeGenerator\ReflectionIT.DisposeGenerator.SourceGenerator\X.LogWriter.g.cs", 47, 22, 47, 30).WithArguments("string", "Dispose"));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestAsyncDisposeAttributeOnNonAsyncDisposableMember() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;

                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable]
                    public partial class LogWriter : IAsyncDisposable {

                        [AsyncDispose]
                        private string Text { get; } = string.Empty;
                    }
                }
                """,
        };

        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Asynchronously releases all resources used by the current instance.
                        /// </summary>
                        /// <returns>
                        /// A task that represents the asynchronous dispose operation.
                        /// </returns>
                        public async global::System.Threading.Tasks.ValueTask DisposeAsync() {
                            await DisposeAsyncCore().ConfigureAwait(false);
                            global::System.GC.SuppressFinalize(this);
                        }

                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Asynchronously releases the resources used by the current instance.
                        /// </summary>
                        /// <returns>
                        /// A task that represents the asynchronous dispose operation.
                        /// </returns>
                        protected virtual async global::System.Threading.Tasks.ValueTask DisposeAsyncCore() {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (Text != null) {
                                await Text.DisposeAsync().ConfigureAwait(false);
                            }
                        }


                    }
                }

                """)));

        context.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1061").WithSpan(@"ReflectionIT.DisposeGenerator\ReflectionIT.DisposeGenerator.SourceGenerator\X.LogWriter.g.cs", 52, 28, 52, 40).WithArguments("string", "DisposeAsync"));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestOverrideDisposeWithoutDisposableBase() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;

                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable(OverrideDispose = true)]
                    public partial class LogWriter : IDisposable {

                        [Dispose]
                        private StreamWriter StreamWriter { get; } = new StreamWriter(Stream.Null);
                    }
                }
                """,
        };

        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Releases the unmanaged resources used by the current instance and optionally releases the managed resources.
                        /// </summary>
                        /// <param name="disposing">"true" to release managed resources; otherwise, "false".</param>
                        protected override void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                StreamWriter?.Dispose();
                            }
                            base.Dispose(disposing);
                        }

                    }
                }

                """)));

        context.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0535").WithSpan(34, 38, 34, 49).WithArguments("X.LogWriter", "System.IDisposable.Dispose()"));
        context.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0115").WithSpan(@"ReflectionIT.DisposeGenerator\ReflectionIT.DisposeGenerator.SourceGenerator\X.LogWriter.g.cs", 33, 33, 33, 40).WithArguments("X.LogWriter.Dispose(bool)"));
        context.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0117").WithSpan(@"ReflectionIT.DisposeGenerator\ReflectionIT.DisposeGenerator.SourceGenerator\X.LogWriter.g.cs", 41, 18, 41, 25).WithArguments("object", "Dispose"));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestOverrideDisposeAsyncCoreWithoutAsyncDisposableBase() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;

                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable(OverrideDisposeAsyncCore = true)]
                    public partial class LogWriter : IAsyncDisposable {

                        [AsyncDispose]
                        private StreamWriter StreamWriter { get; } = new StreamWriter(Stream.Null);
                    }
                }
                """,
        };

        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.LogWriter.g.cs",
            NormalizeGeneratedSource($$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        /// <summary>
                        /// Tracks whether the current instance has been disposed. This field must not be modified manually.
                        /// </summary>
                        private bool _isDisposed;

                        /// <summary>
                        /// Throws an exception if the current instance has been disposed.
                        /// </summary>
                        protected virtual void ThrowIfDisposed() {
                            if (_isDisposed) {
                                throw new global::System.ObjectDisposedException(nameof(LogWriter));
                            }
                        }

                        /// <summary>
                        /// Asynchronously releases the resources used by the current instance.
                        /// </summary>
                        /// <returns>
                        /// A task that represents the asynchronous dispose operation.
                        /// </returns>
                        protected override async global::System.Threading.Tasks.ValueTask DisposeAsyncCore() {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (StreamWriter != null) {
                                await StreamWriter.DisposeAsync().ConfigureAwait(false);
                            }
                            await base.DisposeAsyncCore().ConfigureAwait(false);
                        }

                    }
                }

                """)));

        context.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0535").WithSpan(34, 38, 34, 54).WithArguments("X.LogWriter", "System.IAsyncDisposable.DisposeAsync()"));
        context.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0115").WithSpan(@"ReflectionIT.DisposeGenerator\ReflectionIT.DisposeGenerator.SourceGenerator\X.LogWriter.g.cs", 35, 75, 35, 91).WithArguments("X.LogWriter.DisposeAsyncCore()"));
        context.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0117").WithSpan(@"ReflectionIT.DisposeGenerator\ReflectionIT.DisposeGenerator.SourceGenerator\X.LogWriter.g.cs", 43, 24, 43, 40).WithArguments("object", "DisposeAsyncCore"));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    [Fact]
    public async Task TestDisposableAttributeOnNonPartialClass() {
        var context = new CSharpSourceGeneratorTest<SourceGenerator, DefaultVerifier> {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            TestCode = $$"""
                using System;
                using System.IO;

                {{ATTRIBUTE_CODE_IN_TEST}}

                namespace X {

                    [Disposable]
                    public class LogWriter : IDisposable {

                        [Dispose]
                        private StreamWriter StreamWriter { get; } = new StreamWriter(Stream.Null);
                    }
                }
                """,
        };

        context.ExpectedDiagnostics.Add(new DiagnosticResult(SourceGenerator.TypeMustBePartial).WithSpan(34, 18, 34, 27).WithArguments("LogWriter"));
        context.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0535").WithSpan(34, 30, 34, 41).WithArguments("X.LogWriter", "System.IDisposable.Dispose()"));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
    }

    private static string NormalizeGeneratedSource(string source) {
        var newLine = Environment.NewLine;

        return source
            .Replace("}" + newLine + newLine + "        ///", "}" + newLine + "        " + newLine + "        ///")
            .Replace(";" + newLine + newLine + "        ///", ";" + newLine + "        " + newLine + "        ///")
            .Replace("Dispose();" + newLine + "        }" + newLine + "    }", "Dispose();" + newLine + "        }" + newLine + "        " + newLine + "    }")
            .Replace("}" + newLine + newLine + newLine + "    }", "}" + newLine + "        " + newLine + "    }")
            .Replace("}" + newLine + newLine + "    }", "}" + newLine + "        " + newLine + "    }");
    }

    public const string HEADER_CODE = """
        //------------------------------------------------------------------------------
        // <auto-generated>
        //     This code was generated by the ReflectionIT.DisposeGenerator source generator
        //     Changes to this file may cause incorrect behavior and will be lost if
        //     the code is regenerated.
        // </auto-generated>
        //------------------------------------------------------------------------------
        #pragma warning disable
        #nullable enable annotations

        """;


    public const string ATTRIBUTE_CODE = """
            namespace ReflectionIT.DisposeGenerator.Attributes {
            
                [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
                public class DisposableAttribute : Attribute {
                    public bool OverrideDispose { get; set; }
                    public bool OverrideDisposeAsyncCore { get; set; }
                    public bool GenerateThrowIfDisposed { get; set; } = true;
                    public bool ExplicitInterfaceImplementation { get; set; }
                    public bool IsThreadSafe { get; set; }
                    public bool HasUnmanagedResources { get; set; }
                }

                [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
                public class DisposeAttribute : Attribute {
                    public bool SetToNull { get; set;}    
                }
                
                [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
                public class AsyncDisposeAttribute : DisposeAttribute {
                    public bool ConfigureAwait { get; set; } = true;
                }
            }
            """;

    public const string ATTRIBUTE_CODE_IN_TEST = $$"""
            using System;
            using ReflectionIT.DisposeGenerator.Attributes;
            
            {{ATTRIBUTE_CODE}}
            
            """;

}
