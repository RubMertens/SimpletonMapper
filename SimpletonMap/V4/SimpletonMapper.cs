#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SimpletonMap.V4
{
    public class TypeMapping
    {
        public Type FromType { get; }
        public Type ToType { get; }

        protected readonly Dictionary<PropertyInfo, PropertyInfo> FromInfoByToInfo
            = new Dictionary<PropertyInfo, PropertyInfo>();

        public List<MatchingProperties> MatchingProperties => FromInfoByToInfo.Select(kvp => new MatchingProperties()
        {
            From = kvp.Value,
            To = kvp.Key
        }).ToList();

        public TypeMapping(Type fromType, Type toType)
        {
            FromType = fromType;
            ToType = toType;
            foreach (var properties in MatchingPropertiesByName(fromType, toType)
                .Concat(MatchingPropertiesByAttribute(fromType, toType)))
            {
                FromInfoByToInfo[properties.To] = properties.From;
            }
        }

        private static IEnumerable<MatchingProperties> MatchingPropertiesByName(Type fromType, Type toType)
        {
            return fromType
                .GetProperties()
                .SelectMany(from => toType.GetProperties()
                    .Where(to =>
                        @from.PropertyType == to.PropertyType
                        && @from.PropertyType.IsPublic
                        && to.PropertyType.IsPublic
                        && @from.Name == to.Name
                        && @from.CanRead
                        && @from.CanWrite
                        && to.CanRead
                        && to.CanWrite
                    )
                    .Select(to =>
                        new MatchingProperties
                        {
                            From = @from,
                            To = to
                        }));
        }

        private static IEnumerable<MatchingProperties> MatchingPropertiesByAttribute(Type fromType, Type toType)
        {
            return fromType
                .GetProperties()
                .SelectMany(from =>
                    toType
                        .GetProperties()
                        .Where(to =>
                            @from.PropertyType == to.PropertyType
                            && @from.PropertyType.IsPublic
                            && to.PropertyType.IsPublic
                            && @from.CanRead
                            && @from.CanWrite
                            && to.CanRead
                            && to.CanWrite)
                        .Where(to =>
                        {
                            var mapsFromAttribute = to.GetCustomAttribute<MapsFromAttribute>();
                            var name = mapsFromAttribute?.MapsFromName;
                            return name == @from.Name;
                        })
                        .Select(to => new MatchingProperties()
                        {
                            To = to,
                            From = @from
                        }));
        }
    }

    public class TypeMapping<TFrom, TTo>
        : TypeMapping
    {
        public TypeMapping<TFrom, TTo> With(
            Expression<Func<TFrom, object>> fromPropertySelector,
            Expression<Func<TTo, object>> toPropertySelector
        )
        {
            var fromInfo = GetPropertyInfoFromExpression<TFrom>(fromPropertySelector);
            var toInfo = GetPropertyInfoFromExpression<TTo>(toPropertySelector);
            if (fromInfo == null)
                throw new InvalidOperationException($"Can't find property on {typeof(TFrom)}");
            if (toInfo == null)
                throw new InvalidOperationException($"Can't find proerty on {typeof(TTo)}");

            FromInfoByToInfo[toInfo] = fromInfo;
            return this;
        }

        private PropertyInfo GetPropertyInfoFromExpression<T>(Expression<Func<T, object>> propertySelector)
        {
            if (propertySelector.Body is MemberExpression memberExpression)
            {
                return typeof(T).GetProperty(memberExpression.Member.Name);
            }

            return null;
        }

        public TypeMapping() : base(typeof(TFrom), typeof(TTo))
        {
        }
    }

    interface ITypeMapper
    {
        TTo Map<TTo>(object fromInstance);
    }

    public class ReflectionTypeMapper : ITypeMapper
    {
        private readonly TypeMapping _typeMapping;

        public ReflectionTypeMapper(TypeMapping typeMapping)
        {
            _typeMapping = typeMapping;
        }

        public TTo Map<TTo>(object fromInstance)
        {
            var toInstance = Activator.CreateInstance<TTo>();
            foreach (var property in _typeMapping.MatchingProperties)
            {
                var fromValue = property.From.GetValue(fromInstance);
                property.To.SetValue(toInstance, fromValue);
            }

            return toInstance;
        }
    }

    public class RoslynTypeMapper : ITypeMapper
    {
        private readonly TypeMapping _typeMapping;
        private Type _mapper;

        public RoslynTypeMapper(TypeMapping typeMapping)
        {
            _typeMapping = typeMapping;
            BuildMapper();
        }

        private void BuildMapper()
        {
            var matchingProperties = _typeMapping.MatchingProperties;
            var fromType = _typeMapping.FromType;
            var toType = _typeMapping.ToType;
            var mapClassName = $"SimpletonMapper_{_typeMapping.FromType.Name}_MapTo_{_typeMapping.ToType.Name}";

            var template = $@"
using SimpletonMap.V3;
namespace SimpletonMap_Copy {{
    public static class {mapClassName} {{
        public static {toType.FullName} Map({fromType.FullName} fromInstance){{
            return new {toType.FullName}() {{
                {
                    string
                        .Join(",",
                            matchingProperties
                                .Select(match =>
                                    $"{match.To.Name} = fromInstance.{match.From.Name}"))
                }
            }};
        }}
    }}
}}";

            var refPaths = new[]
            {
                typeof(object).GetTypeInfo().Assembly.Location,
                Path.Combine(Path.GetDirectoryName(typeof(GCSettings).GetTypeInfo().Assembly.Location),
                    "System.Runtime.dll"),
                this.GetType().Assembly.Location,
                fromType.Assembly.Location,
                toType.Assembly.Location
            };

            var references = refPaths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();

            var syntaxTree = CSharpSyntaxTree.ParseText(template);
            var compilation = CSharpCompilation.Create(
                $"SimpletonMapCopy_{Guid.NewGuid()}",
                new[] {syntaxTree},
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var memoryStream = new MemoryStream();
            var result = compilation.Emit(memoryStream);
            if (!result.Success)
            {
                throw new Exception("Couldn't compile mapper class");
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(memoryStream);
            var mapperType = assembly.GetType($"SimpletonMap_Copy.{mapClassName}");
            if (mapperType == null)
                throw new Exception("Couldn't find mapper type");
            _mapper = mapperType;
        }

        public TTo Map<TTo>(object fromInstance)
        {
            var toInstance = _mapper.InvokeMember(
                "Map",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                null,
                null,
                new[] {fromInstance}
            );
            return (TTo) toInstance;
        }
    }

    public class ILTypeMapper : ITypeMapper
    {
        private TypeMapping _typeMapping;
        private DynamicMethod _mapperMethod;

        public ILTypeMapper(TypeMapping typeMapping)
        {
            _typeMapping = typeMapping;
            BuildMapper();
        }

        private void BuildMapper()
        {
            var name = $"Map_From_{_typeMapping.FromType.Name}_To_{_typeMapping.ToType.Name}";

            var dynamicMethod = new DynamicMethod(
                name,
                _typeMapping.ToType,
                new[] {_typeMapping.FromType}, 
                typeof(SimpletonMapper).Module
            );
            var il = dynamicMethod.GetILGenerator();
            
            il.Emit(OpCodes.Newobj,_typeMapping.ToType.GetConstructor(Type.EmptyTypes));
            foreach (var mp in _typeMapping.MatchingProperties)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldarg_0);
                il.EmitCall(OpCodes.Callvirt, mp.From.GetGetMethod(),null);
                il.EmitCall(OpCodes.Callvirt, mp.To.GetSetMethod(),null);
            }            
            il.Emit(OpCodes.Ret);
            _mapperMethod = dynamicMethod;
        }

        public TTo Map<TTo>(object fromInstance)
        {
            var toInstance = _mapperMethod.Invoke(null, new[] {fromInstance});
            return (TTo) toInstance;
        }
    }

    public enum MappingStrategy
    {
        Reflection,
        Roslyn,
        Il
    }

    public class SimpletonMapper
    {
        private readonly Dictionary<Type, TypeMapping> _typeMappingsByFromType
            = new Dictionary<Type, TypeMapping>();

        private readonly Dictionary<Type, ITypeMapper> _typeMappersByFromType
            = new Dictionary<Type, ITypeMapper>();

        private readonly MappingStrategy _strategy;

        public SimpletonMapper(MappingStrategy strategy)
        {
            _strategy = strategy;
        }

        public TypeMapping<TFrom, TTo> Register<TFrom, TTo>()
        {
            var fromType = typeof(TFrom);
            var typeMapping = new TypeMapping<TFrom, TTo>();
            _typeMappingsByFromType.Add(fromType, typeMapping);
            return typeMapping;
        }

        public void Build()
        {
            foreach (var (fromType, typeMapping) in _typeMappingsByFromType)
            {
                if (!_typeMappersByFromType.ContainsKey(fromType))
                {
                    _typeMappersByFromType.Add(fromType, MapperForStrategy(typeMapping));
                }
            }
        }

        private ITypeMapper MapperForStrategy(TypeMapping typeMapping)
        {
            switch (_strategy)
            {
                case MappingStrategy.Reflection:
                    return new ReflectionTypeMapper(typeMapping);
                case MappingStrategy.Roslyn:
                    return new RoslynTypeMapper(typeMapping);
                case MappingStrategy.Il:
                    return new ILTypeMapper(typeMapping);
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public TTo Map<TTo>(object fromInstance)
        {
            var toType = typeof(TTo);
            var fromType = fromInstance.GetType();

            if (!_typeMappingsByFromType.ContainsKey(fromType))
                throw new InvalidOperationException($"No mapping registered from {fromType.Name} to {toType.Name}");

            var typeMapping = _typeMappingsByFromType[fromType];

            if (!_typeMappersByFromType.ContainsKey(fromType))
            {
                _typeMappersByFromType.Add(fromType, MapperForStrategy(typeMapping));
            }

            var mapper = _typeMappersByFromType[fromType];
            return mapper.Map<TTo>(fromInstance);
        }
    }

    public struct MatchingProperties
    {
        public PropertyInfo From { get; set; }
        public PropertyInfo To { get; set; }
    }
}