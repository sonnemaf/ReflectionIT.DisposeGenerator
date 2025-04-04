using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ReflectionIT.DisposeGenerator;

public static class Extensions
{
    internal static bool DoesImplementInterfaces(this ITypeSymbol type, params string[] interfaces) =>
        type.AllInterfaces.Any(i => interfaces.Contains(i.ToString()));

    internal static bool DoesImplementIAsyncDisposable(this ITypeSymbol type) =>
        type.DoesImplementInterfaces("System.IDisposable");

    internal static bool DoesImplementIDisposable(this ITypeSymbol type) =>
        type.DoesImplementInterfaces("System.IAsyncDisposable");

    internal static bool DoesImplementDisposePattern(this ITypeSymbol type) =>
        type.DoesImplementInterfaces("System.IDisposable", "System.IAsyncDisposable");
}
