using System;

namespace SimpletonMap.V4
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