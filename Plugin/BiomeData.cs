using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class BiomeData : IEnumerable<SurfaceTextureLayer>
    {
        internal BiomeData(char c)
        {
            channel = c;
        }

        internal char channel;

        public Source<Texture2D> heightmap_1, heightmap_2;
        public double height_scale_1, height_scale_2;
        public int uv_scale_1, uv_scale_2;

        public SurfaceTextureLayer texture_1 = new SurfaceTextureLayer();
        public SurfaceTextureLayer texture_2 = new SurfaceTextureLayer();
        public SurfaceTextureLayer texture_3 = new SurfaceTextureLayer();
        public SurfaceTextureLayer texture_4 = new SurfaceTextureLayer();

        public IEnumerator<SurfaceTextureLayer> GetEnumerator() => ((IEnumerable<SurfaceTextureLayer>)new[] { texture_1, texture_2, texture_3, texture_4 }).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
