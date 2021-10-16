using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Simpleton.SourceGenerator
{
    public static class RoslynHelpers
    {
        public static bool HasAttribute(this ClassDeclarationSyntax cds, string attributeName)
        {
            var attributes = cds.AttributeLists;
            var hasAttribute = attributes
                .SelectMany(a => a.Attributes)
                .Any(a =>
                    a.Name.GetText().ToString() == attributeName.Replace("Attribute", ""));

            return hasAttribute;
        }

        public static AttributeSyntax GetAttributeSyntax(this ClassDeclarationSyntax cds, string attributeName)
        {
            return cds.AttributeLists.SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == attributeName.Replace("Attribute", ""));
        }
        public static IEnumerable<IPropertySymbol> GetPropertySymbols(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers()
                .Where(m => m is IPropertySymbol)
                .OfType<IPropertySymbol>()
                .ToArray();
        }

        public static string SanitizeNameForDeclaration(this string name)
        {
            return name
                .Replace(".", "_")
                .Replace("+", "_");
        }
    }
}