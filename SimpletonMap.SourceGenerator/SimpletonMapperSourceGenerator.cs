using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimpletonMap.SourceGenerator
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
                            var mappedFromAttributeData = to.GetAttributes()
                                .FirstOrDefault(a =>
                                    //the actual equality comparison doesn't seem to work, but the name(space)s are the same.
                                    //use that to compare instead ¯\_(ツ)_/¯
                                    a.AttributeClass.ToString().Equals(mappedFromAttributeSymbol.ToString(),
                                        StringComparison.InvariantCulture)
                                );
                            if (mappedFromAttributeData == null) return false;
                            var configuredMapFromPropertyName = mappedFromAttributeData
                                //attribute has a constructor
                                .ConstructorArguments
                                //with the first arguement = to the name we expect to map to
                                .First()
                                .Value;
                            return from.Name.Equals(configuredMapFromPropertyName);
                        })
                        .Select(to => new MatchingProperties()
                        {
                            From = from,
                            To = to
                        });
                }).ToArray();
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

            // var mappedFromAttributeTypeSymbol = semanticModel.GetTypeInfo(mappedFromAttribute).Type
            //     as INamedTypeSymbol
            //     ; 

            // var mapsFromAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("SimpletonMap.V5.MapsFromAttribute");
            var mapsFromAttributeTypeSymbol = context
                .Compilation
                .GetTypeByMetadataName(typeof(MapsFromAttribute)?.FullName ?? "");

            var fromTypeInfo = semanticModel
                    .GetTypeInfo(fromTypeSyntax)
                    .Type
                ;

            var toTypeInfo = semanticModel
                    .GetDeclaredSymbol(receiver.ClassWithAttribute)
                as INamedTypeSymbol;

            var matchingPropertiesByName =
                GetMatchingPropertiesBasedOnNames(fromTypeInfo, toTypeInfo);
            var matchingPropertiesByAttribute =
                GetMatchingPropertiesByAttribute(fromTypeInfo, toTypeInfo, mapsFromAttributeTypeSymbol);
            // Debugger.Launch();
            var sourceBuilder = new SourceBuilder(
                fromTypeInfo,
                toTypeInfo,
                matchingPropertiesByName.Concat(matchingPropertiesByAttribute)
            );
            var mapperClassSource =
                sourceBuilder.GenerateSourceText();

            context.AddSource($"{sourceBuilder.GeneratedClassName}.cs", mapperClassSource);
        }
    }
}