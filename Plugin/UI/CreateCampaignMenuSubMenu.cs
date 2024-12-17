using KSP.UI;
using KSP.UI.Binding;
using Newtonsoft.Json.Linq;
using Planety.ModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Planety.UI
{
    public class CreateCampaignMenuSubMenu : MonoBehaviour
    {
        public static TMP_FontAsset font;

        bool loading;

        Dictionary<string, Func<bool>> includeMod = new Dictionary<string, Func<bool>>();

        void Start()
        {
            //gameObject.AddComponent<LayoutElement>().minWidth = transform.parent.Find("CampaignTitle").GetComponent<RectTransform>().sizeDelta.x;
            var vlg = GetComponent<VerticalLayoutGroup>();
            //vlg.spacing = 8;
            vlg.padding = new RectOffset(16, 16, 0, 0);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            SetLoading();
        }

        void SetLoading()
        {
            loading = true;

            includeMod.Clear();

            if (Content.mods != null)
                return;

            foreach (Transform t in transform)
                Destroy(t.gameObject);

            var loadSign = new GameObject();
            loadSign.transform.SetParent(transform, false);
            var tmp = loadSign.AddComponent<TextMeshProUGUI>();
            tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
            tmp.text = "◌";
            tmp.color = Color.gray;
            tmp.font = font;
            tmp.fontSize = 50;
        }

        void Update()
        {
            if (!loading || Content.mods == null)
                return;

            loading = false;

            foreach (Transform t in transform)
                Destroy(t.gameObject);

            //var width = GetComponent<LayoutElement>().minWidth;

            if (Content.mods.Count == 0)
            {
                var noContent = new GameObject();
                noContent.transform.SetParent(transform, false);
                var tmp = noContent.AddComponent<TextMeshProUGUI>();
                tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
                tmp.text = "No (working) Planety mods are installed.";
                tmp.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                tmp.font = font;
                tmp.fontSize = 24;
                return;
            }

            foreach (var mod in Content.mods)
            {
                if (mod.always)
                    continue;

                var go = new GameObject(mod.id);
                go.transform.SetParent(transform, false);
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                var toggle = CreateToggle(go.transform, false);
                includeMod.Add(mod.id, () => toggle.isOn);

                var nameLabel = new GameObject();
                nameLabel.transform.SetParent(go.transform, false);
                var tmp = nameLabel.AddComponent<TextMeshProUGUI>();
                tmp.text = mod.name;
                tmp.color = Color.white;
                tmp.font = font;
                tmp.fontSize = 20;
            }
        }

        Toggle CreateToggle(Transform parent, bool @default)
        {
            var orig = transform.parent.parent.parent.Find("Menu/CampaignOptions/FirstTimeUserField/KSP2ToggleSwitch");
            var ts = orig.GetComponent<AnimateToggleSwitch>();
            var offPos = ts.GetType().GetField("_offPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            var go = Instantiate(orig.gameObject, parent, false);
            offPos.SetValue(go.GetComponent<AnimateToggleSwitch>(), offPos.GetValue(ts));
            Destroy(go.GetComponent<EventTrigger>());
            Destroy(go.GetComponent<UIValue_WriteBool_Toggle>());
            Destroy(go.GetComponent<UIAction_Void_Toggle>());
            go.AddComponent<EventTrigger>();
            go.GetComponent<Toggle>().isOn = @default;

            return go.GetComponent<Toggle>();
        }

        public void Write()
        {
            var jo = new JObject();
            
            foreach (var pair in includeMod)
            {
                if (pair.Value())
                {
                    jo.Add(pair.Key, new JObject());
                }
            }

            ContentLoader.serializedPlanetyData = jo;
        }
    }
}
