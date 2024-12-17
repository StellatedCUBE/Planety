using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public static class Extensions
    {
        public static Color WithAlpha(this Color c, float a) => new(c.r, c.g, c.b, a);
    }
}
