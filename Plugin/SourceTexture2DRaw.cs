using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class SourceTexture2DRaw : Source<Texture2D>
    {
        readonly string path;
        readonly int width, height, bytesPerPixel;
        readonly bool mipmaps, linear;
        readonly TextureFormat format;

        public SourceTexture2DRaw(string path, int width, int height, TextureFormat format, bool mipmaps, bool linear)
        {
            this.path = path;
            this.width = width;
            this.height = height;
            this.format = format;
            this.mipmaps = mipmaps;
            this.linear = linear;
            switch (format)
            {
                case TextureFormat.R16:
                    bytesPerPixel = 2;
                    break;

                case TextureFormat.RGB24:
                    bytesPerPixel = 3;
                    break;

                case TextureFormat.ARGB32:
                case TextureFormat.RGBA32:
                    bytesPerPixel = 4;
                    break;

                default:
                    throw new ArgumentException("Unsupported texture format " + format.ToString(), "format");
            }
        }

        public Texture2D Get()
        {
            Plugin.ALLog("Reading raw image " + Path.GetFileName(path));
            var data = File.ReadAllBytes(path);
            int size = width * height * bytesPerPixel;

            if (data.Length != size)
                throw new IOException($"File {path} had unexpected size {data.Length} (was expecting {size})");

            if (Plugin.IsMainThread())
            {
                Plugin.ALLog("Uploading image");
                Texture2D texture = new(width, height, format, mipmaps, linear);
                texture.SetPixelData(data, 0);
                texture.Apply(mipmaps);
                return texture;
            }

            var messagePass = new BlockingCollection<Texture2D>(1);

            Plugin.RunOnMainThread(() =>
            {
                Plugin.ALLog("Uploading image");
                Texture2D texture = new(width, height, format, mipmaps, linear);
                texture.SetPixelData(data, 0);
                texture.Apply(mipmaps);
                messagePass.Add(texture);
            });

            return messagePass.Take();
        }

        public bool DisposeCreatedResource() => true;

        public override int GetHashCode() => path.GetHashCode() + width + height;
        public override bool Equals(object obj) => obj is SourceTexture2DRaw other && path == other.path && width == other.width;
    }
}
