using HarmonyLib;
using KSP.Sim.impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_CelestialBodyProvider_GetObservedStar
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Callvirt && ((MethodInfo)ins.operand).Name == "get_bodyName")
                    yield return new(OpCodes.Call, typeof(HarmonyPatch_CelestialBodyProvider_GetObservedStar).GetMethod("BodyName"));
                else
                    yield return ins;
            }
        }

        public static string BodyName(CelestialBodyComponent cbc) => cbc == null ? "" : cbc.bodyName;
    }
}
