using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class MapLit : Node
    {
        public Dictionary<string, Node> map;

        public override object Eval(Context ctx) => map.ToDictionary(p => p.Key, p => p.Value.Eval(ctx));
    }
}
