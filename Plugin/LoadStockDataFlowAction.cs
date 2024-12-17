using BepInEx.Logging;
using KSP.Game;
using KSP.Game.Flow;
using KSP.Game.Load;
using KSP.IO;
using KSP.Sim;
using KSP.Sim.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class LoadStockDataFlowAction : FlowAction
    {
        private readonly LoadGameData data;

        private LoadStockDataFlowAction(LoadGameData data) : base("Loading Celestial Body Data for Planety")
        {
            this.data = data;
        }

        protected override void DoAction(Action resolve, Action<string> reject)
        {
            GameManager.Instance.Assets.Load<TextAsset>(data.SavedGame.GalaxyDefinitionKey, galaxy_asset =>
            {
                var galaxy = IOProvider.FromJson<SerializedGalaxyDefinition>(galaxy_asset.text);
                var galaxy_map = new Dictionary<string, SerializedCelestialBody>();

                foreach (SerializedCelestialBody body in galaxy.CelestialBodies)
                {
                    galaxy_map[body.GUID] = body;
                }

                GameManager.Instance.Assets.LoadByLabel<TextAsset>("celestial_bodies", null, body_assets =>
                {
                    var body_map = new Dictionary<string, KSP.Sim.Definitions.CelestialBodyData>();
                    foreach (TextAsset body_asset in body_assets)
                    {
                        var body = IOProvider.FromBuffer<CelestialBodyCore>(body_asset.bytes);

                        if (galaxy_map.TryGetValue(body.data.bodyName, out var body_data)) {
                            body_map[body_data.GUID] = body.data;
                        }
                        else
                        {
                            Plugin.Log(LogLevel.Info, $"Unknown stock body {body.data.bodyName}, ignoring");
                        }
                    }

                    ContentLoader.LoadStockContent(galaxy_map, body_map);
                    resolve();
                });
            });
        }

        public static LoadStockDataFlowAction CreateAction(LoadGameData data)
        {
            return new LoadStockDataFlowAction(data);
        }
    }
}
