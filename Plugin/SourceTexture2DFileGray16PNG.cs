using LibPngDotNet;
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
    public class SourceTexture2DFileGray16PNG : Source<Texture2D>
    {
        readonly string path;

        public SourceTexture2DFileGray16PNG(string path)
        {
            this.path = path;
        }

        public Texture2D Get()
        {
            Plugin.ALLog("Reading height image " + Path.GetFileName(path));
            var decoder = PngDecoder.Open(path);

            if (decoder.Channels != 1 || decoder.BitDepth != 16)
                throw new PlanetyException($"\"{Plugin.DepictPath(path)}\" is not encoded with GRAY16");

            /*if (!Plugin.IsPowerOf2(decoder.Width) || !Plugin.IsPowerOf2(decoder.Height))
                Plugin.PopWarning(Plugin.DepictPath(path) + " does not have power of 2 dimensions");*/

            Plugin.ALLog("Parsing image");
            var pixels = decoder.ReadPixels<ushort>(PixelLayout.Gray16);

            Plugin.ALLog("Converting image");
            var flip = new ushort[pixels.Length];

            for (int y = 0; y < decoder.Height; y++)
            {
                for (int x = 0; x < decoder.Width; x++)
                {
                    var p = pixels[y * decoder.Width + x];
                    flip[(decoder.Height - y - 1) * decoder.Width + x] = (ushort)(((p & 255) << 8) | ((p & 65280) >> 8));
                }
            }

            pixels = flip;

            if (Plugin.IsMainThread())
            {
                Plugin.ALLog("Uploading image");
                Texture2D texture = new(decoder.Width, decoder.Height, TextureFormat.R16, false, true);
                texture.name = Plugin.DepictPath(path);
                texture.SetPixelData(pixels, 0);
                texture.Apply();
                return texture;
            }

            var messagePass = new BlockingCollection<Texture2D>(1);

            Plugin.RunOnMainThread(() =>
            {
                Plugin.ALLog("Uploading image");
                Texture2D texture = new(decoder.Width, decoder.Height, TextureFormat.R16, false, true);
                texture.name = Plugin.DepictPath(path);
                texture.SetPixelData(pixels, 0);
                texture.Apply();
                messagePass.Add(texture);
            });

            return messagePass.Take();
        }

        public bool DisposeCreatedResource() => true;

        public override int GetHashCode() => path.GetHashCode();
        public override bool Equals(object obj) => obj is SourceTexture2DFileGray16PNG other && path == other.path;
    }
}
