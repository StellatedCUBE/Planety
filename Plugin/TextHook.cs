using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    public static class TextHook
    {
        public static Dictionary<string, string> map = new();

        public static string Get(string x) => x != null && map.TryGetValue(x, out string y) ? y : x;
    }
}
