﻿using KSP.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_CelestialBodyLighting_Awake
    {
        public static bool Prefix(CelestialBodyLighting __instance) => __instance.Data != null;
    }
}