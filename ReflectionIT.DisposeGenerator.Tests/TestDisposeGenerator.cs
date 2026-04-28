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
            $$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }
                        private bool _isDisposed;
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

                """));

        // List of expected generated sources
        context.TestState.GeneratedSources.Add((typeof(SourceGenerator), "X.SecondLogWriter.g.cs",
            $$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class SecondLogWriter
                    {
                        private bool _isDisposed;
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

                """));

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
            $$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }
                        private bool _isDisposed;
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

                """));

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
            $$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }
                        private bool _isDisposed;
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

                """));

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
            $$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }
                        public async global::System.Threading.Tasks.ValueTask DisposeAsync() {
                            await DisposeAsyncCore().ConfigureAwait(false);
                            global::System.GC.SuppressFinalize(this);
                        }
                        private bool _isDisposed;
                        protected virtual void Dispose(bool disposing) {
                            if (_isDisposed) {
                                return;
                            }
                            _isDisposed = true;
                            if (disposing) {
                                _streamWriter?.Dispose();
                            }
                        }
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

                """));

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
            $$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        void global::System.IDisposable.Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }
                        private bool _isDisposed;
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

                """));

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
            $$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriterWithAnExtraIntPtr
                    {
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }
                        ~LogWriterWithAnExtraIntPtr() {
                            Dispose(disposing: false);
                        }
                        protected virtual partial void ReleaseUnmanagedResources();
                        private bool _isDisposed;
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

                """));

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
            $$"""
                {{HEADER_CODE}}
                namespace X
                {
                    partial class LogWriter
                    {
                        public void Dispose() {
                            Dispose(disposing: true);
                            global::System.GC.SuppressFinalize(this);
                        }
                        private int _isDisposed;
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

                """));

        context.SolutionTransforms.Add((solution, projectId) => {
            var project = solution.GetProject(projectId)!;
            var parse = (CSharpParseOptions)project.ParseOptions!;
            return solution.WithProjectParseOptions(projectId, parse.WithLanguageVersion(LanguageVersion.CSharp14));
        });

        await context.RunAsync();
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
