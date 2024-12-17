using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class Light
    {
        public Color? color;
        public double range = 5e11;
        public double power = 3.06e24;
        public AnimationCurve falloffCurve;

        internal void UpdateLightingData(KSP.Rendering.CelestialBodyLightingData lightingData)
        {
            lightingData.lightFalloffDistance = range;
            if (falloffCurve != null) lightingData.lightFalloffCurve = falloffCurve;
        }
    }
}
