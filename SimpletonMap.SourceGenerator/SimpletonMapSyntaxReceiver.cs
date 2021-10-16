using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimpletonMap.SourceGenerator
{
    public class SimpletonMapSyntaxReceiver : ISyntaxReceiver
    {
        public ClassDeclarationSyntax ClassWithAttribute { get; set; }

        /// <summary>
        /// Inspect every syntax node whether it is class with the correct attribute 
        /// </summary>
        /// <param name="syntaxNode"></param>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (!(syntaxNode is ClassDeclarationSyntax cds)) return;
            if (!cds.HasAttribute(nameof(MappedFromAttribute))) return;
            ClassWithAttribute = cds;
        }
    }
}