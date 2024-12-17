using I2.Loc;
using KSP.Api.CoreTypes;
using KSP.Game;
using KSP.UI.Binding;
using KSP.UI.Binding.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Planety.UI
{
    internal static class CreateCampaignMenuMenu
    {
        static CreateCampaignMenu menu;
        static CanvasGroup submenu;

        public static void Awake(CreateCampaignMenu __instance)
        {

            menu = __instance;
            var options = __instance.transform.Find("Menu/CampaignOptions");
            var diffButton = options.Find("DifficultyChoice/DifficultyBtn");
            var planetyButton = GameObject.Instantiate(diffButton.gameObject, options, false);
            var label = planetyButton.transform.Find("TXT-Label");
            SetText(label, "Planety Settings");
            var evt = planetyButton.GetComponent<ButtonExtended>().onLeftClick;
            evt.RemoveAllListeners();
            evt.AddListener(OnClick);
            Component.Destroy(planetyButton.GetComponent<UIAction_Void_Button>());
            planetyButton.transform.SetSiblingIndex(options.Find("FirstTimeUserField").GetSiblingIndex());

            var smg = __instance.transform.Find("SubMenuGroup");
            CreateCampaignMenuSubMenu.font = smg.Find("CampaignName/ContentText").GetComponent<TextMeshProUGUI>().font;
            var gmMenu = smg.Find("GameModes");
            var planetyMenu = GameObject.Instantiate(gmMenu.gameObject, smg, false);
            submenu = planetyMenu.GetComponent<CanvasGroup>();
            var header = planetyMenu.transform.Find("CampaignTitle/HeaderText");
            SetText(header, "Planety Mod Options");
            GameObject.Destroy(planetyMenu.transform.Find("Content").gameObject);
            var vlg = planetyMenu.GetComponent<VerticalLayoutGroupExtended>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            new GameObject("Content", typeof(VerticalLayoutGroup), typeof(CreateCampaignMenuSubMenu)).transform.SetParent(planetyMenu.transform, false);
        }

        static void SetText(Component label, string text)
        {
            foreach (var c in label.GetComponents<Localize>())
                Component.Destroy(c);

            foreach (var c in label.GetComponents<UIDataContextBindBase>())
                Component.Destroy(c);

            label.gameObject.AddComponent<DelayedCall>().action = () => label.GetComponent<TextMeshProUGUI>().text = text;
        }

        static void OnClick()
        {
            menu.CloseSubMenu();
            menu.GetType().GetField("_currentSelectedMenu", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(menu, submenu);
            submenu.SetVisible(true);
        }

        public static void Submit() =>
            submenu.GetComponentInChildren<CreateCampaignMenuSubMenu>().Write();
    }
}
