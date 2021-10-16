using System;

namespace SimpletonMap.SourceGenerator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MappedFromAttribute: Attribute
    {
        public Type FromType { get; }

        public MappedFromAttribute(Type fromType)
        {
            FromType = fromType;
        }
    }
    
    [AttributeUsage(AttributeTargets.Property)]
    public class MapsFromAttribute:Attribute
    {
        public string MapsFromName;

        public MapsFromAttribute(string mapsFromName)
        {
            MapsFromName = mapsFromName;
        }
    }
}