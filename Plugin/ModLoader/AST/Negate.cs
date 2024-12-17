using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class Negate : Node
    {
        public Node value;

        public override object Eval(Context ctx) => -Convert.ToDouble(value.Eval(ctx));
    }
}
