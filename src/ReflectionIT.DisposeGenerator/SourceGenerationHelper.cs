namespace ReflectionIT.DisposeGenerator;

public static class SourceGenerationHelper {

    internal const string GeneratorName = "ReflectionIT.DisposeGenerator";

    internal static string ImplementDisposablePattern(DisposableToGenerate classToGenerate) {
        ICsFileBuilder csFileBuilder = new CsFileBuilder();

        csFileBuilder.AddAutoGeneratedHeader(GeneratorName)
                     .AddEmptyLine();

        if (classToGenerate.ImplementIAsyncDisposable) {
            csFileBuilder.AddStatements("#nullable enable")
                         .AddEmptyLine()
                         .AddUsing("ValueTaskAlias = global::System.Threading.Tasks.ValueTask")
                         .AddEmptyLine();
        }

        csFileBuilder.AddNamespace(classToGenerate.Namespace, true);

        GenerateBaseImplementation(csFileBuilder, classToGenerate);

        if (classToGenerate.ImplementDisposable) {
            GenerateDisposableImplementation(csFileBuilder, "IDisposable", false, classToGenerate);
        }
        
        if (classToGenerate.ImplementIAsyncDisposable) {
            GenerateDisposableImplementation(csFileBuilder, "IAsyncDisposable", true, classToGenerate);
        }

        csFileBuilder.EndNamespace();

        return csFileBuilder.Build();
    }

    private static void GenerateBaseImplementation(ICsFileBuilder builder, DisposableToGenerate classToGenerate) {
        builder.AddStatementAndStartBlock($"partial class {classToGenerate.Name}")
               .AddGeneratedAttributes(GeneratorName, includeNonUserCodeAttributes: false)
               .AddStatements("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]",
                                    "private bool _isDisposed = false;")
               .AddEmptyLine()
               .AddProtectedDisposed(classToGenerate.IsSealed)
               .AddEmptyLine()
               .AddFinalizer(classToGenerate.HasUnmangedResources, classToGenerate.Name, !classToGenerate.ImplementDisposable && classToGenerate.ImplementIAsyncDisposable)
               .EndBlock()
               .AddEmptyLine();
    }

    private static void GenerateDisposableImplementation(ICsFileBuilder builder, string interfaceName, bool isAsync, DisposableToGenerate classToGenerate) {
        builder.AddStatementAndStartBlock($"partial class {classToGenerate.Name} : global::System.{interfaceName}")
               //.AddCallbackFunctions(isAsync, classToGenerate)
               .AddDisposeBoolean(isAsync, classToGenerate)
               .AddEmptyLine()
               .AddDispose(isAsync)
               .EndBlock()
               .AddEmptyLine();
    }

    private static ICsFileBuilder AddProtectedDisposed(this ICsFileBuilder builder, bool isSealed) {
        string accessModifier = isSealed ? "private" : "protected";
        builder.AddGeneratedAttributes(GeneratorName)
               .AddStatements($"{accessModifier} bool IsDisposed => _isDisposed;");
        return builder;
    }

    private static ICsFileBuilder AddDispose(this ICsFileBuilder builder, bool isAsync) {
        if (isAsync == false) {
            builder.AddGeneratedAttributes(GeneratorName)
                   .AddStatementAndStartBlock("public void Dispose()")
                   .AddStatements("Dispose(true);")
                   .AddStatements("global::System.GC.SuppressFinalize(this);")
                   .EndBlock();
        }

        return builder;
    }

    private static ICsFileBuilder AddFinalizer(this ICsFileBuilder builder, bool hasUnmanagedResources, string className, bool isAsync) {
        if (hasUnmanagedResources && !isAsync) {

            builder.AddGeneratedAttributes(GeneratorName)
                   .AddStatementAndStartBlock($"~{className}()")
                   .AddStatements("Dispose(false);")
                   .EndBlock();
        }

        return builder;
    }

    private static ICsFileBuilder AddDisposeBoolean(this ICsFileBuilder builder,
        bool isAsync,
        DisposableToGenerate disposableToGenerate) {
        if (isAsync) {
            builder.AddDisposeAsyncBooleanLogic(disposableToGenerate);
        } else {
            builder.AddDisposeBooleanLogic(disposableToGenerate);
        }

        return builder;
    }

    private static void AddDisposeBooleanLogic(this ICsFileBuilder builder, DisposableToGenerate disposableToGenerate) {
        string methodPrefix = disposableToGenerate.IsSealed ? "private" : "protected virtual";
        string[] variablesName = disposableToGenerate.FieldsOrProperties
            .Select(x => GetDisposeStatement(x, false, false)).ToArray();

        builder.AddGeneratedAttributes(GeneratorName)
               .AddStatementAndStartBlock($"{methodPrefix} void Dispose(bool disposing)")
               .AddIfBlock("IsDisposed", "return;")
               .AddStatements("", "OnDisposing(disposing);")
               .AddEmptyLine()
               .AddStatementAndStartBlock("if (disposing)")
               .AddStatements(variablesName)
               .EndBlock()
               .AddStatements("", "OnDisposed(disposing);")
               .AddStatements("", "_isDisposed = true;")
               .EndBlock();

        builder.AddStatements($"partial void OnDisposing(bool disposing);");
        builder.AddStatements($"partial void OnDisposed(bool disposing);");

    }

    private static void AddDisposeAsyncBooleanLogic(this ICsFileBuilder builder, DisposableToGenerate disposableToGenerate) {
        string methodPrefix = disposableToGenerate.IsSealed ? "private" : "protected virtual";

        string[] variablesName = disposableToGenerate.FieldsOrProperties
            .Select(x => GetDisposeStatement(x, true, disposableToGenerate.ConfigureAwait)).ToArray();

        builder.AddGeneratedAttributes(GeneratorName)
               .AddStatementAndStartBlock($"public async ValueTaskAlias DisposeAsync()")
               .AddStatements("", $"await DisposeAsyncCore().ConfigureAwait({disposableToGenerate.ConfigureAwait.ToString().ToLower()}); // Perform async cleanup.")
               .AddStatements("", "Dispose(false); // Dispose of unmanaged resources.")
               .AddStatements("", "global::System.GC.SuppressFinalize(this);")
               .EndBlock()
               .AddEmptyLine();

        builder.AddGeneratedAttributes(GeneratorName)
              .AddStatementAndStartBlock($"{methodPrefix} async ValueTaskAlias DisposeAsyncCore()")
              //.AddStatementAndStartBlock("if (!IsDisposed)")
              //.AddEmptyLine()
              .AddStatementsIf(disposableToGenerate.GenerateOnDisposingAsync, "", $"await OnDisposingAsyncCore().ConfigureAwait({disposableToGenerate.ConfigureAwait.ToString().ToLower()});")
              .AddStatements(variablesName)
              .AddStatementsIf(disposableToGenerate.GenerateOnDisposedAsync, "", $"await OnDisposedAsyncCore().ConfigureAwait({disposableToGenerate.ConfigureAwait.ToString().ToLower()});")
              //.AddStatements("", "_isDisposed = true;")
              //.EndBlock()
              .EndBlock();

        if (disposableToGenerate.GenerateOnDisposingAsync) {
            builder.AddStatements($"{methodPrefix} partial ValueTaskAlias OnDisposingAsyncCore();");
        }

        if (disposableToGenerate.GenerateOnDisposedAsync) {
            builder.AddStatements($"{methodPrefix} partial ValueTaskAlias OnDisposedAsyncCore();");
        }


    }

    public static string GetDisposeStatement(FieldOrPropertyToDispose fieldOrProperty, bool isAsync, bool configureAwait) {
        string needAwait = isAsync ? "await" : string.Empty;
        string needAsync = isAsync ? $"Async().ConfigureAwait({configureAwait.ToString().ToLower()})" : "()";
        string disposeType = isAsync ? "Async" : string.Empty;
        string localVar = $"var{fieldOrProperty.Name}";

        string disposeStatement = string.Empty;
        
        disposeStatement = fieldOrProperty.Type.IsValueType
            ? $"{needAwait} {fieldOrProperty.Name}.Dispose{needAsync};"
            : $"""
                if ({fieldOrProperty.Name} is global::System.I{disposeType}Disposable @{localVar}) {needAwait} @{localVar}.Dispose{needAsync}; 
                """;

        string setToNullStatement = fieldOrProperty.SetToNull ? $"\r\n{fieldOrProperty.Name} = null;" : "";

        return $"{disposeStatement}{setToNullStatement}";
    }

#pragma warning disable IDE1006 // Naming Styles
    private const string Header = """
        //------------------------------------------------------------------------------
        // <auto-generated>
        //     This code was generated by the ReflectionIT.DisposeGenerator source generator
        //
        //     Changes to this file may cause incorrect behavior and will be lost if
        //     the code is regenerated.
        // </auto-generated>
        //------------------------------------------------------------------------------

        #nullable enable
        """;
#pragma warning restore IDE1006 // Naming Styles

    public const string Attribute = Header + """

        #if DISPOSER_GENERATORS_EMBED_ATTRIBUTES
        
        namespace ReflectionIT.DisposeGenerator.Attributes {

            /// <summary>
            /// An attribute that indicates that for this type code is generated for the <see href="https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern">Dispose pattern</see>
            /// <para>
            /// For more info about the Dispose pattern please read:
            /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose">Implement a Dispose method</see>
            /// and
            /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync">Implement a DisposeAsync method</see>
            /// </para>
            /// </summary>
            [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
            public class DisposableAttribute : Attribute
            {
                /// <summary>
                /// Generates a Destructor which calls Dispose(false) if set to True
                /// </summary>
                public bool HasUnmangedResources { get; set; }

                /// <summary>
                /// Implements IAsyncDisposable and the DisposeAsync() & DisposeCoreAsync() methods
                /// </summary>
                public bool GenerateDisposeAsync { get; set; }

                /// <summary>
                /// Generate OnDisposingAsync() partial method which is called at the start of DisposeAsync().
                /// </summary>
                public bool GenerateOnDisposingAsync { get; set; }

                /// <summary>
                /// Generate GenerateOnDisposedAsync partial method which is called at the end of DisposeAsync().
                /// </summary>
                public bool GenerateOnDisposedAsync { get; set; }

                /// <summary>
                /// Use ConfigureAwait(false or true) on async method calls
                /// </summary>
                public bool ConfigureAwait { get; set; }
            }

            [global::System.AttributeUsage(global::System.AttributeTargets.Field | global::System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            public class DisposeAttribute : global::System.Attribute
            {
                /// <summary>
                /// Set the large object to null in the Dispose() method
                /// </summary>
                public bool SetToNull { get; set; }
            }
        }
        #endif
        """;

}