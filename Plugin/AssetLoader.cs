using BepInEx.Logging;
using HarmonyLib;
using KSP;
using KSP.Assets;
using KSP.Game;
using KSP.Rendering;
using KSP.Rendering.Planets;
using KSP.Sim.Converters;
using Shinten;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uber.Scatter;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Planety
{
    public static class AssetLoader
    {
        internal static Material gpqssm;

        static int[] normalized_PQS_scaled_space_sphere_indices;
        static Vector3[] normalized_PQS_scaled_space_sphere_normals;
        static Vector3[] normalized_PQS_scaled_space_sphere_positions;
        static Vector2[] normalized_PQS_scaled_space_sphere_uvs;

        internal static void Load()
        {
            Mesh normalized_PQS_scaled_space_sphere = BinMeshImpExp.FromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NormalizedPQSScaledSpaceSphere.dat"));
            normalized_PQS_scaled_space_sphere_indices = normalized_PQS_scaled_space_sphere.triangles;
            normalized_PQS_scaled_space_sphere_normals = normalized_PQS_scaled_space_sphere.normals;
            normalized_PQS_scaled_space_sphere_positions = normalized_PQS_scaled_space_sphere.vertices;
            normalized_PQS_scaled_space_sphere_uvs = normalized_PQS_scaled_space_sphere.uv;
        }

        public static bool CreateAsyncPrefix(AssetProvider provider, string key, Vector3 position, Quaternion rotation, Transform parent, object resultCallback)
        {
            if (key != null && key.StartsWith("planety://"))
            {
                Plugin.Log(LogLevel.Info, $"Matched request for asset {key} of type {resultCallback.GetType().GetGenericArguments()[0].FullName}");

                if (key.StartsWith("planety://body/"))
                {
                    var id = key.Substring(15).Split('/')[0];
                    var body = ContentLoader.GetBody(id);

                    if (key.EndsWith(".Scaled.prefab"))
                    {
                        if (body.bodyType == CelestialBodyType.Star)
                            LoadStarAsync(body, position, rotation, parent, (Action<GameObject>)resultCallback, provider);
                        else
                            LoadScaledAsync(body, position, rotation, parent, (Action<GameObject>)resultCallback);
                    }

                    if (key.EndsWith(".Simulation.prefab"))
                    {
                        LoadSimulationAsync(body, position, rotation, parent, (Action<GameObject>)resultCallback);
                    }
                }

                else if (key.StartsWith("planety://bodymod/"))
                {
                    var cb = (Action<GameObject>)resultCallback;
                    provider.CreateAsync<GameObject>(key.Substring(18), position, rotation, parent, go =>
                    {
                        var split = key.Split('.');
                        var customData = ContentLoader.GetBody(split[1]);
                        bool scaled = split[2] == "Scaled";

                        var ccbd = go.GetComponent<CoreCelestialBodyData>();
                        if (ccbd)
                            customData.UpdateCoreData(ccbd.Data);

                        var lighting = go.GetComponent<CelestialBodyLighting>();
                        if (lighting)
                            customData.UpdateLightingData(lighting.Data);

                        var rings = go.GetComponent<CelestialBodyRingGroup>();
                        if (rings)
                            rings.UpdateRingsInGroup();

                        var pqs = go.GetComponent<PQS>();
                        if (pqs && customData.terrainScale != 1f)
                        {
                            pqs.data.heightMapInfo.heightMapScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.largeA.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.largeR.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.largeG.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.largeB.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.mediumA.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.mediumR.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.mediumG.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.mediumB.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.subzone3A.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.subzone3R.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.subzone3G.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.subzone3B.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.subzone4A.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.subzone4R.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.subzone4G.heightScale *= (float)customData.terrainScale;
                            pqs.data.heightMapInfo.subzone4B.heightScale *= (float)customData.terrainScale;

                            foreach (PQSDecalInstance decal in go.GetComponentsInChildren<PQSDecalInstance>())
                            {
                                decal.HeightScale = (decal.OverrideHeightScale ? decal.HeightScale : decal.PQSDecal.HeightScale) * (float)customData.terrainScale;
                                decal.OverrideHeightScale = true;
                                decal.HeightOffset = (decal.OverrideHeightOffset ? decal.HeightOffset : decal.PQSDecal.HeightOffset) * (float)customData.terrainScale;
                                decal.OverrideHeightOffset = true;
                                decal.DecalUpdated();
                            }

                            var decal_ctrl = go.GetComponent<PQSDecalController>();
                            if (decal_ctrl)
                            {
                                decal_ctrl.RefreshDecalInstances();
                            }
                        }

                        var filter = go.GetComponent<MeshFilter>();
                        if (scaled && Math.Abs(customData.radius.Value / customData.stockRadius - customData.terrainScale) > 1e-3 && filter)
                        {
                            //Plugin.Log(LogLevel.Info, "Scaling " + customData.id);
                            Task.Run(() =>
                            {
                                var cache_path = Path.Combine(Plugin.cacheFolder, $"Stock\\scaled-{customData.id}.mesh");
                                Mesh scaled_mesh;

                                if (File.Exists(cache_path))
                                {
                                    scaled_mesh = new SourceMeshBinFile(cache_path).Get();
                                }
                                else
                                {
                                    var asset_path = new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, @"aa\StandaloneWindows64")).EnumerateFiles($"celestialbody-scaled-{customData.id}_assets_all_*.bundle").First().FullName;
                                    AssetStudio.AssetsManager manager = new();
                                    manager.LoadFiles(asset_path);
                                    AssetStudio.Mesh as_mesh = null;
                                    foreach (AssetStudio.SerializedFile file in manager.assetsFileList)
                                    {
                                        foreach (AssetStudio.Object obj in file.Objects)
                                        {
                                            if (obj is AssetStudio.Mesh mesh && mesh.m_Name == filter.sharedMesh.name)
                                            {
                                                as_mesh = mesh;
                                                break;
                                            }
                                        }
                                    }

                                    var message_pass = new BlockingCollection<Mesh>(1);

                                    ASConverter.ConvertMesh(as_mesh, mesh =>
                                    {
                                        try
                                        {
                                            BinMeshImpExp.ToFile(mesh, cache_path);
                                        }
                                        catch (Exception e) { Plugin.Log(LogLevel.Error, e.ToString()); }

                                        message_pass.Add(mesh);
                                    });

                                    scaled_mesh = message_pass.Take();
                                }

                                var message_pass2 = new BlockingCollection<Vector3[]>(1);

                                Plugin.RunOnMainThread(() => message_pass2.Add(scaled_mesh.vertices));

                                var positions = message_pass2.Take();

                                float scale = (float)(customData.terrainScale * customData.stockRadius / customData.radius.Value);

                                for (int i = 0; i < positions.Length; i++)
                                {
                                    float mag = positions[i].magnitude;
                                    mag -= 1000;
                                    mag *= scale;
                                    mag += 1000;
                                    positions[i] = positions[i].normalized * mag;
                                }

                                Plugin.RunOnMainThread(() =>
                                {
                                    scaled_mesh.vertices = positions;
                                    scaled_mesh.RecalculateBounds();
                                    scaled_mesh.RecalculateNormals();
                                    filter.mesh = scaled_mesh;
                                    DoRings(customData, go, cb);
                                });
                            });
                        }
                        else if (scaled)
                            DoRings(customData, go, cb);
                        else
                            cb(go);
                    });
                }

                else if (key.StartsWith("planety://log/") && resultCallback.GetType().GetGenericArguments()[0] == typeof(GameObject))
                {
                    var cb = (Action<GameObject>)resultCallback;
                    provider.CreateAsync<GameObject>(key.Substring(14), position, rotation, parent, go =>
                    {
                        Plugin.Log(LogLevel.Info, $"Logging object loaded from key {key.Substring(14)}:\n{UnityObjectPrinter.printer.StringifyGameObject(go)}");
                        cb(go);
                    });
                }

                return false;
            }

            return true;
        }

        private static void CreateScaledMesh(Texture2D heightmap, CelestialBodyData body, Action<Mesh> callback)
        {
            Log(body, "Scaled: Creating mesh data");

            var heightmap_data = heightmap.GetRawTextureData<ushort>();
            var positions = normalized_PQS_scaled_space_sphere_positions.ToArray();

            for (int i = 0; i < positions.Length; i++)
            {
                var uv = PQSJobUtil.GetVertexSphericalUVs(positions[i]);
                var sample = PQSJobUtil.BiquadraticSample(uv, heightmap_data, heightmap.width, heightmap.height);
                positions[i] *= 1000f + (float)body.HeightScale * sample / (float)body.radius.Value * 1000f;
            }

            Plugin.RunOnMainThread(() =>
            {
                Log(body, "Scaled: Creating mesh");
                var mesh = new Mesh();
                mesh.vertices = positions;
                mesh.normals = normalized_PQS_scaled_space_sphere_normals;
                mesh.uv = normalized_PQS_scaled_space_sphere_uvs;
                mesh.triangles = normalized_PQS_scaled_space_sphere_indices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                callback(mesh);
            });
        }

        static Action LogErrors(Action action) => () =>
        {
            try { action(); }
            catch (Exception e)
            {
                Plugin.PopError(e);
                throw e;
            }
        };

        static void Log(CelestialBodyData cb, string message) => Plugin.ALLog($"{cb.displayName}: {message}");

        static void AssertSquare(Texture2D tex)
        {
            if (tex != null && (tex.width != tex.height || !Plugin.IsPowerOf2(tex.width) || !Plugin.IsPowerOf2(tex.height)))
                throw new PlanetyException($"\"{tex.name}\" is not a power-of-two-sized square");
        }

        private static void LoadStarAsync(CelestialBodyData body, Vector3 position, Quaternion rotation, Transform parent, Action<GameObject> resultCallback, AssetProvider assets)
        {
            DisposableObjectGC.Lock(body);
            DisposableObjectGC.FreeUnused();
            float startTime = Time.unscaledTime;
            Log(body, "Scaled: Loading Kerbol");
            assets.Load<GameObject>("Celestial.Kerbol.Scaled.prefab", kerbol =>
            {
                /*
                kerbol.GetComponent<CoreCelestialBodyData>().Data = HarmonyPatch_LoadCelestialBodyDataFilesFlowAction_OnGalaxyDefinitionLoaded.ToCelestialBodyCore(body).data;
                CelestialBodyLightingData lighting = new();
                TypeConverters.CopyFieldsFromFields(ref lighting, kerbol.GetComponent<CelestialBodyLighting>().Data);
                body.UpdateLightingData(lighting);
                kerbol.GetComponent<CelestialBodyLighting>().Data = lighting;
                kerbol.GetComponent<CelestialBodyLighting>().RefreshDictionary();
                kerbol.AddComponent<DisposableObjectGC.BodyLock>().bodyId = body.id;
                */

                Log(body, "Scaled: Creating GameObject");
                var go = new GameObject();
                go.layer = 10;
                go.transform.parent = parent;
                go.transform.position = position;
                go.transform.rotation = rotation;
                go.AddComponent<CoreCelestialBodyData>().Data = HarmonyPatch_LoadCelestialBodyDataFilesFlowAction_OnGalaxyDefinitionLoaded.ToCelestialBodyCore(body).data;

                CelestialBodyLightingData lighting = new();
                TypeConverters.CopyFieldsFromFields(ref lighting, kerbol.GetComponent<CelestialBodyLighting>().Data);
                lighting.lightingOverrides = new();
                lighting.sphericalGaussianSettings = new();
                body.UpdateLightingData(lighting);
                go.AddComponent<CelestialBodyLighting>().Data = lighting;
                go.GetComponent<CelestialBodyLighting>().RefreshDictionary();
                go.AddComponent<SphereCollider>().radius = 1000f;
                go.AddComponent<DisposableObjectGC.BodyLock>().bodyId = body.id;
                //var pp_data = ScriptableObject.CreateInstance<PostProcessData>();
                //pp_data.profile = ScriptableObject.CreateInstance<PostProcessProfile>();
                go.AddComponent<CelestialBodyPostProcess>().Data = kerbol.GetComponent<CelestialBodyPostProcess>().Data;

                Texture2D flow = null;
                Cubemap texture = null;
                Mesh mesh = null;

                Action onceAssetsLoaded = () =>
                {
                    if (mesh == null)
                        go.AddComponent<MeshFilter>().sharedMesh = kerbol.GetComponent<MeshFilter>().sharedMesh;
                    else
                        go.AddComponent<MeshFilter>().mesh = mesh;

                    Log(body, "Scaled: Writing material properties");
                    var mat = new Material(kerbol.GetComponent<MeshRenderer>().sharedMaterial);
                    if (flow != null) mat.SetTexture("_FlowMap", flow);
                    if (texture != null) mat.SetTexture("_EmissionCube", texture);
                    mat.SetFloat("_FlowSpeed", body.starSurfaceData.flow.speed);
                    mat.SetFloat("_FlowStrength", body.starSurfaceData.flow.strength);
                    mat.SetFloat("_FresnelBias", body.starSurfaceData.fresnel.bias);
                    mat.SetFloat("_FresnelScale", body.starSurfaceData.fresnel.scale);
                    mat.SetFloat("_FresnelPower", body.starSurfaceData.fresnel.power);
                    mat.SetColor("_FresnelColor", body.starSurfaceData.fresnel.color);
                    mat.SetColor("_EmissionColorNear", body.starSurfaceData.emissionColorNear);
                    mat.SetColor("_EmissionColorFar", body.starSurfaceData.emissionColorFar);

                    if (body.flare != null)
                    {
                        var flare = GameObject.Instantiate(kerbol.transform.GetChild(0).gameObject, go.transform).transform;
                        flare.localPosition = Vector3.zero;
                        flare.localRotation = Quaternion.identity;
                        flare.localScale = Vector3.one;

                        var fc = go.AddComponent<CelestialBodyLensFlare>();
                        fc.flare = flare.GetComponent<ProFlare>();
                        fc.flareFalloffCurve = body.flare.falloffCurve ?? kerbol.GetComponent<CelestialBodyLensFlare>().flareFalloffCurve;
                        fc.flightAlphaMaxRange = body.flare.flight.alphaMaxRange;
                        fc.flightAlphaMinRange = body.flare.flight.alphaMinRange;
                        fc.flightFalloffDistance = body.flare.flight.falloffDistance ?? body.radius.Value * 573.394495412844;
                        fc.flightMaxScale = body.flare.flight.maxScale;
                        fc.flightMinScale = body.flare.flight.minScale;
                        fc.emissionNearRange = body.flare.nearRange;
                        fc.emissionFarRange = body.flare.farRange;
                        fc.mapFalloffDistance = body.flare.map.falloffDistance ?? body.radius.Value * 57.3394495412844;
                    }

                    go.AddComponent<MeshRenderer>().material = mat;

                    DoRings(body, go, resultCallback);

                    Log(body, $"Scaled: Complete in {Time.unscaledTime - startTime}s");
                    DisposableObjectGC.Release(body);
                };

                if (body.starSurfaceData.flow.map == null && body.starSurfaceData.heightmap == null && body.starSurfaceData.texture == null)
                    onceAssetsLoaded();
                else Task.Run(LogErrors(() =>
                {
                    Log(body, "Scaled: Maybe reading flow map");
                    flow = body.starSurfaceData.flow.map?.Get();
                    Log(body, "Scaled: Maybe reading surface texture");
                    texture = body.starSurfaceData.texture?.Get();
                    if (body.starSurfaceData.heightmap == null)
                        Plugin.RunOnMainThread(onceAssetsLoaded);
                    else
                    {
                        Log(body, "Scaled: Creating Mesh");
                        CreateScaledMesh(body.starSurfaceData.heightmap.Get(), body, m =>
                        {
                            mesh = m;
                            onceAssetsLoaded();
                        });
                    }
                }));
            });
        }

        private static void LoadScaledAsync(CelestialBodyData body, Vector3 position, Quaternion rotation, Transform parent, Action<GameObject> resultCallback)
        {
            DisposableObjectGC.Lock(body);
            DisposableObjectGC.FreeUnused();
            float startTime = Time.unscaledTime;
            Task.Run(LogErrors(() =>
            {
                Log(body, "Scaled: Loading heightmap");
                var heightmap = body.terrainData.globalHeightmap.Get();

                if (body.bodyType == CelestialBodyType.Rock)
                {
                    try
                    {
                        AssertSquare(heightmap);
                    }
                    catch (PlanetyException e)
                    {
                        Plugin.PopError(e);
                    }
                }

                Log(body, "Scaled: Loading texture");
                var scaledTexture = body.terrainData.scaledSpaceTexture.Get();

                Log(body, "Scaled: Creating core data");
                var coreData = HarmonyPatch_LoadCelestialBodyDataFilesFlowAction_OnGalaxyDefinitionLoaded.ToCelestialBodyCore(body).data;

                Log(body, "Scaled: Creating light data");
                CelestialBodyLightingData lighting = new()
                {
                    lightingOverrides = new(),
                    intensityAtApoapsis = 0.5f,
                    intensityAtPeriapsis = 0.5f,
                    sphericalGaussianSettings = new(),
                    dayVisibility = 1,
                    ambientDay = new(0.125f, 0.125f, 0.125f, 0),
                    ambientNight = new(0.04f, 0.04f, 0.04f, 0)
                };
                /*lighting.ambientDay = new Color(0.05f, 0.05f, 0.05f, 0);
                lighting.ambientNight = new Color(0.01f, 0.01f, 0.01f, 0);
                lighting.useAmbient = true;*/
                body.UpdateLightingData(lighting);
                
                CreateScaledMesh(heightmap, body, mesh =>
                {
                    Log(body, "Scaled: Creating material");
                    var material = new Material(Shader.Find("KSP2/Environment/CelestialBody/CelestialBody_Scaled"));
                    material.mainTexture = scaledTexture;

                    Log(body, "Scaled: Creating GameObject");
                    var go = new GameObject();
                    go.layer = 10;
                    go.transform.parent = parent;
                    go.transform.position = position;
                    go.transform.rotation = rotation;
                    go.AddComponent<CoreCelestialBodyData>().Data = coreData;

                    Log(body, "Scaled: Creating Components");
                    go.AddComponent<CelestialBodyLighting>().Data = lighting;
                    go.GetComponent<CelestialBodyLighting>().RefreshDictionary();
                    go.AddComponent<MeshFilter>().mesh = mesh;
                    go.AddComponent<MeshRenderer>().material = material;
                    go.AddComponent<SphereCollider>().radius = 1000f;
                    go.AddComponent<DisposableObjectGC.BodyLock>().bodyId = body.id;

                    var pp_data = ScriptableObject.CreateInstance<PostProcessData>();
                    pp_data.profile = ScriptableObject.CreateInstance<PostProcessProfile>();
                    go.AddComponent<CelestialBodyPostProcess>().Data = pp_data;

                    DoRings(body, go, resultCallback);

                    Log(body, $"Scaled: Complete in {Time.unscaledTime - startTime}s");
                    DisposableObjectGC.Release(body);
                });
            }));
        }

        static Shader ringShader;
        internal static GameObject ringDustParticles, ringRockParticles;

        static void LoadRingDataFromDres(Action callback)
        {
            GameManager.Instance.Assets.Load<GameObject>("Celestial.Dres.Scaled.prefab", dres =>
            {
                var ring = dres.GetComponentInChildren<CelestialBodyRing>();
                ringShader = ring.ringShader;
                ringDustParticles = ring.GetParticleFields()[0];
                ringRockParticles = ring.GetParticleFields()[1];
                callback();
            });
        }

        private static void DoRings(CelestialBodyData body, GameObject go, Action<GameObject> callback)
        {
            var rg = go.GetComponent<CelestialBodyRingGroup>();
            if (rg == null) 
            {
                if (body.rings.Count == 0)
                {
                    callback(go);
                    return;
                }
                Log(body, "Rings: Creating component");
                rg = go.AddComponent<CelestialBodyRingGroup>();
            }

            var rl = rg.GetRings();

            Log(body, "Rings: Updating existing rings");
            if (rl.Count > 0)
            {
                var seen = new bool[rl.Count];

                foreach (var r in body.rings)
                {
                    if (r.Stock)
                    {
                        seen[r.stockIndex] = true;
                        var ro = rl[r.stockIndex];
                        ro.scatteringMie = r.mie;
                        ro.scatteringStrength = r.scatteringStrength;
                        ro.alphaEdge = r.edgeFade;
                    }
                }

                for (int i = seen.Length - 1; i >= 0; i--)
                    if (!seen[i])
                        rg.RemoveGroupAtIndex(i);
            }

            if (body.rings.Count(r => r.texture != null) == 0)
            {
                Log(body, "Rings: Updating materials");
                rg.UpdateAllMaterials();
                callback(go);
                return;
            }

            Task.Run(LogErrors(() =>
            {
                Log(body, "Rings: Loading textures");
                var rts = body.rings.Select(r =>
                {
                    if (r.texture == null)
                        return null;
                    var t = r.texture.Get();
                    if (r.texture.DisposeCreatedResource())
                        DisposableObjectGC.RegisterObject(body, t);
                    return t;
                }).ToList();

                Action next = () =>
                {
                    Log(body, "Rings: Loading particles");
                    var pss = body.rings.Select(r => r.particles.Select(p =>
                    {
                        var g = p.Get();
                        if (p.DisposeCreatedResource())
                            DisposableObjectGC.RegisterObject(body, g);
                        return g;
                    }).ToList()).ToList();

                    Log(body, "Rings: Creating new rings");

                    var particleFields = typeof(CelestialBodyRing).GetField("_particleFields", BindingFlags.Instance | BindingFlags.NonPublic);

                    for (int i = 0; i < body.rings.Count; i++)
                    {
                        var r = body.rings[i];

                        if (r.Stock && rts[i] != null)
                            rl[i].ringTexture = rts[i];

                        else if (!r.Stock)
                        {
                            if (rl.Count == i)
                                rg.AddNewRing(i);
                            var ro = rl[i];
                            ro.ringTexture = rts[i];
                            ro.scatteringMie = r.mie;
                            ro.scatteringStrength = r.scatteringStrength;
                            ro.alphaEdge = r.edgeFade;
                            ro.ringShader = ringShader;
                            particleFields.SetValue(ro, pss[i]);
                        }
                    }

                    Log(body, "Rings: Updating materials");
                    rg.UpdateAllMaterials();
                    callback(go);
                };

                if (ringShader)
                    Plugin.RunOnMainThread(next);
                else
                    Plugin.RunOnMainThread(() => LoadRingDataFromDres(next));
            }));
        }

        private static void LoadSimulationAsync(CelestialBodyData body, Vector3 position, Quaternion rotation, Transform parent, Action<GameObject> resultCallback)
        {
            if (body.bodyType == CelestialBodyType.Star)
            {
                Plugin.Log(LogLevel.Warning, "Attempt to load simulation space star");
                return;
            }

            DisposableObjectGC.Lock(body);
            DisposableObjectGC.FreeUnused();
            float startTime = Time.unscaledTime;
            Task.Run(LogErrors(() =>
            {
                Log(body, "Simulation: Loading heightmap");
                var heightmap = body.terrainData.globalHeightmap.Get();
                AssertSquare(heightmap);

                Log(body, "Simulation: Loading global texture");
                var scaled_texture = body.terrainData.scaledSpaceTexture.Get();

                Log(body, "Simulation: Loading biome map");
                var biome_map = body.terrainData.biomes.map.Get();

                Log(body, "Simulation: Loading biome heightmaps");
                var red_hm_1 = body.terrainData.biomes.red.heightmap_1?.Get();
                var red_hm_2 = body.terrainData.biomes.red.heightmap_2?.Get();
                var green_hm_1 = body.terrainData.biomes.green.heightmap_1?.Get();
                var green_hm_2 = body.terrainData.biomes.green.heightmap_2?.Get();
                var blue_hm_1 = body.terrainData.biomes.blue.heightmap_1?.Get();
                var blue_hm_2 = body.terrainData.biomes.blue.heightmap_2?.Get();
                var alpha_hm_1 = body.terrainData.biomes.alpha.heightmap_1?.Get();
                var alpha_hm_2 = body.terrainData.biomes.alpha.heightmap_2?.Get();

                AssertSquare(red_hm_1);
                AssertSquare(red_hm_2);
                AssertSquare(green_hm_1);
                AssertSquare(green_hm_2);
                AssertSquare(blue_hm_1);
                AssertSquare(blue_hm_2);
                AssertSquare(alpha_hm_1);
                AssertSquare(alpha_hm_2);

                Log(body, "Simulation: Building array data");
                HashSet<(Source<Texture2D>, Source<Texture2D>, Source<Texture2D>)> texture_set = new();

                foreach (BiomeData biome in body.terrainData.biomes)
                {
                    if (biome.texture_1.albedo != null)
                        texture_set.Add(biome.texture_1.GetTextureTuple());
                    if (biome.texture_2.albedo != null)
                        texture_set.Add(biome.texture_2.GetTextureTuple());
                    if (biome.texture_3.albedo != null)
                        texture_set.Add(biome.texture_3.GetTextureTuple());
                    if (biome.texture_4.albedo != null)
                        texture_set.Add(biome.texture_4.GetTextureTuple());
                }

                var texture_src_list = texture_set.ToList();
                var texture_list = texture_src_list.Select(tuple => (tuple.Item1.Get(), tuple.Item2.Get(), tuple.Item3.Get())).ToList();

                Plugin.RunOnMainThread(() =>
                {
                    Log(body, "Simulation: Creating GameObject");
                    var go = new GameObject();
                    go.layer = 15;
                    go.transform.parent = parent;
                    go.transform.position = position;
                    go.transform.rotation = rotation;

                    var objects = new GameObject();
                    objects.name = "objects";
                    objects.transform.parent = go.transform;

                    var decals = new GameObject();
                    decals.name = "decals";
                    decals.transform.parent = go.transform;

                    go.AddComponent<DisposableObjectGC.BodyLock>().bodyId = body.id;

                    Log(body, "Simulation: Creating PQS");
                    var pqs = go.AddComponent<PQS>();

                    Log(body, "Simulation: Creating PQSRenderer");
                    var renderer = go.AddComponent<PQSRenderer>();

                    Log(body, "Simulation: Creating PQSDecalController");
                    var decal_controller = go.AddComponent<PQSDecalController>();

                    Log(body, "Simulation: Creating PqsTerrain");
                    var terrain = go.AddComponent<PqsTerrain>();

                    Log(body, "Simulation: Setting up PQS fields");
                    pqs.PQSRenderer = renderer;
                    renderer.Pqs = pqs;
                    renderer.MaxSubdivision = body.terrainData.maximumSubdivisions;
                    decal_controller.Pqs = pqs;
                    decal_controller.PqsDecalData = ScriptableObject.CreateInstance<PQSDecalData>();
                    decal_controller.PqsDecalData.HeightData = new ushort[1];
                    terrain.Pqs = pqs;

                    Log(body, "Simulation: Creating material");
                    var material = new Material(Shader.Find("KSP2/Environment/CelestialBody/CelestialBody_Local" + (Plugin.useOld ? "_Old" : "")));

                    material.SetTexture("_AlbedoScaledTex", scaled_texture);
                    material.SetTexture("_BiomeMaskTex", biome_map);
                    material.SetFloat("_Radius", (float)body.radius);

                    if (texture_list.Count > 0)
                    {
                        Log(body, "Simulation: Creating texture arrays");
                        var t = texture_list.Select(x => x.Item1).Where(x => x.width > 4).FirstOrDefault() ?? texture_list[0].Item1;
                        Texture2DArray albedo = new(t.width, t.height, texture_list.Count, texture_list[0].Item1.format, false);
                        t = texture_list.Select(x => x.Item2).Where(x => x.width > 4).FirstOrDefault() ?? texture_list[0].Item2;
                        Texture2DArray normal = new(t.width, t.height, texture_list.Count, texture_list[0].Item2.format, false, true);
                        t = texture_list.Select(x => x.Item3).Where(x => x.width > 4).FirstOrDefault() ?? texture_list[0].Item3;
                        Texture2DArray metal = new(t.width, t.height, texture_list.Count, texture_list[0].Item3.format, false, true);

                        DisposableObjectGC.RegisterObject(body, albedo);
                        DisposableObjectGC.RegisterObject(body, normal);
                        DisposableObjectGC.RegisterObject(body, metal);

                        albedo.name = body.id + "_alb";
                        normal.name = body.id + "_nrm";
                        metal.name = body.id + "_met";

                        int i = 0;
                        foreach ((Texture2D a, Texture2D n, Texture2D m) in texture_list)
                        {
                            Graphics.CopyTexture(Resize(a, albedo, body), 0, 0, 0, 0, albedo.width, albedo.height, albedo, i, 0, 0, 0);
                            Graphics.CopyTexture(Resize(n, normal, body), 0, 0, 0, 0, normal.width, normal.height, normal, i, 0, 0, 0);
                            Graphics.CopyTexture(Resize(m, metal, body), 0, 0, 0, 0, metal.width, metal.height, metal, i, 0, 0, 0);
                            i++;
                        }

                        material.SetTexture("_SmallAlbedoArray", albedo);
                        material.SetTexture("_SmallNormalArray", normal);
                        material.SetTexture("_SmallMetalArray", metal);
                    }

                    foreach (var biome in body.terrainData.biomes)
                    {
                        Log(body, "Simulation: Configuring biome " + biome.channel);

                        int ind1 = biome.texture_1.albedo == null ? -1 : texture_src_list.IndexOf(biome.texture_1.GetTextureTuple());
                        int ind2 = biome.texture_2.albedo == null ? -1 : texture_src_list.IndexOf(biome.texture_2.GetTextureTuple());
                        int ind3 = biome.texture_3.albedo == null ? -1 : texture_src_list.IndexOf(biome.texture_3.GetTextureTuple());
                        int ind4 = biome.texture_4.albedo == null ? -1 : texture_src_list.IndexOf(biome.texture_4.GetTextureTuple());

                        material.SetVector("_SmallBiome" + biome.channel, new(ind1, ind2, ind3, ind4));
                        material.SetVector("_SmallEnable" + biome.channel, new(ind1 < 0 ? 0 : 1, ind2 < 0 ? 0 : 1, ind3 < 0 ? 0 : 1, ind4 < 0 ? 0 : 1));

                        ApplySmallChannelData(material, biome);
                    }

                    Log(body, "Simulation: Configuring distance resampling");
                    material.SetVector("_DistanceResampleDistances", new(body.terrainData.resample_1.distance, body.terrainData.resample_2.distance, body.terrainData.resample_3.distance, body.terrainData.resample_4.distance));
                    material.SetVector("_DistanceResampleUVScales", new(body.terrainData.resample_1.uvScale, body.terrainData.resample_2.uvScale, body.terrainData.resample_3.uvScale, body.terrainData.resample_4.uvScale));
                    material.SetVector("_DistanceResampleAlbedoOpacity", new(body.terrainData.resample_1.albedoOpacity, body.terrainData.resample_2.albedoOpacity, body.terrainData.resample_3.albedoOpacity, body.terrainData.resample_4.albedoOpacity));
                    material.SetVector("_DistanceResampleNormalOpacity", new(body.terrainData.resample_1.normalOpacity, body.terrainData.resample_2.normalOpacity, body.terrainData.resample_3.normalOpacity, body.terrainData.resample_4.normalOpacity));

                    Log(body, "Simulation: Creating PQSData");
                    pqs.data = ScriptableObject.CreateInstance<PQSData>();

                    PQSData.HeightRegion nullRegion = new() { heightMap = Texture2D.blackTexture };

                    Log(body, "Simulation: Creating BiomeLookupHashTable");
                    pqs.data.PlanetBiomeHashTable = ScriptableObject.CreateInstance<BiomeLookupHashTable>();

                    Log(body, "Simulation: Setting heightMapInfo");
                    pqs.data.heightMapInfo = new()
                    {
                        globalHeightMap = heightmap,
                        heightMapScale = (float)body.terrainData.heightScale,
                        scaledToLocalBlend = 1e5f,
                        scaledToLocalTransition = 1e6f,
                        largeA = alpha_hm_1 == null ? nullRegion : new() { heightMap = alpha_hm_1, heightScale = (float)body.terrainData.biomes.alpha.height_scale_1, uvScale = body.terrainData.biomes.alpha.uv_scale_1 },
                        largeR = red_hm_1 == null ? nullRegion : new() { heightMap = red_hm_1, heightScale = (float)body.terrainData.biomes.red.height_scale_1, uvScale = body.terrainData.biomes.red.uv_scale_1 },
                        largeG = green_hm_1 == null ? nullRegion : new() { heightMap = green_hm_1, heightScale = (float)body.terrainData.biomes.green.height_scale_1, uvScale = body.terrainData.biomes.green.uv_scale_1 },
                        largeB = blue_hm_1 == null ? nullRegion : new() { heightMap = blue_hm_1, heightScale = (float)body.terrainData.biomes.blue.height_scale_1, uvScale = body.terrainData.biomes.blue.uv_scale_1 },
                        mediumA = alpha_hm_2 == null ? nullRegion : new() { heightMap = alpha_hm_2, heightScale = (float)body.terrainData.biomes.alpha.height_scale_2, uvScale = body.terrainData.biomes.alpha.uv_scale_2 },
                        mediumR = red_hm_2 == null ? nullRegion : new() { heightMap = red_hm_2, heightScale = (float)body.terrainData.biomes.red.height_scale_2, uvScale = body.terrainData.biomes.red.uv_scale_2 },
                        mediumG = green_hm_2 == null ? nullRegion : new() { heightMap = green_hm_2, heightScale = (float)body.terrainData.biomes.green.height_scale_2, uvScale = body.terrainData.biomes.green.uv_scale_2 },
                        mediumB = blue_hm_2 == null ? nullRegion : new() { heightMap = blue_hm_2, heightScale = (float)body.terrainData.biomes.blue.height_scale_2, uvScale = body.terrainData.biomes.blue.uv_scale_2 },
                        subzone3A = nullRegion,
                        subzone3R = nullRegion,
                        subzone3G = nullRegion,
                        subzone3B = nullRegion,
                        subzone4A = nullRegion,
                        subzone4R = nullRegion,
                        subzone4G = nullRegion,
                        subzone4B = nullRegion,
                        mask = biome_map
                    };

                    Log(body, "Simulation: Setting materialSettings");
                    pqs.data.materialSettings = new()
                    {
                        meshCastShadows = true,
                        meshRecieveShadows = true,
                        surfaceMaterial = material,
                        antiTileOn = body.terrainData.antiTile
                    };

                    Log(body, "Simulation: Creating collider");
                    var collider = pqs.settings.colliderPrefab = new GameObject();
                    collider.AddComponent<MeshCollider>();
                    collider.AddComponent<SurfaceColliderData>();

                    for (int i = 0; i < 65536; i++)
                    {
                        pqs.data.PlanetBiomeHashTable.Cells[i].BiomeChunks = new();
                    }

                    Log(body, $"Simulation: Complete in {Time.unscaledTime - startTime}s");
                    resultCallback(go);
                    DisposableObjectGC.Release(body);
                });
            }));
        }

        static Texture2D Resize(Texture2D tex, Texture2DArray size, CelestialBodyData body)
        {
            if (tex.width == size.width && tex.height == size.height)
                return tex;

            if (tex.width > 8)
                Plugin.PopWarning($"Surface textures for {body.displayName} are inconsistent sizes, and will not render properly");

            Texture2D nt = new(size.width, size.height, size.format, false, true);
            Color32 color = tex.GetPixel(0, 0);
            nt.SetPixels32(Enumerable.Repeat(color, size.width * size.height).ToArray(), 0);
            nt.Apply();
            DisposableObjectGC.RegisterObject(body, nt);
            return nt;
        }

        static void ApplySmallChannelData(Material mat, BiomeData biome)
        {
            ApplySmallChannelDatum(mat, biome, "height_weight", "_SmallHeightWeight?");
            ApplySmallChannelDatum(mat, biome, "weight_softness", "_SmallWeightSoftness?");
            // _SmallBiomeHeightEnable?
            ApplySmallChannelDatum4(mat, biome, "height_params", "_SmallBiome?HeightParams");
            // _SmallBiomeSlopeEnable?
            ApplySmallChannelDatum4(mat, biome, "slope_params", "_SmallBiome?SlopeParams");
            ApplySmallChannelDatum4(mat, biome, "grad_map_weights", "_SmallBiome?GradMapWeights");
            // _SmallBiomePeakCavEnable?
            ApplySmallChannelDatum4(mat, biome, "peak_cav_params", "_SmallBiome?PeakCavParams");
            ApplySmallChannelDatum4(mat, biome, "curv_map_weights", "_SmallBiome?CurvMapWeights");
            ApplySmallChannelDatum(mat, biome, "uv_scale", "_SmallUVScale?");
            ApplySmallChannelDatum(mat, biome, "uv_offset", "_SmallUVOffset?");
            //ApplySmallChannelDatum4(mat, biome, "tint", "_SmallTint?");
            ApplySmallChannelDatum(mat, biome, "brightness", "_SmallBrightness?");
            ApplySmallChannelDatum(mat, biome, "contrast", "_SmallContrast?");
            ApplySmallChannelDatum(mat, biome, "saturation", "_SmallSaturation?");
            ApplySmallChannelDatum(mat, biome, "normal_strength", "_SmallNormalStrength?");
            ApplySmallChannelDatum(mat, biome, "gloss_strength", "_SmallGlossStrength?");
            ApplySmallChannelDatum(mat, biome, "metallic_strength", "_SmallMetallicStrength?");
            ApplySmallChannelDatum(mat, biome, "emission_strength", "_SmallEmissionStrength?");
            //ApplySmallChannelDatum4(mat, biome, "emission_color", "_SmallEmissionColor?");
            ApplySmallChannelDatum(mat, biome, "ao_strength", "_SmallAOStrength?");
            ApplySmallChannelDatum(mat, biome, "distance_resample_max", "_SmallDistanceResampleMax?");
        }

        static void ApplySmallChannelDatum(Material mat, BiomeData biome, string field, string shaderProperty)
        {
            FieldInfo fieldInfo = typeof(SurfaceTextureLayer).GetField(field);
            mat.SetVector(shaderProperty.Replace('?', biome.channel), new(
                (float)fieldInfo.GetValue(biome.texture_1),
                (float)fieldInfo.GetValue(biome.texture_2),
                (float)fieldInfo.GetValue(biome.texture_3),
                (float)fieldInfo.GetValue(biome.texture_4)));
        }

        static void ApplySmallChannelDatum4(Material mat, BiomeData biome, string field, string shaderProperty)
        {
            ApplySmallChannelDatum(mat, biome, field + "_1", shaderProperty + "1");
            ApplySmallChannelDatum(mat, biome, field + "_2", shaderProperty + "2");
            ApplySmallChannelDatum(mat, biome, field + "_3", shaderProperty + "3");
            ApplySmallChannelDatum(mat, biome, field + "_4", shaderProperty + "4");
        }
    }
}
