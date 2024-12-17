using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    public interface Source<T>
    {
        public T Get();

        public bool DisposeCreatedResource();
    }

}
