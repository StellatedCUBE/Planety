using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class BiomesData : IEnumerable<BiomeData>
    {
        public Source<Texture2D> map = new SourceTexture2DValue<Color32>(new Color32(255, 0, 0, 0), TextureFormat.RGBA32);

        public BiomeData red = new BiomeData('R');
        public BiomeData green = new BiomeData('G');
        public BiomeData blue = new BiomeData('B');
        public BiomeData alpha = new BiomeData('A');

        public IEnumerator<BiomeData> GetEnumerator() => ((IEnumerable<BiomeData>)new[] { red, green, blue, alpha }).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
