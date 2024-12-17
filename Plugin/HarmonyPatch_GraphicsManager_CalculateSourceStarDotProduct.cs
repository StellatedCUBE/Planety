using KSP.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_GraphicsManager_CalculateSourceStarDotProduct
    {
        public static bool Prefix(GraphicsManager __instance, ref float __result)
        {
            if (string.IsNullOrEmpty(__instance.LightingSystem.GetCurrentStarName()))
            {
                __result = -1;
                return false;
            }
            return true;
        }
    }
}
