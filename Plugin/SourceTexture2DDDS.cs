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
    public class SourceTexture2DDDS : Source<Texture2D>
    {
        readonly string path;

        public SourceTexture2DDDS(string path) => this.path = path;

        public Texture2D Get()
        {
            Plugin.ALLog("Reading color image " + Path.GetFileName(path));
            var data = File.ReadAllBytes(path);

            if (data[4] != 124)
                throw new FormatException($"\"{Plugin.DepictPath(path)}\" is not a valid DDS file");

            int width = (data[17] << 8) + data[16];
            int height = (data[13] << 8) + data[12];
            var format = data[87] == (byte)'1' ? TextureFormat.DXT1 : TextureFormat.DXT5;

            if (!Plugin.IsPowerOf2(width) || !Plugin.IsPowerOf2(height))
                Plugin.PopWarning(Plugin.DepictPath(path) + " does not have power of 2 dimensions");

            if (Plugin.IsMainThread())
            {
                Texture2D texture = new(width, height, format, false);
                texture.SetPixelData(data, 0, 128);
                texture.Apply();
                return texture;
            }

            BlockingCollection<Texture2D> messagePass = new();

            Plugin.RunOnMainThread(() =>
            {
                Texture2D texture = new(width, height, format, false);
                texture.SetPixelData(data, 0, 128);
                texture.Apply();
                messagePass.Add(texture);
            });

            return messagePass.Take();
        }

        public bool DisposeCreatedResource() => true;

        public override int GetHashCode() => path.GetHashCode();
        public override bool Equals(object obj) => obj is SourceTexture2DDDS other && path == other.path;
    }
}
