using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader
{
    public class ScriptException : PlanetyException
    {
        public ScriptException(string message) : base(message) { }
        public ScriptException(string message, AST.Node cause) : base(message)
        {
            if (cause != null)
            {
                sourceFile = cause.sourceFile;
                pos = cause.position;
            }
        }

        public string sourceFile;
        public (int, int) pos = (-1, -1);

        public override string ToString()
        {
            var b = base.ToString();

            if (sourceFile != null)
            {
                if (pos.Item1 < 0)
                    b = $"In \"{sourceFile}\"\n  {b}";
                else
                    b = $"In \"{sourceFile}\"\n at line {pos.Item1}, column {pos.Item2}\n  {b}";
            }

            return b;
        }
    }
}
