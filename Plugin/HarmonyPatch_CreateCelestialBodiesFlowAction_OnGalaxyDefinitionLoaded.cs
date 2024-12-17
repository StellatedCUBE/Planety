using HarmonyLib;
using KSP.Sim.Definitions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Planety
{
    static class HarmonyPatch_CreateCelestialBodiesFlowAction_OnGalaxyDefinitionLoaded
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found_galaxy_write = false;
            foreach (CodeInstruction ins in instructions)
            {
                if (!found_galaxy_write && ins.opcode == OpCodes.Stfld)
                {
                    found_galaxy_write = true;
                    yield return new CodeInstruction(OpCodes.Call, typeof(AddContent).GetMethod("AlterSerializedGalaxyDefinition"));
                }

                if (ins.opcode == OpCodes.Ldfld && ((FieldInfo)ins.operand).Name == "IsStar")
                    yield return new CodeInstruction(OpCodes.Call, typeof(HarmonyPatch_CreateCelestialBodiesFlowAction_OnGalaxyDefinitionLoaded).GetMethod("IsSystemRoot"));
                else
                    yield return ins;
            }
        }

        public static bool IsSystemRoot(CelestialBodyProperties cbp) => ContentLoader.GetBody(cbp.bodyName).orbit == null;
    }
}
