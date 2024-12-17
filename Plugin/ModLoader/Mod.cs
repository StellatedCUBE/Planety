using BepInEx.Logging;
using Planety.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader
{
    public class Mod
    {
        public string id, name, path;
        public RunTime runTime;
        public AST.Block code;
        public bool always;
        public List<Parameter> parameters = new();

        internal void Load()
        {
            Plugin.Log(LogLevel.Info, "Loading mod " + id);

            try
            {
                code.Eval(new Context(this));
            }
            catch (Exception e)
            {
                Plugin.PopError(e, path);
            }
        }

        public class Parameter
        {
            public string name, displayname, description;
            public Type type;
            public string defaultValue;
            public double min, max, step;
            public string[] options;
            public InputType inputType;
            public bool canRoll;
        }

        public class Extension : Mod
        {
            public bool failSilently;
            public (int, int) pos;
        }
    }
}
