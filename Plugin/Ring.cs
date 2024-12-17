using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class Ring
    {
        [NoScriptAccess]
        public int stockIndex = -1;
        public bool Stock { get => stockIndex >= 0; }

        public double? innerRadius, outerRadius;
        public float mie = 1, scatteringStrength = 0, edgeFade = 0.1f;

        [ScriptName("gradient")]
        [ScriptName("colors", true)]
        public Source<Texture2D> texture;

        public List<Source<GameObject>> particles = new();
    }
}
