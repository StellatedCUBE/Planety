using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class ZeroAlphaPass : Source<Texture2D>
    {
        Source<Texture2D> inner;

        public ZeroAlphaPass(Source<Texture2D> @in)
        {
            inner = @in;
        }

        public Texture2D Get()
        {
            var inner = this.inner.Get();

            if (Plugin.IsMainThread())
            {
                Texture2D @out = new(inner.width, inner.height, inner.format, inner.mipmapCount, false);

                for (int i = 0; i < @out.mipmapCount; i++)
                {
                    var data = inner.GetPixels32(i);
                    for (int j = 0; j < data.Length; j++)
                        data[j].a = 0;
                    @out.SetPixels32(data, i);
                }

                @out.Apply();
                return @out;
            }

            var message_pass = new BlockingCollection<Texture2D>(1);

            Plugin.RunOnMainThread(() =>
            {
                Texture2D @out = new(inner.width, inner.height, TextureFormat.RGBA32, inner.mipmapCount, false);

                for (int i = 0; i < @out.mipmapCount; i++)
                {
                    var data = inner.GetPixels32(i);
                    for (int j = 0; j < data.Length; j++)
                        data[j].a = 0;
                    @out.SetPixels32(data, i);
                }

                @out.Apply();
                message_pass.Add(@out);
            });

            return message_pass.Take();
        }

        public bool DisposeCreatedResource() => inner.DisposeCreatedResource();

        public override int GetHashCode() => inner.GetHashCode();
        public override bool Equals(object obj) => obj is ZeroAlphaPass other && inner.Equals(other.inner);
    }
}
