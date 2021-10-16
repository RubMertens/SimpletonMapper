using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SimpletonMap.V5;

namespace Simpleton.SourceGenerator
{
    [Generator]
    public class SimpletonMapperSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //register a class to receive every syntax node
            context.RegisterForSyntaxNotifications(() => new SimpletonMapSyntaxReceiver());
        }

        private static TypeSyntax GetTypeSyntaxFromMappedFromAttribute(AttributeSyntax syntax)
        {
            return syntax
                .DescendantNodes().OfType<TypeOfExpressionSyntax>()
                .SingleOrDefault()
                ?.Type;
        }

        private static IEnumerable<MatchingProperties> GetMatchingPropertiesByAttribute(
            ITypeSymbol fromTypeSymbol,
            ITypeSymbol toTypeSymbol, INamedTypeSymbol mappedFromAttributeSymbol)
        {
            Debugger.Launch();
            var fromProperties = fromTypeSymbol.GetPropertySymbols().ToArray();
            var toProperties = toTypeSymbol.GetPropertySymbols().ToArray();

            return fromProperties
                .SelectMany(from =>
                {
                    return toProperties
                        .Where(to =>
                            from.SetMethod?.DeclaredAccessibility == Accessibility.Public
                            && to.SetMethod?.DeclaredAccessibility == Accessibility.Public
                            && to.GetMethod?.DeclaredAccessibility == Accessibility.Public
                            && from.GetMethod?.DeclaredAccessibility == Accessibility.Public
                        )
                        .Where(to =>
                        {
                            var mappedFromAttributeData =  to.GetAttributes()
                                .FirstOrDefault(a => a.AttributeClass?.Equals(mappedFromAttributeSymbol) ?? false);
                            var arg = mappedFromAttributeData.ConstructorArguments.First();
                            var fromType = arg.Type;
                            return fromType.Equals(fromTypeSymbol);
                        })
                        
                        .Select(to =>  new MatchingProperties()
                        {
                            From = from,
                            To = to
                        });
                }); 
        }

        private static IEnumerable<MatchingProperties> GetMatchingPropertiesBasedOnNames(
            ITypeSymbol fromTypeSymbol,
            ITypeSymbol toTypeSymbol)
        {
            var fromProperties = fromTypeSymbol.GetPropertySymbols().ToArray();
            var toProperties = toTypeSymbol.GetPropertySymbols().ToArray();

            var matchingProperties = fromProperties
                .SelectMany(from =>
                {
                    return toProperties
                        .Where(to =>
                            //this implicitly also checks if the getter and setter even exist ¯\_(ツ)_/¯
                            //check if they are even public
                            from.SetMethod?.DeclaredAccessibility == Accessibility.Public
                            && to.SetMethod?.DeclaredAccessibility == Accessibility.Public
                            && from.GetMethod?.DeclaredAccessibility == Accessibility.Public
                            && to.GetMethod?.DeclaredAccessibility == Accessibility.Public
                            //check if the types match, no matching types = no mapping!
                            && SymbolEqualityComparer.Default.Equals(to.Type, from.Type)
                            //finally check if the names match
                            && from.Name.Equals(to.Name, StringComparison.InvariantCulture)
                        )
                        .Select(to => new MatchingProperties
                        {
                            From = @from,
                            To = to
                        });
                }).ToList();
            return matchingProperties;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = context.SyntaxReceiver as SimpletonMapSyntaxReceiver;
            if (receiver?.ClassWithAttribute == null) return;
            
            var mappedFromAttribute = receiver.ClassWithAttribute.GetAttributeSyntax(nameof(MappedFromAttribute));
            
            var fromTypeSyntax = GetTypeSyntaxFromMappedFromAttribute(mappedFromAttribute);
            var semanticModel = context.Compilation.GetSemanticModel(receiver.ClassWithAttribute.SyntaxTree);
            
            var fromTypeInfo = semanticModel
                .GetTypeInfo(fromTypeSyntax)
                .Type
                ;
            
            var toTypeInfo = semanticModel
                .GetDeclaredSymbol(receiver.ClassWithAttribute)
                as INamedTypeSymbol;

            var matchingPropertiesByName = 
                GetMatchingPropertiesBasedOnNames(fromTypeInfo, toTypeInfo);
            // var matchingPropertiesByAttribute =
            //     GetMatchingPropertiesByAttribute(fromTypeInfo, toTypeInfo, mappedFromAttributeSymbol);

            var sourceBuilder = new SourceBuilder(fromTypeInfo, toTypeInfo, matchingPropertiesByName);
            var mapperClassSource =
                sourceBuilder.GenerateSourceText();
            
            context.AddSource($"{sourceBuilder.GeneratedClassName}.cs", mapperClassSource);
        }
    }
}