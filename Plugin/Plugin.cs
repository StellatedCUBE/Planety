using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using I2.Loc;
using KSP.Assets;
using KSP.Audio;
using KSP.Game;
using KSP.Game.Flow;
using KSP.Messages;
using Planety.ModLoader;
using Planety.UI;
using SpaceWarp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    [BepInPlugin(GUID, "Planety", VERSION)]
    [BepInProcess("KSP2_x64.exe")]
    [BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "moe.shinten.ksp2.planety";
        public const string VERSION = "0.2.0";
        public static string folder;
        public static string cacheFolder;
        public static Sprite icon;

        public const bool useOld = false;

        static ManualLogSource loggerStatic;
        static int mainThread;
        static ConcurrentQueue<Action> runOnMainThread = new ConcurrentQueue<Action>();
        public static bool traceAssetLoading = true;

        //PQSServer server = new();

        private void Awake()
        {
            try
            {
                Load();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void Load()
        {
            loggerStatic = Logger;
            Log(LogLevel.Info, $"Loading {GUID}");

            mainThread = Thread.CurrentThread.ManagedThreadId;
            folder = Path.Combine(Application.dataPath, "..\\Planety");
            cacheFolder = Path.Combine(Application.dataPath, "..\\.planetyCache");

            if (!Directory.Exists(cacheFolder))
            {
                var info = new DirectoryInfo(cacheFolder);
                info.Create();
                info.Attributes |= FileAttributes.Hidden;
            }

            Directory.CreateDirectory(Path.Combine(cacheFolder, "Stock"));
            Directory.CreateDirectory(folder);

            icon = Sprite.Create(new SourceTexture2DFile(Path.Combine(Application.dataPath, "..\\BepInEx\\plugins\\Planety\\Icon.png")).Get(), new(0, 0, 810, 810), Vector2.zero);

            var harmony = new Harmony(GUID);
            harmony.Patch(AccessTools.Method("KSP.Game.Load.CreateCelestialBodiesFlowAction:OnGalaxyDefinitionLoaded"), transpiler: new(typeof(HarmonyPatch_CreateCelestialBodiesFlowAction_OnGalaxyDefinitionLoaded).GetMethod("Transpiler")));
            harmony.Patch(AccessTools.Method("KSP.Game.Load.LoadCelestialBodyDataFilesFlowAction:OnGalaxyDefinitionLoaded"), transpiler: new(typeof(HarmonyPatch_CreateCelestialBodiesFlowAction_OnGalaxyDefinitionLoaded).GetMethod("Transpiler")), prefix: new(typeof(HarmonyPatch_LoadCelestialBodyDataFilesFlowAction_OnGalaxyDefinitionLoaded).GetMethod("Prefix")));
            harmony.Patch(AccessTools.Method("KSP.Game.SaveLoadManager:PrivateLoadCommon"), transpiler: new(typeof(HarmonyPatch_SaveLoadManager_PrivateLoadCommon).GetMethod("Transpiler")));
            harmony.Patch(AccessTools.Method("KSP.Map.Map3DView:ProcessSingleMapItem"), transpiler: new(typeof(HarmonyPatch_Map3DView_ProcessSingleMapItem).GetMethod("Transpiler")));
            harmony.Patch(AccessTools.Method("KSP.Sim.impl.ModelLookup:FindSimObjectByNameKey"), transpiler: new(typeof(HarmonyPatch_ModelLookup_FindSimObjectByNameKey).GetMethod("Transpiler")));
            harmony.Patch(AccessTools.Method("KSP.UI.Binding.UIValue_ReadString_Text:RedrawValue"), transpiler: new(typeof(HarmonyPatch_UIValue_ReadString_Text_RedrawValue).GetMethod("Transpiler")));
            harmony.Patch(AccessTools.Method("KSP.Rendering.CameraEffectsSystem:AddRingField"), transpiler: new(typeof(HarmonyPatch_CameraEffectsSystem_AddRingField).GetMethod("Transpiler")));
            harmony.Patch(AccessTools.Method("KSP.Game.CelestialBodyProvider:GetObservedStar"), transpiler: new(typeof(HarmonyPatch_CelestialBodyProvider_GetObservedStar).GetMethod("Transpiler")));
            harmony.Patch(AccessTools.Method("KSP.Game.CelestialBodyProvider:GetNeighboringBodiesByVisibility"), transpiler: new(typeof(HarmonyPatch_CelestialBodyProvider_GetNeighboringBodiesByVisibility).GetMethod("Transpiler")));
            harmony.Patch(AccessTools.Method("KSP.Rendering.Planets.PQSRenderer:Awake"), prefix: new(typeof(HarmonyPatch_PQSRenderer_Awake).GetMethod("Prefix")));
            harmony.Patch(AccessTools.Method("KSP.Rendering.CelestialBodyRing:EnableModels"), prefix: new(typeof(HarmonyPatch_CelestialBodyRing_EnableModels).GetMethod("Prefix")));
            harmony.Patch(AccessTools.Method("KSP.Rendering.CelestialBodyRing:UpdateMaterial"), prefix: new(typeof(HarmonyPatch_CelestialBodyRing_UpdateMaterial).GetMethod("Prefix")));
            harmony.Patch(AccessTools.Method("KSP.Game.CheatsMenu:Awake"), prefix: new(typeof(HarmonyPatch_CheatsMenu_Awake).GetMethod("Prefix")));
            harmony.Patch(AccessTools.Method("KSP.Rendering.GraphicsManager:CalculateSourceStarDotProduct"), prefix: new(typeof(HarmonyPatch_GraphicsManager_CalculateSourceStarDotProduct).GetMethod("Prefix")));
            harmony.Patch(AccessTools.Method("KSP.Game.StartupFlow.InitializeGameInstanceFlowAction:DoAction"), prefix: new(typeof(Content).GetMethod("LoadAsync")));
            //harmony.Patch(typeof(KSPBaseAudio).GetMethods().Where(x => x.Name == "PostEvent" && x.GetParameters().Length > 4 && x.GetParameters()[0].ParameterType == typeof(string)).First(), prefix: new HarmonyMethod(typeof(HarmonyPatch_KSPBaseAudio_PostEvent).GetMethod("Prefix")));
            if (useOld)
            {
                harmony.Patch(AccessTools.Method("KSP.Rendering.Planets.PQSRenderer:DrawPQSDeferredDecalSurfacePass"), prefix: new(typeof(HarmonyPatch_PQSRenderer_DrawPQSDeferredDecalSurfacePass).GetMethod("Prefix")), postfix: new(typeof(HarmonyPatch_PQSRenderer_DrawPQSDeferredDecalSurfacePass).GetMethod("Postfix")));
                harmony.Patch(AccessTools.Method("KSP.Rendering.Planets.PQSRenderer:DrawPqsDepthNow"), prefix: new(typeof(HarmonyPatch_PQSRenderer_DrawPQSDeferredDecalSurfacePass).GetMethod("Prefix")), postfix: new(typeof(HarmonyPatch_PQSRenderer_DrawPQSDeferredDecalSurfacePass).GetMethod("Postfix")));
            }
            harmony.Patch(AccessTools.Method("KSP.Rendering.CelestialBodyLighting:Awake"), prefix: new(typeof(HarmonyPatch_CelestialBodyLighting_Awake).GetMethod("Prefix")));
            harmony.Patch(AccessTools.Method("KSP.Game.CreateCampaignMenu:Awake"), prefix: new(typeof(CreateCampaignMenuMenu).GetMethod("Awake")));
            harmony.Patch(AccessTools.Method("KSP.Game.CreateCampaignMenu:CreateNewCampaign"), prefix: new(typeof(CreateCampaignMenuMenu).GetMethod("Submit")));

            //harmony.Patch(AccessTools.Method("KSP.Rendering.GraphicsManager:OnObserverSOIChanged"), prefix: new HarmonyMethod(typeof(Plugin).GetMethod("SOIPrefix")));
            /*
            harmony.Log("KSP.Rendering.Planets.PQSRenderer:DrawPQSDeferredDecalSurfacePass");
            harmony.Log("KSP.Rendering.Planets.PQSRenderer:DrawPqsDepthNow");
            harmony.Log("KSP.Rendering.Planets.PQSRenderer:RenderPQS");
            harmony.Log("KSP.Rendering.Planets.PQSRenderer:RenderPrepass");
            harmony.Log("KSP.Rendering.Planets.PQSRenderer:DrawPlanet");
            harmony.Log("KSP.Rendering.Planets.PQSRenderer:GetMaterial");*/
            
            //harmony.Patch(AccessTools.Method("KSP.Game.Flow.SequentialFlow:UpdateProgress"), prefix: new(GetType().GetMethod("UPPf")));

            AssetLoader.Load();

            //Task.Run(server.Run);

            SaveData.Register();

            Casting.Populate();

            ContentLoader.lsd.OnMissingTranslation = LanguageSourceData.MissingTranslationAction.Fallback;
            ContentLoader.lsd.owner = new LanguageSource();
            LocalizationManager.Sources.Add(ContentLoader.lsd);

            Log($"Loaded {GUID}");
        }

        //internal static bool logNextFrame;

        private void Update()
        {
            while (runOnMainThread.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    PopError(e);
                }
            }

            /*
            if (HarmonyPatch_LogMethodCall.logging)
            {
                HarmonyPatch_LogMethodCall.logging = false;
            } else if (logNextFrame)
            {
                logNextFrame = false;
                HarmonyPatch_LogMethodCall.logging = true;
            }
            */
        }

        /*
        private void OnDestroy()
        {
            server.Stop();
        }
        */

        internal static void Log(LogLevel level, string message)
        {
            loggerStatic.Log(level, message);
        }

        internal static void Log(string message) => Log(LogLevel.Info, message);

        internal static void ALLog(string message)
        {
            if (traceAssetLoading)
                Log(message);
        }

        public static bool IsMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == mainThread;
        }

        public static void RunOnMainThread(Action action)
        {
            runOnMainThread.Enqueue(action);
        }

        /*
        public static MessageCenterMessage LAST_SOI_MESSAGE;
        public static void SOIPrefix(MessageCenterMessage msg)
        {
            LAST_SOI_MESSAGE = msg;
            Log(LogLevel.Info, UnityObjectPrinter.printer.StringifyObject(msg));
        }*/

        public static List<KSPAudioEventToStringPropertyWatcherBinding.StringToAudioEventListBinding> GetSOIAudioEvents() =>
            ((KSPAudioEventToStringPropertyWatcherBinding)FindObjectOfType<KSPAudioEventManager>().PropertyWatcherBindings.Find(binding => binding.Name == "AudioSOI")).EventsToStringBindings;

        public static bool IsPowerOf2(int x) => (x &~- x) == 0;

        //public static void UPPf(SequentialFlow __instance) => Log("Start " + (__instance.GetCurrentAction()?.Name ?? "null"));

        public static string DepictPath(string path)
        {
            path = Path.GetFullPath(path).Replace('\\', '/');
            var gamePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
            return path.StartsWith(gamePath) ? path.Substring(gamePath.Length + 1) : path;
        }

        public static void PopWarning(string message) => new PopMessage { message = message }.Pop();

        public static void PopWarning(string message, string sourceFile, (int, int) sourcePosition)
        {
            if (sourceFile == null)
                PopWarning(message);
            else
                (sourcePosition.Item1 < 0 ? new PopMessage { message = message, moreLines = $"In \"{DepictPath(sourceFile)}\"" } :
                    new PopMessage { message = message, moreLines = $"In \"{DepictPath(sourceFile)}\"\nOn line {sourcePosition.Item1} at column {sourcePosition.Item2}" }).Pop();
        }

        public static void PopError(string message) => new PopMessage { message = message, error = true }.Pop();

        public static void PopError(Exception error, string sourceFile = null)
        {
            if (error is ScriptException se)
            {
                var lines = se.Message.Split('\n');
                var fLines = lines.Skip(1).Select(l => l.Trim()).ToList();
                sourceFile = se.sourceFile ?? sourceFile;
                if (sourceFile != null)
                {
                    fLines.Add($"In \"{DepictPath(sourceFile)}\"");
                    if (se.pos.Item1 >= 0)
                        fLines.Add($"On line {se.pos.Item1} at column {se.pos.Item2}");
                }
                new PopMessage { message = lines[0], moreLines = fLines.Count > 0 ? string.Join("\n", fLines) : null, error = true }.Pop();
            }

            else if (error is PlanetyException)
            {
                if (sourceFile == null)
                    PopError(error.Message);

                else
                    new PopMessage { message = error.Message, moreLines = $"In \"{DepictPath(sourceFile)}\"", error = true }.Pop();
            }

            else if (sourceFile == null)
                new PopMessage { message = $"{error.GetType().Name}: {error.Message}", error = true, logOnlyExtra = "\n" + error.StackTrace };

            else
                new PopMessage { message = $"{error.GetType().Name}: {error.Message}", moreLines = $"In \"{DepictPath(sourceFile)}\"", error = true, logOnlyExtra = "\n" + error.StackTrace }.Pop();
        }
    }
}
