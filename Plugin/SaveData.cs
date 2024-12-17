using Newtonsoft.Json.Linq;
using Planety.UI;
using SpaceWarp.API.SaveGameManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety
{
    [Serializable]
    public class SaveData
    {
        [NonSerialized]
        public static SaveData instance;

        public JObject data;

        static void OnLoad(SaveData data)
        {
            ContentLoader.serializedPlanetyData = data.data;
        }

        internal static void Register()
        {
            instance = ModSaves.RegisterSaveLoadGameData<SaveData>("Planety", onLoad: OnLoad);
        }
    }
}
