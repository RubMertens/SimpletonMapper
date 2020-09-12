using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpletonMap.V2
{
    public class TypeMapping
    {
        private readonly Type _fromType;
        private readonly Type _toType;
        
        public List<MatchingProperties> MatchingProperties { get; } = new List<MatchingProperties>();

        public TypeMapping(Type fromType, Type toType)
        {
            _fromType = fromType;
            _toType = toType;
            MatchingProperties.AddRange(FindMatchingProperties(fromType, toType));
        }
        
        private MatchingProperties[] FindMatchingProperties(Type from, Type to)
        {
            
            var matchingProperties = from
                .GetProperties()
                .SelectMany(from =>
                {
                    var matchingByAttribute =
                        to
                            .GetProperties()
                            .Where(to =>
                            {
                                var mapsFromAttribute = to.GetCustomAttribute<MapsFromAttribute>();
                                var name = mapsFromAttribute?.MapsFromName;
                                return name == from.Name;
                            })
                            .Select(to => new MatchingProperties()
                            {
                                To = to,
                                From = from
                            });
                    
                    var matchingByName= to.GetProperties()
                        .Where(to => matchingByAttribute
                            .All(mba => mba.To != to))
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
                            });
                    return matchingByAttribute
                        .Concat(matchingByName)
                        .ToArray();
                });
            return matchingProperties.ToArray();
        }
        
        
    }
    
    public class TypeMapping<TFrom, TTo>
        :TypeMapping
    {
        public TypeMapping<TFrom, TTo> With(
            Expression<Func<TFrom, object>> fromPropertySelector,
            Expression<Func<TTo, object>> toPropertySelector
        )
        {
            var fromInfo = GetPropertyInfoFromExpression<TFrom>(fromPropertySelector);
            var toInfo = GetPropertyInfoFromExpression<TTo>(toPropertySelector);
            if(fromInfo == null)
                throw new InvalidOperationException($"Can't find property on {typeof(TFrom)}");
            if(toInfo == null)
                throw new  InvalidOperationException($"Can't find proerty on {typeof(TTo)}");
            
            MatchingProperties.Add(new MatchingProperties()
            {
                From = fromInfo,
                To = toInfo
            });
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
    
    public class SimpletonMapper
    {
        private readonly Dictionary<Type, TypeMapping> _typeMappingsByFromType 
            = new Dictionary<Type, TypeMapping>();
        
        public TypeMapping<TFrom, TTo> Register<TFrom, TTo>()
        {
            var fromType = typeof(TFrom);
            var typeMapping = new TypeMapping<TFrom, TTo>();
            _typeMappingsByFromType.Add(fromType, typeMapping);
            return typeMapping;
        }
        
        public TTo Map<TTo>(object fromInstance)
        {
            var toType = typeof(TTo);
            var fromType = fromInstance.GetType();
            
            if(!_typeMappingsByFromType.ContainsKey(fromType))
                throw new InvalidOperationException($"No mapping registered from {fromType.Name} to {toType.Name}");

            var typeMapping = _typeMappingsByFromType[fromType];
            
            var toInstance= Activator.CreateInstance<TTo>();

            foreach (var property in typeMapping.MatchingProperties)
            {
                var fromValue = property.From.GetValue(fromInstance);
                property.To.SetValue(toInstance, fromValue);
            }
            return toInstance;
        }
    }

    public struct MatchingProperties
    {
        public PropertyInfo From { get; set; }
        public PropertyInfo To { get; set; }
    }
}