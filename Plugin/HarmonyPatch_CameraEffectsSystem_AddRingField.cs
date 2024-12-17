using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    static class HarmonyPatch_CameraEffectsSystem_AddRingField
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool flag = true;

            foreach (var ins in instructions)
            {
                yield return ins;

                if (ins.opcode == OpCodes.Ldloc_0 && flag)
                {
                    flag = false;
                    yield return new(OpCodes.Call, typeof(HarmonyPatch_CameraEffectsSystem_AddRingField).GetMethod("Activate"));
                }
            }
        }

        public static GameObject Activate(GameObject go)
        {
            go.SetActive(true);
            return go;
        }
    }
}
