using KSP.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_CheatsMenu_Awake
    {
        public static bool Prefix(CheatsMenu __instance) => __instance.name == "CheatsMenu(Clone)";
    }
}
