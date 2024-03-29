using System.Collections.Generic;

namespace SimpletonMap.SourceGenerator
{
    public static class StringExtensions
    {
        public static string Join(this IEnumerable<string> str, string separator)
        {
            return string.Join(separator, str);
        }
    }
}