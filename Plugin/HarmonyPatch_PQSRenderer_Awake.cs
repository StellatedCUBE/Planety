using KSP.Rendering.Planets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_PQSRenderer_Awake
    {
        public static void Prefix(PQSRenderer __instance)
        {
            if (!__instance.Pqs)
                __instance.Pqs = __instance.GetComponent<PQS>();
        }
    }
}
