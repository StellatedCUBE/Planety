using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class ListLit : Node
    {
        public List<Node> list;

        public override object Eval(Context ctx) => list.Select(n => n.Eval(ctx)).ToList();
    }
}
