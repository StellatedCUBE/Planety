using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class RingParticle : Source<GameObject>
    {
        internal RingParticle() { }

        [NoScriptAccess]
        public bool isMesh;

        public int density;
        public int seed = UnityEngine.Random.Range(1, 1000000000);
        public float size = 1;
        public Vector2Int atlasSize = Vector2Int.one;
        public Source<Texture2D> texture;
        public Source<Mesh> mesh;

        public bool DisposeCreatedResource() => true;

        GameObject Make(Texture2D texture, Mesh mesh)
        {
            var go = GameObject.Instantiate(isMesh ? AssetLoader.ringRockParticles : AssetLoader.ringDustParticles);
            go.SetActive(false);

            var ps = go.GetComponent<PAParticleField>();
            ps.particleCount = density;
            ps.seed = seed;
            ps.particleSize *= size;

            if (texture)
            {
                ps.texture = texture;
                if (atlasSize == Vector2Int.one)
                    ps.textureType = PAParticleField.TextureType.Simple;
                else
                {
                    ps.textureType = PAParticleField.TextureType.SpriteGrid;
                    ps.spriteColumns = atlasSize.x;
                    ps.spriteRows = atlasSize.y;
                }
            }
            if (mesh && isMesh)
                go.GetComponent<MeshFilter>().mesh = mesh;

            return go;
        }

        public GameObject Get()
        {
            var t = texture?.Get();
            var m = mesh?.Get();

            if (Plugin.IsMainThread())
                return Make(t, m);

            BlockingCollection<GameObject> messagePass = new(1);

            Plugin.RunOnMainThread(() => messagePass.Add(Make(t, m)));

            return messagePass.Take();
        }
    }
}
