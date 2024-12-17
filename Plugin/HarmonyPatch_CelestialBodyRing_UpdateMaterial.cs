using KSP.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_CelestialBodyRing_UpdateMaterial
    {
        public static bool Prefix(CelestialBodyRing __instance) => __instance.ringShader;
    }
}
