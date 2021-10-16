using System.Collections.Generic;

namespace Simpleton.SourceGenerator
{
    public static class StringExtensions
    {
        public static string Join(this IEnumerable<string> str, string separator)
        {
            return string.Join(separator, str);
        }
    }
}