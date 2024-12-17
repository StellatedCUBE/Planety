using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    public class SkyboxVisibilityData
    {
        public double? innerAltitude, outerAltitude;
        public float? day, night, blendDistanceScale;

        internal void UpdateLightingData(KSP.Rendering.CelestialBodyLightingData lightingData)
        {
            if (innerAltitude.HasValue) lightingData.skyboxVisibilityInnerAltitude = innerAltitude.Value;
            if (outerAltitude.HasValue) lightingData.skyboxVisibilityOuterAltitude = outerAltitude.Value;
            if (day.HasValue) lightingData.dayVisibility = day.Value;
            if (night.HasValue) lightingData.nightVisibility = night.Value;
            if (blendDistanceScale.HasValue) lightingData.dayNightBlendDistanceScale = blendDistanceScale.Value;
        }
    }
}
