using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_UIValue_ReadString_Text_RedrawValue
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool flag = true;

            foreach (var ins in instructions)
            {
                yield return ins;
                if (flag && ins.opcode == OpCodes.Call)
                {
                    flag = false;
                    yield return new CodeInstruction(OpCodes.Call, typeof(TextHook).GetMethod("Get"));
                }
            }
        }
    }
}
