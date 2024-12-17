using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class ScriptNameAttribute : Attribute
    {
        public string name;
        public bool keepExistingName;

        public ScriptNameAttribute(string name, bool keepExistingName = false)
        {
            this.name = name;
            this.keepExistingName = keepExistingName;
        }
    }
}
