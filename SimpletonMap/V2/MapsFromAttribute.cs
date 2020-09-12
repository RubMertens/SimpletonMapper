using System;

namespace SimpletonMap.V2
{
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