using KSP.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety.UI
{
    public static class Audio
    {
        public enum Sound
        {
            Hover, Select
        }

        public static void Play(Sound sound, float y = 0.5f)
        {
            KSPBaseAudio.SetRTPCValue(KSPAudioParams.ui_position_y_rtpc, Mathf.Clamp01(y), KSPAudioEventManager.UIAudioGameObject);
            KSPBaseAudio.PostEvent(sound == Sound.Hover ? "Play_ui_extended_button_hover" : "Play_ui_extended_button_select", KSPAudioEventManager.UIAudioGameObject);
        }

        public static void Play(Sound sound, Vector2 position) => Play(sound, position.y / Screen.height);

        public static void Play(Sound sound, Component component) => Play(sound, component.transform.position);

        public static void Play(Sound sound, GameObject gameObject) => Play(sound, gameObject.transform.position);
    }
}
