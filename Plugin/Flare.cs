using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class Flare
    {
        public class Context
        {
            public double? falloffDistance;
            public double alphaMinRange, alphaMaxRange;
            public float minScale, maxScale;
        }

        public double nearRange = 7.5e8, farRange = 1.5e9;
        public AnimationCurve falloffCurve;
        public Context map = new(), flight = new() {
            minScale = 10,
            maxScale = 5500,
            alphaMinRange = 8e8,
            alphaMaxRange = 1.2e9
        };
    }
}
