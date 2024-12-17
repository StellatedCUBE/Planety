using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class LightingData
    {
        public class Ambient
        {
            public double? innerAltitude, outerAltitude;
            public Color? day, night, scaled;
        }

        public float? horizonOffset;
        public float? dayBlendRange;
        public float? nightBlendRange;
        public Ambient ambient = new();
        public double? directionalInnerAltitude;
        public double? directionalOuterAltitude;

        internal void UpdateLightingData(KSP.Rendering.CelestialBodyLightingData lightingData)
        {
            if (horizonOffset.HasValue) lightingData.horizonOffset = horizonOffset.Value;
            if (dayBlendRange.HasValue) lightingData.dayBlendRange = dayBlendRange.Value;
            if (nightBlendRange.HasValue) lightingData.nightBlendRange = nightBlendRange.Value;
            if (directionalInnerAltitude.HasValue) lightingData.directionalInnerAltitude = directionalInnerAltitude.Value;
            if (directionalOuterAltitude.HasValue) lightingData.directionalOuterAltitude = directionalOuterAltitude.Value;
            if (ambient.innerAltitude.HasValue) lightingData.ambientInnerAltitude = ambient.innerAltitude.Value;
            if (ambient.outerAltitude.HasValue) lightingData.ambientOuterAltitude = ambient.outerAltitude.Value;
            if (ambient.day.HasValue) lightingData.ambientDay = ambient.day.Value.WithAlpha(0);
            if (ambient.night.HasValue) lightingData.ambientNight = ambient.night.Value.WithAlpha(0);
            if (ambient.scaled.HasValue) lightingData.ambientScaled = ambient.scaled.Value.WithAlpha(0);
            lightingData.useAmbient = lightingData.ambientNight.grayscale + lightingData.ambientNight.grayscale + lightingData.ambientScaled.grayscale > 0;
        }
    }
}
