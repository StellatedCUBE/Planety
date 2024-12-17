using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_LogMethodCall
    {
        static void Prefix(MethodBase __originalMethod)
        {
            if (logging)
                Plugin.Log(BepInEx.Logging.LogLevel.Info, "Entering " + __originalMethod.Name);
        }

        static HarmonyMethod prefix = new HarmonyMethod(typeof(HarmonyPatch_LogMethodCall).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic));
        internal static bool logging;

        public static void Log(this Harmony harmony, string to)
        {
            harmony.Patch(AccessTools.Method(to), prefix);
        }
    }
}
