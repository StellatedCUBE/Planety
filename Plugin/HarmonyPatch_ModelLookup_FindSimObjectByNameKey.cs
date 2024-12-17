using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_ModelLookup_FindSimObjectByNameKey
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool flag = false;
            foreach (var ins in instructions)
            {
                if (flag)
                    flag = false;
                else if (ins.opcode == OpCodes.Ldstr)
                    flag = true;
                else
                    yield return ins;
            }
        }
    }
}
