using HarmonyLib;
using KSP.Game;
using KSP.Game.Load;
using KSP.Sim.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Planety
{
    static class HarmonyPatch_LoadCelestialBodyDataFilesFlowAction_OnGalaxyDefinitionLoaded
    {
        public static void Prefix(object __instance)
        {
            FieldInfo field = AccessTools.Field(__instance.GetType(), "_resolve");
            Action resolve = (Action)field.GetValue(__instance);
            field.SetValue(__instance, (Action)delegate {
                var new_bodies = ContentLoader.GetContent();
                var game = (GameInstance)AccessTools.Field(__instance.GetType(), "_game").GetValue(__instance);
                var data = (LoadGameData)AccessTools.Field(__instance.GetType(), "_data").GetValue(__instance);
                int num = data.CelestialBodyProperties.Length - new_bodies.Count;

                foreach (CelestialBodyData body in new_bodies)
                {
                    var core = ToCelestialBodyCore(body);
                    game.CelestialBodies.RegisterBodyFromData(core);
                    data.CelestialBodyProperties[num] = core.data.ToOldBodyProperties();
                    num++;
                }

                for (int i = 0; i < data.CelestialBodyProperties.Length; i++)
                {
                    /*if (data.CelestialBodyProperties[i].bodyName == "Mun")
                    {
                        data.CelestialBodyProperties[i].assetKeyScaled = "planety://log/" + data.CelestialBodyProperties[i].assetKeyScaled;
                        data.CelestialBodyProperties[i].assetKeySimulation = "planety://log/" + data.CelestialBodyProperties[i].assetKeySimulation;
                    }*/

                    var customData = ContentLoader.GetBody(data.CelestialBodyProperties[i].bodyName);
                    if (customData.dirty && customData.stock)
                    {
                        var cbd = KSP.Sim.Definitions.CelestialBodyData.FromCelestialBodyProperties(data.CelestialBodyProperties[i]);
                        customData.UpdateCoreData(cbd);
                        cbd.assetKeyScaled = "planety://bodymod/" + cbd.assetKeyScaled;
                        cbd.assetKeySimulation = string.IsNullOrEmpty(cbd.assetKeySimulation) ? "" : "planety://bodymod/" + cbd.assetKeySimulation;
                        data.CelestialBodyProperties[i] = cbd.ToOldBodyProperties();

                        var ocbd = game.CelestialBodies.Get(customData.id).data;
                        cbd.LocalColonyObjects = ocbd.LocalColonyObjects;
                        cbd.LocalColonyObjectsData = ocbd.LocalColonyObjectsData;
                        cbd.LocalSimObjects = ocbd.LocalSimObjects;
                        cbd.LocalSimObjectsData = ocbd.LocalSimObjectsData;
                        game.CelestialBodies.Get(customData.id).data = cbd;
                    }
                }

                resolve();
            });
        }

        internal static CelestialBodyCore ToCelestialBodyCore(CelestialBodyData data)
        {
            return new CelestialBodyCore()
            {
                version = "0.3",
                useExternal = false,
                data = new KSP.Sim.Definitions.CelestialBodyData()
                {
                    bodyName = data.id,
                    assetKeyScaled = data.keyPrefix + ".Scaled.prefab",
                    assetKeySimulation = data.bodyType <= CelestialBodyType.Star ? "" : data.keyPrefix + ".Simulation.prefab",
                    bodyDisplayName = "CelestialBody/" + data.id,
                    bodyDescription = "Custom Body by Planety",
                    isStar = data.bodyType == CelestialBodyType.Star,
                    isHomeWorld = false,
                    hasSolidSurface = data.bodyType == CelestialBodyType.Rock,
                    hasOcean = false,
                    HasLocalSpace = true,
                    radius = data.radius.Value,
                    gravityASL = data.surfaceGravity.Value,
                    TerrainHeightScale = 1,
                    isRotating = data.rotation.rotating,
                    rotationPeriod = data.rotation.period,
                    initialRotation = data.rotation.offset,
                    isTidallyLocked = data.rotation.tidallyLocked,
                    atmospherePressureCurve = new(0, 0),
                    BodyAltitudeFluxCurve = new(0, 0),
                    BodyAltitudeRelativeHumidityCurve = new(0, 0),
                    BodyAltitudeSurfaceFluxCurve = new(0, 0),
                    BodyAltitudeTemperatureCurve = data.deathZoneRadius <= 0 ? new(0, 0) : new(new Keyframe[] {
                        new(0, 1e6f), new(data.deathZoneRadius - (float)data.radius, 1e6f), new(Next(data.deathZoneRadius - (float)data.radius), 0) }),
                    BodySurfaceFluxMapPath = "",
                    BodySurfaceFluxScale = 1,
                    ringGroupData = data.rings.Select(r => new CelestialBodyRingData { innerRadius = r.innerRadius.Value, outerRadius = r.outerRadius.Value }).ToList(),
                    MaxTerrainHeight = data.terrainData == null ? 0 : data.terrainData.heightScale + Math.Max(Math.Max(
                        data.terrainData.biomes.red.height_scale_1 + data.terrainData.biomes.red.height_scale_2,
                        data.terrainData.biomes.green.height_scale_1 + data.terrainData.biomes.green.height_scale_2), Math.Max(
                        data.terrainData.biomes.blue.height_scale_1 + data.terrainData.biomes.blue.height_scale_2,
                        data.terrainData.biomes.alpha.height_scale_1 + data.terrainData.biomes.alpha.height_scale_2)),
                    axialTilt = data.axialTilt,
                    StarLuminosity = data.light?.power ?? 0,
                    TimeWarpAltitudeOffset = data.additionalTimewarpLockHeight,
                    SphereOfInfluenceCalculationType = data.orbit == null ? 1 : 0
                    //atmosphereTemperatureSeaLevel = 9999,
                }
            };
        }

        static float Next(float x) => BitConverter.ToSingle(BitConverter.GetBytes(1 + BitConverter.ToInt32(BitConverter.GetBytes(x), 0)), 0);
    }
}
