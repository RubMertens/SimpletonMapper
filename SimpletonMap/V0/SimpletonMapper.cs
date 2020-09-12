using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpletonMap.V0
{
    public class SimpletonMapper
    {
        private readonly Dictionary<Type, Type> _registeredTypes = new Dictionary<Type, Type>();

        private readonly Dictionary<Type, MatchingProperties[]> _matchingPropertiesByFromType =
            new Dictionary<Type, MatchingProperties[]>();

        public void Register<TFrom, TTo>()
        {
            var fromType = typeof(TFrom);
            var toType = typeof(TTo);
            _registeredTypes.Add(fromType, toType);
            var matchingProperties = FindMatchingProperties(fromType, toType);
            _matchingPropertiesByFromType.Add(fromType, matchingProperties);
        }

        private MatchingProperties[] FindMatchingProperties(Type from, Type to)
        {
            var matchingProperties = from
                .GetProperties()
                .SelectMany(from =>
                    to.GetProperties()
                        .Where(to =>
                            from.PropertyType == to.PropertyType
                            && from.PropertyType.IsPublic
                            && to.PropertyType.IsPublic
                            && from.Name == to.Name
                            && from.CanRead
                            && from.CanWrite
                            && to.CanRead
                            && to.CanWrite
                        )
                        .Select(to =>
                            new MatchingProperties
                            {
                                From = from,
                                To = to
                            }));
            return matchingProperties.ToArray();
        }
        public TTo Map<TTo>(object fromInstance)
        {
            var toType = typeof(TTo);
            var fromType = fromInstance.GetType();
            
            if(!_registeredTypes.ContainsKey(fromType))
                throw new InvalidOperationException($"No mapping registered from {fromType.Name} to {toType.Name}");

            var matchingProperties = _matchingPropertiesByFromType[fromType];
            var toInstance= Activator.CreateInstance<TTo>();

            foreach (var property in matchingProperties)
            {
                var fromValue = property.From.GetValue(fromInstance);
                property.To.SetValue(toInstance, fromValue);
            }
            return toInstance;
        }
    }

    internal struct MatchingProperties
    {
        public PropertyInfo From { get; set; }
        public PropertyInfo To { get; set; }
    }
}