using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using ReflectionIT.DisposeGenerator.Attributes;

namespace ReflectionIT.DisposeGenerator;

[Generator]
public class DisposableGenerator : IIncrementalGenerator {

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "DisposeGenerator.Attributes.g.cs", SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));

        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is ClassDeclarationSyntax m && m.AttributeLists.Count > 0;

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context) {
        // we know the node is a ClassDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        ClassDeclarationSyntax classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // loop through all the attributes on the method
        foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists) {
            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes) {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol) {
                    // weird, we couldn't get the symbol, ignore it
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                // Is the attribute the [Disposable] attribute?
                if (fullName == typeof(DisposableAttribute).FullName) {
                    // return the class
                    return classDeclarationSyntax;
                }
            }
        }

        // we didn't find the attribute we were looking for
        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context) {
        if (classes.IsDefaultOrEmpty) {
            // nothing to do yet
            return;
        }

        IEnumerable<ClassDeclarationSyntax> distinctClasses = classes.Distinct();

        List<DisposableToGenerate> classesToGenerate = GetTypesToGenerate(compilation, distinctClasses, context.CancellationToken);
        if (classesToGenerate.Count == 0) {
            return;
        }

        foreach (DisposableToGenerate classToGenerate in classesToGenerate) {

            foreach (FieldOrPropertyToDispose fieldOrProperty in classToGenerate.FieldsOrProperties) {
                if (!fieldOrProperty.ImplementIAsyncDisposable && !fieldOrProperty.ImplementDisposable) {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "DISPOSER-0001",
                            "Non-void method return type",
                            $"The type '{0}' of {1} '{2}' does not implement System.IDisposable nor System.IAsyncDisposable. {typeof(DisposableAttribute).FullName} must be attached only to fields or properties that implements System.IDisposable or System.IAsyncDisposable",
                    "Disposer", DiagnosticSeverity.Error,
                    true),
                        fieldOrProperty.Location,
                        fieldOrProperty.Type.Name,
                        fieldOrProperty.IsProperty ? "property" : "field",
                        fieldOrProperty.Name));

                    return;
                }
            }
            
            string result = SourceGenerationHelper.ImplementDisposablePattern(classToGenerate);

            context.AddSource(classToGenerate.Name + "Disposable.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }

    private static DisposeAttribute? GetDisposeAttributeOrDefault(AttributeData? attributeData) {
        if (attributeData is null) {
            return null;
        }

        bool? setToNull = attributeData.NamedArguments.FirstOrDefault(a => a.Key == nameof(DisposeAttribute.SetToNull)).Value.Value as bool?;

        DisposeAttribute attribute = new() { SetToNull = setToNull.GetValueOrDefault() };

        return attribute;
    }

    private static DisposableAttribute GetDisposableAttributeOrDefault(AttributeData? attributeData) {
        if (attributeData is null) {
            return new();
        }

        bool? configureAwait = attributeData.NamedArguments.FirstOrDefault(a => a.Key == nameof(DisposableAttribute.ConfigureAwait)).Value.Value as bool?;
        bool? generateDisposeAsync = attributeData.NamedArguments.FirstOrDefault(a => a.Key == nameof(DisposableAttribute.GenerateDisposeAsync)).Value.Value as bool?;
        bool? generateOnDisposingAsync = attributeData.NamedArguments.FirstOrDefault(a => a.Key == nameof(DisposableAttribute.GenerateOnDisposingAsync)).Value.Value as bool?;
        bool? generateOnDisposedAsync = attributeData.NamedArguments.FirstOrDefault(a => a.Key == nameof(DisposableAttribute.GenerateOnDisposedAsync)).Value.Value as bool?;
        bool? hasUnmanagedResources = attributeData.NamedArguments.FirstOrDefault(a => a.Key == nameof(DisposableAttribute.HasUnmangedResources)).Value.Value as bool?;

        DisposableAttribute attribute = new() { 
            ConfigureAwait = configureAwait.GetValueOrDefault(),
            GenerateDisposeAsync = generateDisposeAsync.GetValueOrDefault(),
            GenerateOnDisposingAsync = generateOnDisposingAsync.GetValueOrDefault(),
            GenerateOnDisposedAsync = generateOnDisposedAsync.GetValueOrDefault(),
            HasUnmangedResources = hasUnmanagedResources.GetValueOrDefault(),
        };

        return attribute;
    }

    static FieldOrPropertyToDispose? GetField(IFieldSymbol field, INamedTypeSymbol? namedTypeSymbol) {
        bool fieldTypeImplementDisposable = field.Type.DoesImplementIDisposable();
        bool fieldTypeImplementAsyncDisposable = field.Type.DoesImplementIAsyncDisposable();

        if (!fieldTypeImplementAsyncDisposable && !fieldTypeImplementDisposable) {
            return null;
        }

        AttributeData? attributeData = field.GetAttributes()
            .FirstOrDefault(a => namedTypeSymbol?.Equals(a.AttributeClass, SymbolEqualityComparer.Default) ?? false);

        DisposeAttribute? disposeAttribute = GetDisposeAttributeOrDefault(attributeData);

        return field.Name.Contains('<') ? null :
            new FieldOrPropertyToDispose(field.Name, false,
                field.Locations.FirstOrDefault(),
                field.Type,
                fieldTypeImplementDisposable,
                fieldTypeImplementAsyncDisposable,
                disposeAttribute?.SetToNull ?? false);
    }

    static FieldOrPropertyToDispose? GetProperty(IPropertySymbol property, INamedTypeSymbol? namedTypeSymbol) {
        bool fieldTypeImplementDisposable = property.Type.DoesImplementIDisposable();
        bool fieldTypeImplementAsyncDisposable = property.Type.DoesImplementIAsyncDisposable();

        if (!fieldTypeImplementAsyncDisposable && !fieldTypeImplementDisposable) {
            return null;
        }

        AttributeData? attributeData = property.GetAttributes()
            .FirstOrDefault(a => namedTypeSymbol?.Equals(a.AttributeClass, SymbolEqualityComparer.Default) ?? false);

        DisposeAttribute? disposeAttribute = GetDisposeAttributeOrDefault(attributeData);

        return new FieldOrPropertyToDispose(property.Name, false,
                property.Locations.FirstOrDefault(),
                property.Type,
                fieldTypeImplementDisposable,
                fieldTypeImplementAsyncDisposable,
                disposeAttribute?.SetToNull ?? false);
    }

    private static List<DisposableToGenerate> GetTypesToGenerate(
        Compilation compilation,
        IEnumerable<ClassDeclarationSyntax> classes,
        CancellationToken ct) {
        List<DisposableToGenerate> classesToGenerate = [];
        INamedTypeSymbol? disposableAttribute = compilation.GetTypeByMetadataName(typeof(DisposableAttribute).FullName);
        INamedTypeSymbol? disposeAttribute = compilation.GetTypeByMetadataName(typeof(DisposeAttribute).FullName);

        if (disposableAttribute is null) {
            // nothing to do if this type isn't available
            return classesToGenerate;
        }

        foreach (ClassDeclarationSyntax classDeclarationSyntax in classes) {
            // stop if we're asked to
            ct.ThrowIfCancellationRequested();

            SemanticModel semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol) {
                // report diagnostic, something went wrong
                continue;
            }

            string name = classSymbol.Name;
            string nameSpace = classSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : classSymbol.ContainingNamespace.ToString();
            bool isSealed = classSymbol.IsSealed;

            AttributeData? disposable = classSymbol.GetAttributes().FirstOrDefault(a => disposableAttribute?.Equals(a.AttributeClass, SymbolEqualityComparer.Default) ?? false);

            bool hasDisposable = disposable is not null;

            if (!hasDisposable) {
                continue;
            }

            DisposableAttribute disposableValues = GetDisposableAttributeOrDefault(disposable);

            List<FieldOrPropertyToDispose> list = [];

            var fields = classSymbol.GetMembers().OfType<IFieldSymbol>();

            foreach (IFieldSymbol field in fields) {
                FieldOrPropertyToDispose? toDispose = GetField(field, disposeAttribute);
                if (toDispose is not null) {
                    list.Add(toDispose.Value);
                }
            }

            var properties = classSymbol
                .GetMembers()
                .OfType<IPropertySymbol>();

            foreach (IPropertySymbol property in properties) {
                FieldOrPropertyToDispose? toDispose = GetProperty(property, disposeAttribute);
                if (toDispose is not null) {
                    list.Add(toDispose.Value);
                }
            }

            classesToGenerate.Add(new DisposableToGenerate(
                     name: name,
                     ns: nameSpace,
                     hasUnmangedResources: disposableValues.HasUnmangedResources,
                     isSealed,
                     hasDisposable,
                     disposableValues.GenerateDisposeAsync,
                     list.ToArray(),
                     disposableValues.GenerateOnDisposingAsync, 
                     disposableValues.GenerateOnDisposedAsync,
                     disposableValues.ConfigureAwait));
        }

        return classesToGenerate;
    }
}