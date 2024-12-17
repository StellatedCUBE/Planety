using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class SourceCubemapFromFaces : Source<Cubemap>
    {
        readonly Dictionary<Source<Texture2D>, bool[]> faceMap = new();

        public SourceCubemapFromFaces(Source<Texture2D>[] faces)
        {
            if (faces.Length != 6)
                throw new ArgumentException("Cubes must have six faces", "faces");
            
            for (int i = 0; i < 6; i++)
            {
                if (!faceMap.TryGetValue(faces[i], out var ba))
                    faceMap.Add(faces[i], ba = new bool[6]);
                ba[i] = true;
            }
        }

        public bool DisposeCreatedResource() => true;

        Cubemap Make(Texture2D[] a)
        {
            int w = a[0].width;
            var f = a[0].format;
            Cubemap cm = new(w, f, false);
            for (int i = 0; i < 6; i++)
            {
                if (a[i].format != f)
                    throw new FormatException("Cubemap faces must be the same format");
                if (a[i].width != w || a[i].height != w)
                    throw new FormatException("Cubemap faces must be the same size, and squares");
                cm.SetPixelData(a[i].GetPixelData<byte>(0), 0, (CubemapFace)i);
            }
            cm.Apply();
            return cm;
        }

        public Cubemap Get()
        {
            List<Texture2D> destroy = new();
            var faces = new Texture2D[6];

            foreach (var p in faceMap)
            {
                var t = p.Key.Get();
                if (p.Key.DisposeCreatedResource())
                    destroy.Add(t);
                for (int i = 0; i < 6; i++)
                    if (p.Value[i])
                        faces[i] = t;
            }

            if (Plugin.IsMainThread())
            {
                var cube = Make(faces);
                foreach (var td in destroy)
                    Texture2D.Destroy(td);
                return cube;
            }

            var messagePass = new BlockingCollection<Cubemap>(1);

            Plugin.RunOnMainThread(() =>
            {
                var cube = Make(faces);
                foreach (var td in destroy)
                    Texture2D.Destroy(td);
                messagePass.Add(cube);
            });

            return messagePass.Take();
        }
    }
}
