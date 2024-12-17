using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class SourceTexture2DFile : Source<Texture2D>
    {
        readonly string path;
        readonly bool value_map;

        public SourceTexture2DFile(string path, bool value_map = false)
        {
            this.path = path;
            this.value_map = value_map;
        }

        public Texture2D Get()
        {
            Plugin.ALLog("Reading color image " + Path.GetFileName(path));
            var data = File.ReadAllBytes(path);
            
            if (Plugin.IsMainThread())
            {
                Plugin.ALLog("Parsing image");
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                ImageConversion.LoadImage(texture, data);

                if (!Plugin.IsPowerOf2(texture.width) || !Plugin.IsPowerOf2(texture.height))
                    Plugin.PopWarning(Plugin.DepictPath(path) + " does not have power of 2 dimensions");

                if (value_map)
                {
                    Texture2D src = texture;
                    texture = new Texture2D(src.width, src.height, TextureFormat.R16, false, true);
                    var srca = src.GetRawTextureData<uint>();
                    var texa = src.GetRawTextureData<ushort>();

                    for (int i = 0; i < texa.Length; i++)
                    {
                        texa[i] = (ushort)((srca[i] >> 8) & 65535);
                    }

                    texture.Apply();
                    Component.Destroy(src);
                }

                return texture;
            }

            var message_pass = new BlockingCollection<Texture2D>(1);

            Plugin.RunOnMainThread(() =>
            {
                Plugin.ALLog("Parsing image");
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                ImageConversion.LoadImage(texture, data);

                if (!Plugin.IsPowerOf2(texture.width) || !Plugin.IsPowerOf2(texture.height))
                    Plugin.PopWarning(Plugin.DepictPath(path) + " does not have power of 2 dimensions");

                if (value_map)
                {
                    Texture2D src = texture;
                    texture = new Texture2D(src.width, src.height, TextureFormat.R16, false, true);
                    var srca = src.GetRawTextureData<uint>();
                    var texa = texture.GetRawTextureData<ushort>();

                    for (int i = 0; i < texa.Length; i++)
                    {
                        texa[i] = (ushort)((srca[i] >> 8) & 65535);
                    }

                    texture.Apply();
                    Component.Destroy(src);
                }

                message_pass.Add(texture);
            });

            return message_pass.Take();
        }

        public bool DisposeCreatedResource() => true;


        public override int GetHashCode() => path.GetHashCode();
        public override bool Equals(object obj) => obj is SourceTexture2DFile other && path == other.path;
    }
}
