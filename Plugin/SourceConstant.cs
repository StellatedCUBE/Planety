using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    public class SourceConstant<T> : Source<T>
    {
        T value;

        public SourceConstant(T value)
        {
            this.value = value;
        }

        public T Get() => value;

        public bool DisposeCreatedResource() => false;

        public override int GetHashCode() => value.GetHashCode();
        public override bool Equals(object obj) => obj is SourceConstant<T> other && value.Equals(other.value);
    }
}
