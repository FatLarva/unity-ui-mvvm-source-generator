using Microsoft.CodeAnalysis;

namespace ViewsSourceGenerator.Extensions
{
    public static class NamedTypeSymbolExtensions
    {
        public static string GetFullNamespace(this INamedTypeSymbol namedTypeSymbol)
        {
            INamespaceSymbol namespaceSymbol = namedTypeSymbol.ContainingNamespace;
            if (namespaceSymbol.IsGlobalNamespace)
            {
                return string.Empty;
            }

            string result = namespaceSymbol.Name;
            while (!namespaceSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                namespaceSymbol = namespaceSymbol.ContainingNamespace;
                result = namespaceSymbol.Name + "." + result;
            }

            return result;
        }
    }
}