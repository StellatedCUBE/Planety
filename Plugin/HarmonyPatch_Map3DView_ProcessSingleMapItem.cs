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
    static class HarmonyPatch_Map3DView_ProcessSingleMapItem
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Callvirt && ((MethodInfo)ins.operand).Name == "get_IsStar")
                    yield return new(OpCodes.Call, typeof(HarmonyPatch_Map3DView_ProcessSingleMapItem).GetMethod("IsSystemRoot"));
                else
                    yield return ins;
            }
        }

        public static bool IsSystemRoot(CelestialBodyComponent cbc) => ContentLoader.GetBody(cbc.bodyName).orbit == null;
    }
}
