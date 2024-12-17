using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_CelestialBodyRing_EnableModels
    {
        public static bool Prefix(List<CelestialBodyRingModel> ___models) => ___models != null;
    }
}
