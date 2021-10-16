using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Simpleton.SourceGenerator
{
    public class SourceBuilder
    {
        private readonly ITypeSymbol _from;
        private readonly ITypeSymbol _to;
        private readonly MatchingProperties[] _matchingProperties;

        public string GeneratedClassName => $"{_from.ToString().SanitizeNameForDeclaration()}_To_{_to.ToString().SanitizeNameForDeclaration()}_Simpleton_Extensions";

        public SourceBuilder(ITypeSymbol @from, ITypeSymbol to, IEnumerable<MatchingProperties> matchingProperties)
        {
            _from = @from;
            _to = to;
            _matchingProperties = matchingProperties.ToArray();
        }
        
        public SourceText GenerateSourceText()
        {
            return SourceText.From($@"
using System;
namespace SimpletonMap.SourceGenerator {{
    public static class {GeneratedClassName} {{
        public static {_to} From(this {_to} self, {_from} from){{
            {_matchingProperties.Select(p => $"self.{p.To.Name} = from.{p.From.Name};").Join("\n")}
            return self;
        }}

        public static {_to} To{_to.Name}(this {_from} from){{
            return new {_to}(){{
                {_matchingProperties.Select(p => $"{p.To.Name} = from.{p.From.Name}").Join(",")}
            }};
        }}
    }}
}}
", Encoding.UTF8);
        }
    }
}