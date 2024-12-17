using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class StarSurfaceData
    {
        public class Flow
        {
            public Source<Texture2D> map;
            public float speed = 0.01f;
            public float strength = 0.05f;
        }

        public class Fresnel
        {
            public float bias = 0.05f;
            public float scale = 1.25f;
            public float power = 8;
            public Color color = new(4.237095f, 2.994805f, 0.75425f);
        }

        public Flow flow = new();
        public Fresnel fresnel = new();
        public Source<Cubemap> texture;
        [ScriptName("global_heightmap", true)]
        public Source<Texture2D> heightmap;
        public double heightScale;
        public Color emissionColorNear = new(1.498039f, 0.8862745f, 0.4f);
        public Color emissionColorFar = new(4.402531f, 3.203936f, 1.290794f);
    }
}
