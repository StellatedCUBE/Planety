using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class Scope : Node
    {
        public override object Eval(Context ctx) => ctx.scope;
    }
}
