using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace ReflectionIT.DisposeGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class SourceGenerator : IIncrementalGenerator {

    internal static readonly DiagnosticDescriptor TypeMustBePartial = new(
        id: "RITDG001",
        title: "Disposable type must be partial",
        messageFormat: "Type '{0}' is annotated with [Disposable] and must be declared partial for ReflectionIT.DisposeGenerator to generate code",
        category: "ReflectionIT.DisposeGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MemberMustSupportDispose = new(
        id: "RITDG002",
        title: "Member must support synchronous disposal",
        messageFormat: "Member '{0}' is annotated with [Dispose] and must implement System.IDisposable",
        category: "ReflectionIT.DisposeGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MemberMustSupportAsyncDispose = new(
        id: "RITDG003",
        title: "Member must support asynchronous disposal",
        messageFormat: "Member '{0}' is annotated with [AsyncDispose] and must implement System.IAsyncDisposable",
        category: "ReflectionIT.DisposeGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor SetToNullRequiresAssignableNullableMember = new(
        id: "RITDG004",
        title: "SetToNull requires an assignable nullable member",
        messageFormat: "Member '{0}' uses SetToNull but cannot be assigned null; use a writable nullable field or property",
        category: "ReflectionIT.DisposeGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor StaticMemberNotSupported = new(
        id: "RITDG005",
        title: "Static disposable members are not supported",
        messageFormat: "Member '{0}' is static and cannot participate in instance disposal",
        category: "ReflectionIT.DisposeGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor OverrideRequiresSuitableBaseMethod = new(
        id: "RITDG006",
        title: "Dispose override requires a suitable base method",
        messageFormat: "Type '{0}' sets {1} but no suitable overridable base method exists",
        category: "ReflectionIT.DisposeGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ContainingTypeMustBePartial = new(
        id: "RITDG007",
        title: "Containing type must be partial",
        messageFormat: "Containing type '{0}' must be declared partial so code can be generated for nested type '{1}'",
        category: "ReflectionIT.DisposeGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor UnsupportedDisposableType = new(
        id: "RITDG008",
        title: "Disposable type is not supported",
        messageFormat: "Type '{0}' cannot use the requested dispose generation: {1}",
        category: "ReflectionIT.DisposeGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor GeneratedMemberConflict = new(
        id: "RITDG009",
        title: "Generated member conflicts with an existing member",
        messageFormat: "Type '{0}' already declares member '{1}', which conflicts with generated dispose code",
        category: "ReflectionIT.DisposeGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context) {

        var disposableInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeMetadata.DisposableAttributeName,
                predicate: static (node, cancel) => node is TypeDeclarationSyntax,
                transform: static (context, cancel) =>
                    new DisposableInfo(
                        (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.TargetNode, cancel)!,
                        (TypeDeclarationSyntax)context.TargetNode!)
            );

        var disposeInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeMetadata.DisposeAttributeName,
                predicate: static (node, cancel) => node is VariableDeclaratorSyntax or PropertyDeclarationSyntax,
                transform: static (context, cancel) =>
                    new DisposeInfo(context.SemanticModel.GetDeclaredSymbol(context.TargetNode, cancel)!, AttributeMetadata.DisposeAttributeName)
            );

        var asyncDisposeInfos = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeMetadata.AsyncDisposeAttributeName,
            predicate: static (node, cancel) => node is VariableDeclaratorSyntax or PropertyDeclarationSyntax,
            transform: static (context, cancel) =>
                new AsyncDisposeInfo(context.SemanticModel.GetDeclaredSymbol(context.TargetNode, cancel)!)
        );

        var all = disposableInfos.Collect().Combine(disposeInfos.Collect().Combine(asyncDisposeInfos.Collect()));

        context.RegisterSourceOutput(all, GenerateSource);
    }

    private void GenerateSource(SourceProductionContext context, (ImmutableArray<DisposableInfo> Left, (ImmutableArray<DisposeInfo> Left, ImmutableArray<AsyncDisposeInfo> Right) Right) tuple) {

        if (context.CancellationToken.IsCancellationRequested) {
            return;
        }

        try {
            var types = tuple.Left;
            if (types.IsDefaultOrEmpty) {
                return;
            }

            foreach (var dtInfo in types) {

                if (!dtInfo.IsPartial) {
                    context.ReportDiagnostic(Diagnostic.Create(TypeMustBePartial, dtInfo.TypeDeclarationSyntax.Identifier.GetLocation(), dtInfo.TypeSymbol.Name));
                    continue;
                }

                if (!ValidateType(context, dtInfo)) {
                    continue;
                }

                // During live analysis, cached values from different incremental compilations
                // can contain equivalent source symbols that are not equal by symbol identity.
                // Match by a stable fully-qualified type key instead.
                DisposeInfo[] requestedDisposeInfos = tuple.Right.Left
                    .Where(d => d.ContainingTypeKey == dtInfo.TypeKey)
                    .ToArray();
                AsyncDisposeInfo[] requestedAsyncDisposeInfos = tuple.Right.Right
                    .Where(d => d.ContainingTypeKey == dtInfo.TypeKey)
                    .ToArray();

                Dictionary<string, DisposeInfo> disposeInfos = requestedDisposeInfos
                    .Where(d => ValidateMember(context, d, isAsync: false))
                    .ToDictionary(p => p.MemberName);
                Dictionary<string, AsyncDisposeInfo> asyncDisposeInfos = requestedAsyncDisposeInfos
                    .Where(d => ValidateMember(context, d, isAsync: true))
                    .ToDictionary(p => p.MemberName);

                bool generateDispose = requestedDisposeInfos.Length > 0 || dtInfo.HasUnmanagedResources;
                bool generateAsyncDispose = requestedAsyncDisposeInfos.Length > 0;

                if (!generateDispose && !generateAsyncDispose) {
                    continue;
                }

                ValidateOverrides(context, dtInfo, generateDispose, generateAsyncDispose);
                if (!ValidateGeneratedMemberConflicts(context, dtInfo, generateDispose, generateAsyncDispose)) {
                    continue;
                }

                CsFileBuilder builder = new CsFileBuilder();

                builder.AddAutoGeneratedHeader("ReflectionIT.DisposeGenerator")
                     .AddPreprocessorDirectives()
                     .AddEmptyLine();

                builder.AddNamespace(dtInfo.TypeSymbol.ContainingNamespace);

                builder.AddPartialType(dtInfo.TypeSymbol);
                builder.AddStatementAndStartBlock(string.Empty);

                if (!dtInfo.OverrideDispose && generateDispose) {

                    string am = dtInfo.ExplicitInterfaceImplementation ? "void global::System.IDisposable." : "public void ";

                    builder.AddXmlCommentLines(
                            "<summary>",
                            "Releases all resources used by the current instance.",
                            "</summary>")
                        .AddGeneratedCodeAttribute()
                        .AddStatements(
                            $$"""{{am}}Dispose() {""",
                            "    Dispose(disposing: true);",
                            "    global::System.GC.SuppressFinalize(this);",
                            "}")
                        .AddEmptyLine();
                }

                const string valueTaskText = "global::System.Threading.Tasks.ValueTask";

                if (!dtInfo.OverrideDisposeAsyncCore && generateAsyncDispose) {

                    string am = dtInfo.ExplicitInterfaceImplementation ? $"async {valueTaskText} global::System.IAsyncDisposable." : $"public async {valueTaskText} ";

                    builder.AddXmlCommentLines(
                            "<summary>",
                            "Asynchronously releases all resources used by the current instance.",
                            "</summary>",
                            "<returns>",
                            "A task that represents the asynchronous dispose operation.",
                            "</returns>")
                        .AddGeneratedCodeAttribute()
                        .AddStatements(
                            $$"""{{am}}DisposeAsync() {""",
                            "    await DisposeAsyncCore().ConfigureAwait(false);",
                            "    global::System.GC.SuppressFinalize(this);",
                            "}")
                        .AddEmptyLine();
                }

                (string isDisposedType, string isDisposedReturnCheck, string isDisposedCheck, string? setIsDisposed) = dtInfo.IsThreadSafe
                    ? ("int", "global::System.Threading.Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0", "_isDisposed != 0", null)
                    : ("bool", "_isDisposed", "_isDisposed", "_isDisposed = true;");

                string accessModifiers = dtInfo.IsSealed || dtInfo.IsValueType ? "private" : "protected virtual";
                string? syncBaseDispose = null;
                string isDisposedAccessModifiers = dtInfo.IsValueType ? "private" : HasDisposableBase(dtInfo.TypeSymbol) ? "protected override" : dtInfo.IsSealed ? "private" : "protected virtual";
                string isDisposedGetter = dtInfo.IsThreadSafe ? "_isDisposed != 0" : "_isDisposed";
                string? baseIsDisposed = isDisposedAccessModifiers == "protected override" ? " || base.IsDisposed" : null;

                if (dtInfo.OverrideDispose
                    && generateDispose
                    && accessModifiers.Contains("virtual")
                    && HasSuitableBaseDisposeMethod(dtInfo.TypeSymbol, isAsync: false)) {
                    accessModifiers = "protected override";
                    syncBaseDispose = "    base.Dispose(disposing);";
                }

                if (dtInfo.HasUnmanagedResources && generateDispose) {
                    builder.AddXmlCommentLines(
                            "<summary>",
                            "Releases unmanaged resources held by the current instance.",
                            "</summary>")
                        .AddGeneratedCodeAttribute()
                        .AddStatements(
                            $$"""~{{dtInfo.TypeSymbol.Name}}() {""",
                            "    Dispose(disposing: false);",
                            "}")
                        .AddEmptyLine()
                        .AddXmlCommentLines(
                            "<summary>",
                            "Releases unmanaged resources held by the current instance.",
                            "</summary>")
                        .AddGeneratedCodeAttribute()
                        .AddStatements(
                           $$"""{{accessModifiers}} partial void ReleaseUnmanagedResources();""")
                        .AddEmptyLine();
                }

                if (generateDispose || generateAsyncDispose) {
                    builder.AddXmlCommentLines(
                            "<summary>",
                            dtInfo.IsThreadSafe
                                ? "Detects redundant Dispose() calls in a thread-safe manner. _isDisposed == 0 means Dispose(bool) has not been called yet, and _isDisposed == 1 means Dispose(bool) has already been called. This field must not be modified manually."
                                : "Tracks whether the current instance has been disposed. This field must not be modified manually.",
                            "</summary>")
                        .AddGeneratedCodeAttribute(false)
                        .AddStatements($"private {isDisposedType} _isDisposed;")
                        .AddEmptyLine();

                    builder.AddXmlCommentLines(
                            "<summary>",
                            "Gets a value indicating whether the current instance has been disposed.",
                            "</summary>")
                        .AddGeneratedCodeAttribute()
                        .AddStatements(
                            $$"""{{isDisposedAccessModifiers}} bool IsDisposed => {{isDisposedGetter}}{{baseIsDisposed}};""")
                        .AddEmptyLine();

                    if (dtInfo.GenerateThrowIfDisposed) {
                        string throwIfDisposedAccessModifiers = dtInfo.IsValueType || dtInfo.IsSealed
                            ? "private"
                            : HasThrowIfDisposedBase(dtInfo.TypeSymbol) ? "protected override" : "protected virtual";

                        builder.AddXmlCommentLines(
                                "<summary>",
                                "Throws an exception if the current instance has been disposed.",
                                "</summary>")
                            .AddGeneratedCodeAttribute()
                            .AddStatements(
                                $$"""{{throwIfDisposedAccessModifiers}} void ThrowIfDisposed() {""",
                                "    if (IsDisposed) {",
                                $$"""        throw new global::System.ObjectDisposedException(nameof({{dtInfo.TypeSymbol.Name}}));""",
                                "    }")
                            .AddStatements("}")
                            .AddEmptyLine();
                    }
                }

                if (generateDispose) {

                    builder.AddXmlCommentLines(
                            "<summary>",
                            "Releases the unmanaged resources used by the current instance and optionally releases the managed resources.",
                            "</summary>",
                            "<param name=\"disposing\">\"true\" to release managed resources; otherwise, \"false\".</param>")
                        .AddGeneratedCodeAttribute()
                        .AddStatements(
                            $$"""{{accessModifiers}} void Dispose(bool disposing) {""",
                            $$"""    if ({{isDisposedReturnCheck}}) {""",
                             "        return;",
                             "    }",
                             $"""    {setIsDisposed}""",
                              """    if (disposing) {""");

                    foreach (var item in disposeInfos.Values) {
                        builder.AddStatements($"        this.{item.MemberName}?.Dispose();");
                    }

                    foreach (var item in asyncDisposeInfos.Values) {
                        if (!disposeInfos.ContainsKey(item.MemberName)) {
                            builder.AddStatements($"        if ({item.MemberName} is global::System.IDisposable local{item.MemberName}) local{item.MemberName}.Dispose();");
                        }
                    }
                    builder.AddStatements("    }");
                    builder.AddStatementsIf(dtInfo.HasUnmanagedResources, "    ReleaseUnmanagedResources();");

                    SetNull(disposeInfos, asyncDisposeInfos, builder);

                    builder.AddStatements(
                       syncBaseDispose,
                       "}");
                    builder.AddEmptyLine();
                }

                if (generateAsyncDispose) {

                    accessModifiers = dtInfo.IsSealed || dtInfo.IsValueType ? "private" : "protected virtual";
                    string? asyncBaseDispose = null;

                    if (dtInfo.OverrideDisposeAsyncCore
                        && accessModifiers.Contains("virtual")
                        && HasSuitableBaseDisposeMethod(dtInfo.TypeSymbol, isAsync: true)) {
                        accessModifiers = "protected override";
                        asyncBaseDispose = "    await base.DisposeAsyncCore().ConfigureAwait(false);";
                    } else if (dtInfo.OverrideDispose
                        && HasSuitableBaseDisposeMethod(dtInfo.TypeSymbol, isAsync: false)) {
                        asyncBaseDispose = "    base.Dispose(disposing: true);";
                    }

                    builder.AddXmlCommentLines(
                            "<summary>",
                            "Asynchronously releases the resources used by the current instance.",
                            "</summary>",
                            "<returns>",
                            "A task that represents the asynchronous dispose operation.",
                            "</returns>")
                        .AddGeneratedCodeAttribute()
                        .AddStatements(
                            $$"""{{accessModifiers}} async {{valueTaskText}} DisposeAsyncCore() {""",
                            $$"""    if ({{isDisposedReturnCheck}}) {""",
                             "        return;",
                             "    }",
                             $"""    {setIsDisposed}""");

                    foreach (var item in asyncDisposeInfos.Values) {
                        builder.AddStatements($$"""    if (this.{{item.MemberName}} != null) {""",
                                              $"        await this.{item.MemberName}.DisposeAsync().ConfigureAwait({item.ConfigureAwait.ToString().ToLower()});",
                                               "    }");
                    }

                    foreach (var item in disposeInfos.Values) {
                        if (!asyncDisposeInfos.ContainsKey(item.MemberName)) {
                            builder.AddStatements($"    this.{item.MemberName}?.Dispose();");
                        }
                    }

                    builder.AddStatementsIf(dtInfo.HasUnmanagedResources, "    ReleaseUnmanagedResources();");
                    SetNull(disposeInfos, asyncDisposeInfos, builder);

                    builder.AddStatements(
                       asyncBaseDispose,
                       "}");
                    builder.AddEmptyLine();
                }

                builder.EndPartialType(dtInfo.TypeSymbol);
                builder.EndNamespace();

                var src = builder.Build();

                var filename = dtInfo.TypeSymbol.ToDisplayString()
                                  .Replace('<', '{')
                                  .Replace('>', '}')
                                  .Replace(" ", string.Empty);

                context.AddSource($"{filename}.g.cs", SourceText.From(src, Encoding.UTF8));
            }
        } catch (Exception ex) {
            ReportException(context, ex);
        }

        static void SetNull(Dictionary<string, DisposeInfo> disposeInfos, Dictionary<string, AsyncDisposeInfo> asyncDisposeInfos, CsFileBuilder builder) {
            foreach (var item in disposeInfos.Values.Where(static p => p.SetToNull).Union(asyncDisposeInfos.Values.Where(static p => p.SetToNull))) {
                builder.AddStatements($"    this.{item.MemberName} = null;");
            }
        }
    }

    private static void ReportException(SourceProductionContext spc, Exception ex) {
        var descriptor = new DiagnosticDescriptor(
            id: "DISPGEN001",
            title: "DisposeGenerator crashed",
            messageFormat: "DisposeGenerator threw an exception: {0}",
            category: "SourceGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        spc.ReportDiagnostic(
            Diagnostic.Create(descriptor, Location.None, ex.ToString()));
    }

    private static bool HasDisposableBase(ITypeSymbol typeSymbol) {
        var baseType = typeSymbol.BaseType;
        while (baseType is not null) {
            if (HasOverridableProperty(baseType, "IsDisposed") || BaseGeneratesDisposedState(baseType)) {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static bool HasThrowIfDisposedBase(ITypeSymbol typeSymbol) {
        var baseType = typeSymbol.BaseType;
        while (baseType is not null) {
            var method = baseType.GetMembers("ThrowIfDisposed")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(static m =>
                    m.MethodKind == MethodKind.Ordinary
                    && m.Arity == 0
                    && m.Parameters.Length == 0
                    && m.ReturnsVoid
                    && !m.ReturnsByRef
                    && !m.ReturnsByRefReadonly);

            if (method is not null) {
                return IsOverridable(method);
            }

            if (BaseGeneratesDisposedState(baseType) && ReadDisposableOption(baseType, AttributeMetadata.GenerateThrowIfDisposedPropertyName, defaultValue: true)) {
                return true;
            }

            baseType = baseType.BaseType;
        }
        return false;
    }

    private static bool ValidateType(SourceProductionContext context, DisposableInfo info) {
        if (info.IsStatic) {
            ReportUnsupported("static classes are not supported");
            return false;
        }

        if (info.IsReadOnly) {
            ReportUnsupported("readonly structs cannot contain mutable disposal state");
            return false;
        }

        if (info.IsRefLikeType) {
            ReportUnsupported("ref structs are not supported");
            return false;
        }

        if (info.HasUnmanagedResources && info.IsValueType) {
            ReportUnsupported("unmanaged-resource finalization is only supported for classes");
            return false;
        }

        var containingType = info.TypeSymbol.ContainingType;
        while (containingType is not null) {
            TypeDeclarationSyntax? declaration = containingType.DeclaringSyntaxReferences
                .Select(static r => r.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault(static d => !d.Modifiers.Any(SyntaxKind.PartialKeyword));

            if (declaration is not null) {
                context.ReportDiagnostic(Diagnostic.Create(
                    ContainingTypeMustBePartial,
                    declaration.Identifier.GetLocation(),
                    containingType.Name,
                    info.TypeSymbol.Name));
                return false;
            }

            containingType = containingType.ContainingType;
        }

        return true;

        void ReportUnsupported(string reason) =>
            context.ReportDiagnostic(Diagnostic.Create(
                UnsupportedDisposableType,
                info.TypeDeclarationSyntax.Identifier.GetLocation(),
                info.TypeSymbol.Name,
                reason));
    }

    private static bool ValidateMember(SourceProductionContext context, DisposeInfo info, bool isAsync) {
        Location? location = info.Symbol.Locations.FirstOrDefault();

        if (info.IsStatic) {
            context.ReportDiagnostic(Diagnostic.Create(StaticMemberNotSupported, location, info.MemberName));
            return false;
        }

        bool supportsDisposal = isAsync
            ? ImplementsInterface(info.MemberType, "System.IAsyncDisposable")
            : ImplementsInterface(info.MemberType, "System.IDisposable");

        if (!supportsDisposal) {
            context.ReportDiagnostic(Diagnostic.Create(
                isAsync ? MemberMustSupportAsyncDispose : MemberMustSupportDispose,
                location,
                info.MemberName));
            return false;
        }

        if (info.SetToNull && !info.CanSetToNull) {
            context.ReportDiagnostic(Diagnostic.Create(
                SetToNullRequiresAssignableNullableMember,
                location,
                info.MemberName));
            return false;
        }

        return true;
    }

    private static void ValidateOverrides(
        SourceProductionContext context,
        DisposableInfo info,
        bool generateDispose,
        bool generateAsyncDispose) {

        if (generateDispose && info.OverrideDispose && !HasSuitableBaseDisposeMethod(info.TypeSymbol, isAsync: false)) {
            context.ReportDiagnostic(Diagnostic.Create(
                OverrideRequiresSuitableBaseMethod,
                info.TypeDeclarationSyntax.Identifier.GetLocation(),
                info.TypeSymbol.Name,
                AttributeMetadata.OverrideDisposePropertyName));
        }

        if (generateAsyncDispose && info.OverrideDisposeAsyncCore && !HasSuitableBaseDisposeMethod(info.TypeSymbol, isAsync: true)) {
            context.ReportDiagnostic(Diagnostic.Create(
                OverrideRequiresSuitableBaseMethod,
                info.TypeDeclarationSyntax.Identifier.GetLocation(),
                info.TypeSymbol.Name,
                AttributeMetadata.OverrideDisposeAsyncCorePropertyName));
        }
    }

    private static bool ValidateGeneratedMemberConflicts(
        SourceProductionContext context,
        DisposableInfo info,
        bool generateDispose,
        bool generateAsyncDispose) {

        List<string> conflicts = [];
        AddMemberConflict("_isDisposed");
        AddMemberConflict("IsDisposed");

        if (info.GenerateThrowIfDisposed) {
            AddMethodConflict("ThrowIfDisposed", 0);
        }

        if (generateDispose) {
            if (!info.OverrideDispose && !info.ExplicitInterfaceImplementation) {
                AddMethodConflict("Dispose", 0);
            }
            AddMethodConflict("Dispose", 1, SpecialType.System_Boolean);
        }

        if (generateAsyncDispose) {
            if (!info.OverrideDisposeAsyncCore && !info.ExplicitInterfaceImplementation) {
                AddMethodConflict("DisposeAsync", 0);
            }
            AddMethodConflict("DisposeAsyncCore", 0);
        }

        if (info.HasUnmanagedResources && info.TypeSymbol.GetMembers().OfType<IMethodSymbol>().Any(static m => m.MethodKind == MethodKind.Destructor)) {
            conflicts.Add("finalizer");
        }

        foreach (string conflict in conflicts.Distinct(StringComparer.Ordinal)) {
            context.ReportDiagnostic(Diagnostic.Create(
                GeneratedMemberConflict,
                info.TypeDeclarationSyntax.Identifier.GetLocation(),
                info.TypeSymbol.Name,
                conflict));
        }

        return conflicts.Count == 0;

        void AddMemberConflict(string name) {
            if (info.TypeSymbol.GetMembers(name).Length > 0) {
                conflicts.Add(name);
            }
        }

        void AddMethodConflict(string name, int parameterCount, SpecialType parameterType = SpecialType.None) {
            bool exists = info.TypeSymbol.GetMembers(name)
                .OfType<IMethodSymbol>()
                .Any(m => m.Parameters.Length == parameterCount
                    && (parameterType == SpecialType.None || m.Parameters[0].Type.SpecialType == parameterType));
            if (exists) {
                conflicts.Add(parameterCount == 0 ? $"{name}()" : $"{name}(bool)");
            }
        }
    }

    private static bool HasSuitableBaseDisposeMethod(ITypeSymbol typeSymbol, bool isAsync) {
        string methodName = isAsync ? "DisposeAsyncCore" : "Dispose";
        var baseType = typeSymbol.BaseType;

        while (baseType is not null) {
            IMethodSymbol? method = baseType.GetMembers(methodName)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m =>
                    m.MethodKind == MethodKind.Ordinary
                    && m.Arity == 0
                    && !m.ReturnsByRef
                    && !m.ReturnsByRefReadonly
                    && (isAsync
                        ? m.Parameters.Length == 0 && m.ReturnType.ToDisplayString() == "System.Threading.Tasks.ValueTask"
                        : m.Parameters.Length == 1
                            && m.Parameters[0].RefKind == RefKind.None
                            && m.Parameters[0].Type.SpecialType == SpecialType.System_Boolean
                            && m.ReturnsVoid));

            if (method is not null) {
                return IsOverridable(method);
            }

            if (isAsync ? BaseGeneratesAsyncDispose(baseType) : BaseGeneratesDispose(baseType)) {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool BaseGeneratesDisposedState(ITypeSymbol typeSymbol) =>
        BaseGeneratesDispose(typeSymbol) || BaseGeneratesAsyncDispose(typeSymbol);

    private static bool BaseGeneratesDispose(ITypeSymbol typeSymbol) =>
        HasDisposableAttribute(typeSymbol)
        && (ReadDisposableOption(typeSymbol, AttributeMetadata.HasUnmanagedResourcesPropertyName)
            || typeSymbol.GetMembers().SelectMany(static m => m.GetAttributes())
                .Any(static a => a.AttributeClass?.ToDisplayString() == AttributeMetadata.DisposeAttributeName));

    private static bool BaseGeneratesAsyncDispose(ITypeSymbol typeSymbol) =>
        HasDisposableAttribute(typeSymbol)
        && typeSymbol.GetMembers().SelectMany(static m => m.GetAttributes())
            .Any(static a => a.AttributeClass?.ToDisplayString() == AttributeMetadata.AsyncDisposeAttributeName);

    private static bool HasDisposableAttribute(ITypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().Any(static a => a.AttributeClass?.ToDisplayString() == AttributeMetadata.DisposableAttributeName);

    private static bool ReadDisposableOption(ITypeSymbol typeSymbol, string propertyName, bool defaultValue = false) {
        AttributeData? attribute = typeSymbol.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == AttributeMetadata.DisposableAttributeName);
        if (attribute is null) {
            return defaultValue;
        }

        KeyValuePair<string, TypedConstant> argument = attribute.NamedArguments.FirstOrDefault(n => n.Key == propertyName);
        return argument.Key is null ? defaultValue : argument.Value.Value is true;
    }

    private static bool HasOverridableProperty(ITypeSymbol typeSymbol, string name) =>
        typeSymbol.GetMembers(name).OfType<IPropertySymbol>().Any(static p =>
            p.Parameters.Length == 0
            && p.Type.SpecialType == SpecialType.System_Boolean
            && p.GetMethod is not null
            && !p.ReturnsByRef
            && !p.ReturnsByRefReadonly
            && IsOverridable(p));

    private static bool IsOverridable(ISymbol symbol) =>
        (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride)
        && !symbol.IsSealed
        && symbol.DeclaredAccessibility == Accessibility.Protected;

    private static bool ImplementsInterface(ITypeSymbol typeSymbol, string metadataName) =>
        typeSymbol.ToDisplayString() == metadataName
        || typeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == metadataName);

}
