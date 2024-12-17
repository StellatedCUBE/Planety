using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class Block : Node
    {
        public readonly List<Node> contents;

        public Block(List<Node> contents, (int, int) position)
        {
            this.contents = contents;
            this.position = contents.Count == 0 ? position : contents[contents.Count - 1].position;
        }

        public override object Eval(Context ctx)
        {
            ctx.currentCodePath = sourceFile;

            object ret = null;

            foreach (var node in contents)
                ret = node.Eval(ctx);

            return ret;
        }
    }
}
