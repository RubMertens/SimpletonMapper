using Microsoft.CodeAnalysis;

namespace Simpleton.SourceGenerator
{
    public struct MatchingProperties
    {
        public IPropertySymbol From { get; set; }
        public IPropertySymbol To { get; set; }
    }
}