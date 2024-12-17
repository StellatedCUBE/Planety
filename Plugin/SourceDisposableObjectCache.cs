using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Planety
{
    public class SourceDisposableObjectCache<T> : Source<T> where T : UnityEngine.Object
    {
        enum State { NoValue, GeneratingValue, HasValue }

        readonly Source<T> inner;
        readonly string body;
        T cache = null;
        State state = State.NoValue;

        public SourceDisposableObjectCache(Source<T> inner, string bodyId)
        {
            this.inner = inner;
            body = bodyId;
            DisposableObjectGC.OnFree(bodyId, Clear);
        }

        public T Get()
        {
            switch (state)
            {
                case State.NoValue:
                    state = State.GeneratingValue;
                    cache = inner.Get();
                    state = State.HasValue;
                    if (inner.DisposeCreatedResource())
                        DisposableObjectGC.RegisterObject(body, cache);
                    return cache;

                case State.GeneratingValue:
                    while (state == State.GeneratingValue)
                        Thread.Sleep(10);
                    return Get();

                default:
                    return cache;
            }
        }

        public bool DisposeCreatedResource() => false;

        internal void Clear()
        {
            cache = null;
            if (state == State.HasValue)
                state = State.NoValue;
        }

        public override int GetHashCode() => inner.GetHashCode();
        public override bool Equals(object obj) => obj is SourceDisposableObjectCache<T> other && inner.Equals(other.inner);
    }
}
