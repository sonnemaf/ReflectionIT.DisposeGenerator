using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ReflectionIT.DisposeGenerator.Tests;

public class GeneratorValidationTests {

    [Fact]
    public void GeneratesDisposePatternForUnmanagedResourcesOnly() {
        const string source = """
            [Disposable(HasUnmanagedResources = true)]
            public partial class NativeOwner : IDisposable {
                protected virtual partial void ReleaseUnmanagedResources() {
                }
            }
            """;

        (Compilation output, GeneratorDriverRunResult result) = RunGenerator(source);

        AssertNoErrors(output);
        string generated = Assert.Single(result.GeneratedTrees).ToString();
        Assert.Contains("public void Dispose()", generated);
        Assert.Contains("~NativeOwner()", generated);
        Assert.Contains("protected virtual partial void ReleaseUnmanagedResources();", generated);
    }

    [Fact]
    public void DoesNotLeakSyncBaseCallIntoAsyncDisposeCore() {
        const string source = """
            [Disposable]
            public partial class BaseOwner : IDisposable {
                [Dispose]
                private readonly System.IO.MemoryStream _baseStream = new();
            }

            [Disposable(OverrideDispose = true)]
            public partial class DerivedOwner : BaseOwner, IAsyncDisposable {
                [Dispose]
                private readonly System.IO.MemoryStream _stream = new();

                [AsyncDispose]
                private readonly System.IO.MemoryStream _asyncStream = new();
            }
            """;

        (Compilation output, GeneratorDriverRunResult result) = RunGenerator(source);

        AssertNoErrors(output);
        string generated = result.GeneratedTrees
            .Single(t => t.FilePath.EndsWith("DerivedOwner.g.cs", StringComparison.Ordinal))
            .ToString();
        Assert.Contains("base.Dispose(disposing);", generated);
        int asyncCoreStart = generated.IndexOf("ValueTask DisposeAsyncCore()", StringComparison.Ordinal);
        Assert.True(generated.IndexOf("base.Dispose(disposing: true);", asyncCoreStart, StringComparison.Ordinal) > asyncCoreStart);
        Assert.Equal(-1, generated.IndexOf("base.Dispose(disposing);", asyncCoreStart, StringComparison.Ordinal));
    }

    [Fact]
    public void DoesNotOverrideIncompatibleThrowIfDisposedMethod() {
        const string source = """
            public class BaseOwner {
                public virtual void ThrowIfDisposed() {
                }
            }

            [Disposable]
            public partial class DerivedOwner : BaseOwner {
                [Dispose]
                private readonly System.IO.MemoryStream _stream = new();
            }
            """;

        (Compilation output, GeneratorDriverRunResult result) = RunGenerator(source);

        AssertNoErrors(output);
        string generated = Assert.Single(result.GeneratedTrees).ToString();
        Assert.Contains("protected virtual void ThrowIfDisposed()", generated);
        Assert.DoesNotContain("protected override void ThrowIfDisposed()", generated);
    }

    [Theory]
    [MemberData(nameof(InvalidUsageCases))]
    public void ReportsInvalidUsageAtUserCode(string source, string diagnosticId) {
        (_, GeneratorDriverRunResult result) = RunGenerator(source);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Id == diagnosticId);
        Assert.True(diagnostic.Location.IsInSource);
    }

    public static TheoryData<string, string> InvalidUsageCases => new() {
        {
            """
            [Disposable]
            public partial class Owner {
                [Dispose(SetToNull = true)]
                private readonly System.IO.MemoryStream _stream = new();
            }
            """,
            "RITDG004"
        },
        {
            """
            [Disposable]
            public partial class Owner {
                [Dispose]
                private static System.IO.MemoryStream Stream { get; } = new();
            }
            """,
            "RITDG005"
        },
        {
            """
            public class Outer {
                [Disposable]
                public partial class Owner {
                    [Dispose]
                    private readonly System.IO.MemoryStream _stream = new();
                }
            }
            """,
            "RITDG007"
        },
        {
            """
            [Disposable(HasUnmanagedResources = true)]
            public partial struct Owner {
            }
            """,
            "RITDG008"
        },
        {
            """
            [Disposable]
            public partial class Owner {
                private bool _isDisposed;

                [Dispose]
                private readonly System.IO.MemoryStream _stream = new();
            }
            """,
            "RITDG009"
        },
        {
            """
            public class BaseOwner {
                protected virtual void Dispose(ref bool disposing) {
                }
            }

            [Disposable(OverrideDispose = true)]
            public partial class Owner : BaseOwner {
                [Dispose]
                private readonly System.IO.MemoryStream _stream = new();
            }
            """,
            "RITDG006"
        },
    };

    private static (Compilation Output, GeneratorDriverRunResult Result) RunGenerator(string source) {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            TestDisposeGenerator.ATTRIBUTE_CODE_IN_TEST + Environment.NewLine + source,
            new CSharpParseOptions(LanguageVersion.CSharp13));

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorValidation",
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SourceGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out _);
        return (output, driver.GetRunResult());
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences() {
        string trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path));
    }

    private static void AssertNoErrors(Compilation compilation) {
        Diagnostic[] errors = compilation.GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.Empty(errors);
    }
}
