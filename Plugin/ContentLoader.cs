using BepInEx.Logging;
using KSP.Sim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Planety.ModLoader;
using I2.Loc;
using Planety.UI;

namespace Planety
{
    static class ContentLoader
    {
        internal static LanguageSourceData lsd = new LanguageSourceData();
        //internal static List<Exception> modErrorsRuntime;
        internal static JObject serializedPlanetyData;
        internal static List<CelestialBodyData> newBodies;
        static Dictionary<string, CelestialBodyData> bodyMap;
        internal static Dictionary<string, CelestialBodyData> stockBodyMap;
        internal static List<AK.Wwise.Event> starMusic;

        internal static void LoadStockContent(Dictionary<string, SerializedCelestialBody> galaxy, Dictionary<string, KSP.Sim.Definitions.CelestialBodyData> bodies)
        {
            stockBodyMap = new Dictionary<string, CelestialBodyData>();

            var soiae = Plugin.GetSOIAudioEvents();

            foreach(string id in galaxy.Keys)
            {
                var galaxyData = galaxy[id];
                var bodyData = bodies[id];
                var rotation = bodyData.isRotating ? bodyData.isTidallyLocked ? Rotation.TidallyLocked() : Rotation.Period(bodyData.rotationPeriod) : Rotation.None();
                rotation.offset = bodyData.initialRotation;
                CelestialBodyData cbd = new()
                {
                    stock = true,
                    stockRadius = bodyData.radius,
                    id = id,
                    orbit = string.IsNullOrEmpty(galaxyData.OrbitProperties?.referenceBodyGuid) ? null : new OrbitData()
                    {
                        argumentOfPeriapsis = galaxyData.OrbitProperties.argumentOfPeriapsis,
                        eccentricity = galaxyData.OrbitProperties.eccentricity,
                        epoch = galaxyData.OrbitProperties.epoch,
                        inclination = galaxyData.OrbitProperties.inclination,
                        longitudeOfAscendingNode = galaxyData.OrbitProperties.longitudeOfAscendingNode,
                        meanAnomalyAtEpoch = galaxyData.OrbitProperties.meanAnomalyAtEpoch,
                        parentBody = galaxyData.OrbitProperties.referenceBodyGuid,
                        semiMajorAxis = galaxyData.OrbitProperties.semiMajorAxis
                    },
                    galacticPosition = bodyData.isStar ? Vector3.zero : null,
                    orbitColor = string.IsNullOrEmpty(galaxyData.OrbitProperties?.referenceBodyGuid) ? null : galaxyData.OrbiterProperties.orbitColor,
                    nodeColor = string.IsNullOrEmpty(galaxyData.OrbitProperties?.referenceBodyGuid) ? null : galaxyData.OrbiterProperties.nodeColor,
                    bodyType = bodyData.isStar ? CelestialBodyType.Star : !bodyData.hasSolidSurface ? CelestialBodyType.Gas : CelestialBodyType.Rock,
                    keyPrefix = bodyData.assetKeyScaled.Substring(0, bodyData.assetKeyScaled.Length - ".Scaled.prefab".Length),
                    radius = bodyData.radius,
                    surfaceGravity = bodyData.gravityASL,
                    rotation = rotation,
                    additionalTimewarpLockHeight = bodyData.TimeWarpAltitudeOffset,
                    axialTilt = bodyData.axialTilt,
                    light = bodyData.isStar ? new() : null,
                    _music = soiae.Find(e => string.Equals(e.Name, id, StringComparison.InvariantCultureIgnoreCase)).TargetEvents
                };

                if (bodyData.ringGroupData != null)
                    cbd.rings.AddRange(bodyData.ringGroupData.Select((rgd, i) => new Ring { innerRadius = rgd.innerRadius, outerRadius = rgd.outerRadius, stockIndex = i }));

                stockBodyMap[id] = cbd;
            }

            starMusic = soiae.Find(e => string.Equals(e.Name, "Kerbol", StringComparison.InvariantCultureIgnoreCase)).TargetEvents;
        }

        static void LoadContent()
        {
            PopMessage.ClearAll();

            var toLoad = serializedPlanetyData ?? new JObject();
            serializedPlanetyData = null;
            SaveData.instance.data = toLoad;

            newBodies = new List<CelestialBodyData>();
            //modErrorsRuntime = new List<Exception>();

            DisposableObjectGC.FreeUnused();

            foreach (var mod in Content.mods)
                if (mod.always || toLoad.ContainsKey(mod.id))
                    mod.Load();
        }

        static void CheckContent()
        {
            HashSet<string> ids = new(), existsButJustRemoved = new();

            foreach (var cbd in stockBodyMap.Values)
            {
                if (cbd.removed)
                    existsButJustRemoved.Add(cbd.id);
                else
                    ids.Add(cbd.id);
            }

            for (int i = 0; i < newBodies.Count; i++)
            {
                var body = newBodies[i];

                if (body.removed)
                    existsButJustRemoved.Add(body.id);
                else
                {
                    if (body.HasValidFieldSet(out HashSet<string> errors))
                    {
                        foreach (string error in errors)
                        {
                            Plugin.PopError($"Error loading celestial body \"{body.displayName}\" ({body.id}): {error}");
                        }

                        newBodies.RemoveAt(i);
                        i--;
                        continue;
                    }

                    body.FillOut();

                    ids.Add(body.id);
                }
            }

            bool dirty = true;
            while (dirty)
            {
                dirty = false;
                for (int i = 0; i < newBodies.Count; i++)
                {
                    var body = newBodies[i];

                    if (body.orbit != null && !ids.Contains(body.orbit.parentBody))
                    {
                        if (existsButJustRemoved.Contains(body.orbit.parentBody))
                            existsButJustRemoved.Add(body.id);
                        else
                            Plugin.PopError($"Error loading celestial body \"{body.displayName}\" ({body.id}): \"orbit.around\" is \"{body.orbit.parentBody}\", which does not exist");
                        newBodies.RemoveAt(i);
                        ids.Remove(body.id);
                        dirty = true;
                        break;
                    }
                }

                foreach (var body in stockBodyMap.Values)
                {
                    if (body.orbit != null && !ids.Contains(body.orbit.parentBody))
                    {
                        if (existsButJustRemoved.Contains(body.orbit.parentBody))
                            existsButJustRemoved.Add(body.id);
                        else
                            Plugin.PopError($"Error loading celestial body \"{body.displayName}\" ({body.id}): \"orbit.around\" is \"{body.orbit.parentBody}\", which does not exist");
                        body.removed = true;
                        ids.Remove(body.id);
                        dirty = true;
                        break;
                    }
                }
            }
        }

        static HashSet<string> localised = new();

        internal static List<CelestialBodyData> GetContent()
        {
            if (newBodies == null)
            {
                LoadContent();
                CheckContent();

                List<string[]> csv = new() { new string[] { "Key", "Type", LocalizationManager.CurrentLanguage } };

                bodyMap = new();
                foreach (var body in stockBodyMap.Values)
                {
                    bodyMap[body.id] = body;
                }

                foreach (var body in newBodies)
                {
                    bodyMap[body.id] = body;
                    TextHook.map[body.id] = body.displayName;
                    if (localised.Add(body.id))
                    {
                        csv.Add(new string[] { "CelestialBody/" + body.id, "Text", body.displayName });
                        csv.Add(new string[] { "Menu/TrackingStation/Synopsis/" + body.id, "Text", body.description });
                    }
                }

                if (csv.Count > 1)
                {
                    lsd.Import_CSV("", csv);
                    lsd.UpdateDictionary(true);
                }

                foreach (var body in bodyMap.Values)
                {
                    string p = body.orbit?.parentBody;
                    if (body.extendStarLightToReach && p != null && bodyMap.TryGetValue(p, out var pb) && pb.IsStar && pb.light != null && pb.extendStarLightToReach)
                    {
                        double minRange = body.orbit.semiMajorAxis.Value * (body.orbit.eccentricity + 1) * 1.35;
                        if (pb.light.range < minRange)
                        {
                            pb.light.range = minRange;
                            pb.dirty = true;
                        }
                    }
                }
            }

            return newBodies;
        }

        internal static CelestialBodyData GetBody(string name) => bodyMap[name];

        public static void Reset() => newBodies = null;
    }
}
