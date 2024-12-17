using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety.ModLoader
{
    public static class ScriptFunctions
    {
        public static CelestialBodyData GetBodyById(Context ctx, string id)
        {
            foreach (var body in ContentLoader.newBodies)
            {
                if (body.id == "Planety::" + id || body.id == $"Planety::{ctx.mod.id}::{id}")
                    return body;
            }

            if (id.StartsWith("Stock::"))
                id = id.Substring(7);

            ContentLoader.stockBodyMap.TryGetValue(id, out var body2);
            return body2;
        }

        public static CelestialBodyData CreateTerrestrialBody(Context ctx, string id)
        {
            if (!Parser.IsValidASCIIID(id))
                throw new ScriptException($"\"{id}\" is not a valid ID for a celestial body. If you want to use it as a name, pick a different ID then set the \"name\" property afterwards");

            var body = new CelestialBodyData
            {
                id = $"Planety::{ctx.mod.id}::{id}",
                displayName = id,
                terrainData = new(),
                bodyType = CelestialBodyType.Rock
            };

            ContentLoader.newBodies.Add(body);
            return body;
        }

        public static CelestialBodyData CreateStar(Context ctx, string id)
        {
            if (!Parser.IsValidASCIIID(id))
                throw new ScriptException($"\"{id}\" is not a valid ID for a celestial body. If you want to use it as a name, pick a different ID then set the \"name\" property afterwards");

            var body = new CelestialBodyData
            {
                id = $"Planety::{ctx.mod.id}::{id}",
                displayName = id,
                starSurfaceData = new(),
                light = new() { color = Color.white },
                flare = new(),
                Music = ContentLoader.starMusic,
                bodyType = CelestialBodyType.Star,
                deathZoneRadius = -1,
                extendStarLightToReach = false
            };

            body.light.falloffCurve = new(
                new(0, 1, -2.8018868f, -2.8018868f, 0, 0.0353535339f),
                new(1, 0, 0, 0, 0, 0)
            );

            ContentLoader.newBodies.Add(body);
            return body;
        }

        public static RingParticle CreateRingDust() => new() { density = 500 };

        public static RingParticle CreateRingRocks() => new() { density = 392, isMesh = true };

        public static ZeroAlphaPass SetAlphaToZero(Source<Texture2D> image) => new(image);

        public static SourceTexture2DFile ReadTextureFromPng(Context ctx, string path) => new(Path.Combine(Path.GetDirectoryName(ctx.currentCodePath), path));

        public static SourceTexture2DFile ReadTextureFromJpg(Context ctx, string path) => ReadTextureFromPng(ctx, path);

        public static SourceTexture2DDDS ReadTextureFromDds(Context ctx, string path) => new(Path.Combine(Path.GetDirectoryName(ctx.currentCodePath), path));

        public static SourceMeshBinFile ReadMesh(Context ctx, string path) => new(Path.Combine(Path.GetDirectoryName(ctx.currentCodePath), path));

        //public static SourceTexture2DFile ReadHeightmapFromPng(Context ctx, string path) => new SourceTexture2DFile(Path.Combine(Path.GetDirectoryName(ctx.currentCodePath), path), true);
        public static SourceTexture2DFileGray16PNG ReadHeightmapFromPng(Context ctx, string path) => new(Path.Combine(Path.GetDirectoryName(ctx.currentCodePath), path));

        public static SourceTexture2DRaw ReadHeightmapRaw(Context ctx, string path, int size) => new(Path.Combine(Path.GetDirectoryName(ctx.currentCodePath), path), size, size, TextureFormat.R16, false, true);
    }
}
