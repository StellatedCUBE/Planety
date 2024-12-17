using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public abstract class Node
    {
        public readonly string sourceFile = Content.currentFileBeingParsed;
        public (int, int) position = (-1, -1);

        public abstract object Eval(Context ctx);
        public virtual void MarkAsAssignLeft() { }
    }
}
