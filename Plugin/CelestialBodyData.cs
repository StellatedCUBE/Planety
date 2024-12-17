using KSP.Sim.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Planety
{
    public class CelestialBodyData
    {
        [NoScriptAccess]
        public bool stock = false;

        [NoScriptAccess]
        public bool dirty = false;

        [NoScriptAccess]
        public bool removed = false;

        public bool doSanityChecks = true;

        internal double stockRadius;
        public double terrainScale = 1;

        public string id;
        public string displayName;
        public string description = "A custom body created with Planety.";
        public OrbitData orbit;
        public Vector3? galacticPosition;
        public Color? orbitColor;
        public Color? nodeColor;
        public bool extendStarLightToReach = true;

        [NoScriptAccess]
        public CelestialBodyType bodyType;

        [NoScriptAccess]
        public string keyPrefix;

        [NoScriptAccess]
        public double HeightScale { get => terrainData?.heightScale ?? starSurfaceData.heightScale; }

        public double? radius;
        public double? surfaceGravity;
        //[ScriptName("soi", true)]
        //public double SphereOfInfluence { get => orbit == null ? PhysicsSettings.METERS_PER_LIGHTYEAR : orbit.semiMajorAxis * Math.Pow(); }
        public Rotation rotation = Rotation.None();
        public Vector3d axialTilt;
        public double additionalTimewarpLockHeight = 3000;

        public double rotationOffset { get => rotation.offset; set => rotation.offset = value; }

        public LightingData lighting = new();
        public Light light;
        public SkyboxVisibilityData skyboxVisibility = new();
        public Color? reflectedColor;
        public Flare flare;

        public float deathZoneRadius;

        [NoScriptAccess]
        public TerrainData terrainData;
        [NoScriptAccess]
        public StarSurfaceData starSurfaceData;

        [ScriptName("terrain", true)]
        public object Surface
        {
            get => (object)terrainData ?? starSurfaceData;
            set
            {
                if (value is TerrainData td)
                    terrainData = td;
                else if (value is StarSurfaceData ssd)
                    starSurfaceData = ssd;
                else
                    throw new ArgumentException("Invalid type for surface data: " + value.GetType().Name);
            }
        }

        public List<Ring> rings = new();
        public List<object> scaledSpaceExtras = new(), simulationSpaceExtras = new();

        internal List<AK.Wwise.Event> _music;

        public List<AK.Wwise.Event> Music
        {
            set
            {
                _music = value;
                var list = Plugin.GetSOIAudioEvents();
                var entry = list.Find(e => string.Equals(e.Name, id, StringComparison.InvariantCultureIgnoreCase));
                if (entry == null)
                    list.Add(new() { Name = id, SearchMode = KSP.Audio.KSPAudioEventToStringPropertyWatcherBinding.StringToAudioEventListBinding.StringSearchMode.EXACTLY, TargetEvents = value });
                else
                    entry.TargetEvents = value;
            }
            get => _music;
        }

        public bool IsStar { get => bodyType == CelestialBodyType.Star; }
        public bool IsGasGiant { get => bodyType == CelestialBodyType.Gas; }
        public bool IsTerrestrial { get => bodyType == CelestialBodyType.Rock; }

        internal bool HasValidFieldSet(out HashSet<string> errors)
        {
            errors = new HashSet<string>();

            if (string.IsNullOrEmpty(id))
            {
                errors.Add("\"id\" is not set");
            }

            if (string.IsNullOrEmpty(displayName))
            {
                errors.Add("\"display_name\" is not set");
            }

            if (orbit != null)
            {
                orbit.HasValidFieldSet(out HashSet<string> orbit_errors);
                errors.UnionWith(orbit_errors);
            }

            if (!orbitColor.HasValue && !nodeColor.HasValue && orbit != null)
            {
                errors.Add("No color is set (Either \"orbit_color\" or \"node_color\")");
            }

            if (!radius.HasValue)
            {
                errors.Add("\"radius\" is not set");
            }

            if (!surfaceGravity.HasValue)
            {
                errors.Add("Neither \"surface_gravity\" nor \"mass\" is set");
            }

            foreach (var ring in rings)
            {
                if (!ring.innerRadius.HasValue)
                    errors.Add("Ring's inner_radius is not set");
                if (!ring.outerRadius.HasValue)
                    errors.Add("Ring's outer_radius is not set");
                if (!ring.Stock && ring.texture == null)
                    errors.Add("Ring's texture is not set");
            }

            return errors.Count > 0;
        }

        internal void FillOut()
        {
            if (string.IsNullOrEmpty(keyPrefix))
                keyPrefix = $"planety://body/{id}/";

            if (orbit == null)
                nodeColor = Color.white;

            orbitColor ??= nodeColor.Value;
            nodeColor ??= orbitColor.Value;

            if (deathZoneRadius < 0)
                deathZoneRadius = (float)radius * 3;

            if (stock && dirty && !keyPrefix.StartsWith("planety://"))
                keyPrefix = $"planety://bodymod/{keyPrefix}";

            UseCaches();
        }

        internal Dictionary<Source<Texture2D>, SourceDisposableObjectCache<Texture2D>> textureCacheSourceMap;
        void UseCaches()
        {
            textureCacheSourceMap = new();

            if (terrainData != null)
            {
                DisposableObjectGC.UseCache(ref terrainData.globalHeightmap, this);
                DisposableObjectGC.UseCache(ref terrainData.biomes.map, this);
                DisposableObjectGC.UseCache(ref terrainData.scaledSpaceTexture, this);

                foreach (var biome in terrainData.biomes)
                {
                    DisposableObjectGC.UseCache(ref biome.heightmap_1, this);
                    DisposableObjectGC.UseCache(ref biome.heightmap_2, this);

                    foreach (var layer in biome)
                    {
                        DisposableObjectGC.UseCache(ref layer.albedo, this);
                        DisposableObjectGC.UseCache(ref layer.normal, this);
                        DisposableObjectGC.UseCache(ref layer.metal, this);
                    }
                }
            }

            else if (starSurfaceData != null)
            {
                DisposableObjectGC.UseCache(ref starSurfaceData.flow.map, this);
                DisposableObjectGC.UseCache(ref starSurfaceData.texture, this);
                DisposableObjectGC.UseCache(ref starSurfaceData.heightmap, this);
            }

            foreach (var ring in rings)
                DisposableObjectGC.UseCache(ref ring.texture, this);

            textureCacheSourceMap = null;
        }

        internal void UpdateCoreData(KSP.Sim.Definitions.CelestialBodyData coreData)
        {
            coreData.radius = radius.Value;
            coreData.gravityASL = surfaceGravity.Value;
            coreData.isRotating = rotation.rotating;
            coreData.rotationPeriod = rotation.period;
            coreData.isTidallyLocked = rotation.tidallyLocked;
            coreData.initialRotation = rotation.offset;
            coreData.axialTilt = axialTilt;
            coreData.TimeWarpAltitudeOffset = additionalTimewarpLockHeight;
            coreData.SphereOfInfluenceCalculationType = orbit == null ? 1 : 0;
            coreData.ringGroupData.Clear();
            coreData.ringGroupData.AddRange(rings.Select(r => new CelestialBodyRingData { innerRadius = r.innerRadius.Value, outerRadius = r.outerRadius.Value }));
            if (light != null) coreData.StarLuminosity = light.power;
        }

        internal void UpdateLightingData(KSP.Rendering.CelestialBodyLightingData lightingData)
        {
            var color = light?.color ?? reflectedColor;
            if (color.HasValue)
            {
                lightingData.color = color.Value;
                lightingData.enabled = lightingData.color.grayscale > 0;
            }

            lighting.UpdateLightingData(lightingData);
            skyboxVisibility.UpdateLightingData(lightingData);
            light?.UpdateLightingData(lightingData);
        }

        public override string ToString() => id;

        public void Remove() => removed = true;


    }
}
