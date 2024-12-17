using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader
{
    public class Context
    {
        internal static Dictionary<string, object> GlobalScopeTemplate
        {
            get
            {
                if (globalScopeTemplate_bk == null)
                    CreateGlobalScopeTemplate();
                return globalScopeTemplate_bk;
            }
        }

        static Dictionary<string, object> globalScopeTemplate_bk;

        static void CreateGlobalScopeTemplate()
        {
            globalScopeTemplate_bk = new Dictionary<string, object>
            {
                { "true", true },
                { "false", false },
                { "null", null }
            };

            foreach (var method in typeof(ScriptFunctions).GetMethods())
                globalScopeTemplate_bk[Fields.TransformName(method.Name)] = ((object, MethodInfo))(null, method);
        }

        public Mod mod;
        public string currentCodePath;
        public Dictionary<string, object> scope;

        public Context(Mod @for)
        {
            mod = @for;
            scope = new Dictionary<string, object>(GlobalScopeTemplate);

            foreach (var body in ContentLoader.stockBodyMap.Values)
                scope[Fields.TransformName(body.id)] = body;
        }
    }
}
