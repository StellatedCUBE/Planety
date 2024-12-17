using KSP.Rendering.Planets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    static class HarmonyPatch_PQSRenderer_DrawPQSDeferredDecalSurfacePass
    {
        static ShaderRefactorState old;

        public static void Prefix(Material selectedMaterial)
        {
            old = PQSRenderer.ShaderRefactorState;
            if (old == ShaderRefactorState.Standard && selectedMaterial.shader.name.EndsWith("_Old"))
                PQSRenderer.ShaderRefactorState = ShaderRefactorState.SwapOld;
        }

        public static void Postfix()
        {
            PQSRenderer.ShaderRefactorState = old;
            //Plugin.Log(BepInEx.Logging.LogLevel.Info, "drawplanet");
        }
    }
}
