using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class ListLit2 : Node
    {
        public List<Node> list, counts;

        public override object Eval(Context ctx)
        {
            List<object> output = new();
            for (int i = 0; i < list.Count; i++)
            {
                int q;
                object qo = counts[i].Eval(ctx);
                try
                {
                    q = Convert.ToInt32(qo);
                }
                catch
                {
                    throw new ScriptException($"Type {qo.GetType()} cannot be converted to an integer", counts[i]);
                }
                if (q < 0)
                    throw new ScriptException("List quantities cannot be negative", counts[i]);
                object x = list[i].Eval(ctx);
                for (int j = 0; j < q; j++)
                    output.Add(x);
            }
            return output;
        }
    }
}
