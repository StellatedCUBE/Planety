using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class TerrainData
    {
        public Source<Texture2D> globalHeightmap;
        public double heightScale;

        public Source<Texture2D> scaledSpaceTexture;
        public bool antiTile = true;

        public int maximumSubdivisions = 17;

        public BiomesData biomes = new();

        public ResampleData resample_1 = new() { distance = 200, uvScale = 0.1f };
        public ResampleData resample_2 = new() { distance = 1000 };
        public ResampleData resample_3 = new() { distance = 3000 };
        public ResampleData resample_4 = new() { distance = 10000 };
    }
}
