using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class SourceTexture2DValue<T> : Source<Texture2D> where T : struct
    {
        T value;
        TextureFormat format;

        public SourceTexture2DValue(T value, TextureFormat format)
        {
            this.value = value;
            this.format = format;
        }

        public Texture2D Get()
        {
            if (Plugin.IsMainThread())
            {
                var tex = new Texture2D(1, 1, format, false, true);
                var arr = tex.GetPixelData<T>(0);
                arr[0] = value;
                tex.Apply();
                return tex;
            }

            var messagePass = new BlockingCollection<Texture2D>(1);

            Plugin.RunOnMainThread(() =>
            {
                var tex = new Texture2D(1, 1, format, false, true);
                var arr = tex.GetPixelData<T>(0);
                arr[0] = value;
                tex.Apply();
                messagePass.Add(tex);
            });

            return messagePass.Take();
        }

        public bool DisposeCreatedResource() => true;
    }
}
