using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    static class HarmonyPatch_KSPBaseAudio_PostEvent
    {
        public static void Prefix(string eventName, GameObject owner, uint flags)
        {
            Plugin.Log($"Audio event {eventName} by {owner.name} ({flags}) from {new StackTrace()}");
        }
    }
}
