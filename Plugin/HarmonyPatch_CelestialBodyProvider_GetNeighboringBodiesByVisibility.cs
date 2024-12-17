using HarmonyLib;
using KSP.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_CelestialBodyProvider_GetNeighboringBodiesByVisibility
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //int i = 0;
            foreach (var ins in instructions)
            {
                yield return ins;
                /*if (i == 131)
                    yield return new(OpCodes.Call, typeof(GetNeighboringBodiesByVisibilityLogger).GetMethod("Log"));
                i++;*/
                if (ins.opcode == OpCodes.Call && ((MethodInfo)ins.operand).Name == "GetArcRadius")
                {
                    yield return new(OpCodes.Ldloc_S, 4);
                    yield return new(OpCodes.Call, typeof(HarmonyPatch_CelestialBodyProvider_GetNeighboringBodiesByVisibility).GetMethod("Expand"));
                }
            }
        }

        /*
        public static object Log(object list)
        {
            Plugin.Log("GetNeighboringBodiesByVisibilityLogger");
            foreach (var i in (IEnumerable)list)
            {
                string s = "";
                foreach (var f in i.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                    s += $"{f.Name}: {f.GetValue(i)}; ";
                Plugin.Log(s);
            }
            return list;
        }*/

        public static double Expand(double r, string body) => ContentLoader.GetBody(body).IsStar ? Math.Max(r, 0.0005641895835477564) : r;
    }
}
