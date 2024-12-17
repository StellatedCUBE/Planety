using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    static class HarmonyPatch_SaveLoadManager_PrivateLoadCommon
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Call, typeof(ContentLoader).GetMethod("Reset"));

            int actions_added = 0;
            var how_to_add_action = new List<CodeInstruction>();
            foreach (CodeInstruction ins in instructions)
            {
                yield return ins;
                how_to_add_action.Add(ins);

                if (ins.opcode == OpCodes.Callvirt && ((MethodInfo)ins.operand).Name == "AddAction")
                {
                    actions_added++;

                    if (actions_added == 20)
                    {
                        foreach (CodeInstruction ins2 in how_to_add_action)
                        {
                            if (ins2.opcode == OpCodes.Newobj)
                            {
                                yield return new CodeInstruction(OpCodes.Call, typeof(LoadStockDataFlowAction).GetMethod("CreateAction"));
                            }
                            else
                            {
                                yield return new CodeInstruction(ins2.opcode, ins2.operand);
                            }
                        }
                    }

                    how_to_add_action.Clear();
                }
            }
        }
    }
}
