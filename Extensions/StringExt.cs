using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IgnitionHelper.Extensions
{
    public static class StringExt
    {
        public static bool Contains(this string? source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static bool ContainsMany(this string source, string toCheck, StringComparison comp)
        {
            return toCheck.Split(";").ToArray().Any(a => source.Contains(a, comp));
        }
    }
}
