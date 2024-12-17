using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class Add : Node
    {
        public Node lhs, rhs;

        public override object Eval(Context ctx)
        {
            var left = lhs.Eval(ctx);
            var right = rhs.Eval(ctx);

            if (left is string || right is string)
                return left.ToString() + right.ToString();

            return Convert.ToDouble(left) + Convert.ToDouble(right);
        }
    }
}
