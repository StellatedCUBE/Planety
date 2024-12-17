using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class MathBinOp : Node
    {
        public Node lhs, rhs;
        public Func<double, double, double> op;

        public override object Eval(Context ctx) => op(Convert.ToDouble(lhs.Eval(ctx)), Convert.ToDouble(rhs.Eval(ctx)));
    }
}
