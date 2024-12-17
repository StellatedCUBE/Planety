using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PlanetyPatch
{
    public static class Patcher
    {
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

        public static void Patch(AssemblyDefinition gameAssembly)
        {
            var modAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../plugins/Planety/Planety.dll"));

            var modFunc = gameAssembly.MainModule.ImportReference((from MethodDefinition mmd in modAssembly.MainModule.GetType("Planety.AssetLoader").Methods where mmd.Name == "CreateAsyncPrefix" select mmd).First());

            MethodDefinition CreateAsync = null;
            foreach (MethodDefinition method in gameAssembly.MainModule.GetType("KSP.Assets.AssetProvider").Methods)
            {
                if (method.Name == "CreateAsync" && (CreateAsync == null || CreateAsync.Parameters.Count < method.Parameters.Count))
                    CreateAsync = method;
            }

            var ret = CreateAsync.Body.Instructions.Last();

            var processor = CreateAsync.Body.GetILProcessor();

            var prev = processor.Create(OpCodes.Nop);
            var ins = prev;
            processor.InsertBefore(CreateAsync.Body.Instructions[0], prev);

            for (byte i = 0; i < 6; i++)
            {
                ins = processor.Create(OpCodes.Ldarg_S, i);
                processor.InsertAfter(prev, ins);
                prev = ins;
            }

            ins = processor.Create(OpCodes.Call, modFunc);
            processor.InsertAfter(prev, ins);
            prev = ins;

            ins = processor.Create(OpCodes.Brfalse, ret);
            processor.InsertAfter(prev, ins);
        }
    }
}
