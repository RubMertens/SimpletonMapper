using Microsoft.CodeAnalysis;

namespace SimpletonMap.SourceGenerator
{
    public struct MatchingProperties
    {
        public IPropertySymbol From { get; set; }
        public IPropertySymbol To { get; set; }
    }
}