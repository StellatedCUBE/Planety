using I2.Loc;
using KSP.Api.CoreTypes;
using KSP.Game;
using KSP.Input;
using KSP.UI.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Planety.UI
{
    public class PopMessage
    {
        public static List<PopMessage> s = new();
        public static float lastPopTime = float.NegativeInfinity;

        public bool error;
        public string message;
        public string moreLines;
        public string logOnlyExtra;

        static GameObject icon;
        static GameObject window;
        static TMP_FontAsset font;

        public void Pop()
        {
            Plugin.Log(error ? BepInEx.Logging.LogLevel.Error : BepInEx.Logging.LogLevel.Warning, (moreLines == null ? message : $"{message}\n{moreLines}") + (logOnlyExtra ?? ""));
            Plugin.RunOnMainThread(() =>
            {
                if (s.Contains(this))
                    return;
                s.Add(this);
                lastPopTime = Time.unscaledTime;

                if (!icon)
                {
                    int size = Screen.height / 24;
                    icon = new("Planety Errors", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    icon.AddComponent<CanvasGroup>().blocksRaycasts = true;
                    GameObject.DontDestroyOnLoad(icon);
                    GameObject border1 = new("Outer Border", typeof(Image), typeof(Behaviour));
                    GameObject border2 = new();
                    GameObject image = new();
                    GameObject text = new();

                    var tp = border1.GetComponent<RectTransform>();
                    tp.SetParent(icon.transform, false);
                    tp.anchoredPosition = Vector2.zero;
                    tp.offsetMin = Vector2.one * -(size + 5);
                    tp.offsetMax = Vector2.one * +(size + 5);
                    tp.localPosition = new((Screen.width >> 1) - size - (size >> 1), (Screen.height >> 1) - size - (size >> 1));
                    tp.localScale = Vector3.zero;

                    border2.AddComponent<Image>().color = Color.black;
                    var t = border2.GetComponent<RectTransform>();
                    t.SetParent(tp, false);
                    t.anchoredPosition = Vector2.zero;
                    t.offsetMin = Vector2.one * -(size + 4);
                    t.offsetMax = Vector2.one * +(size + 4);

                    image.AddComponent<Image>().sprite = Plugin.icon;
                    t = image.GetComponent<RectTransform>();
                    t.SetParent(tp, false);
                    t.anchoredPosition = Vector2.zero;
                    t.offsetMin = Vector2.one * -size;
                    t.offsetMax = Vector2.one * +size;

                    var x = text.AddComponent<TextMeshProUGUI>();
                    x.fontSize = size * 0.75f;
                    x.text = "⚠";
                    x.color = Color.yellow;
                    x.alignment = TextAlignmentOptions.Center;
                    text.transform.SetParent(tp, false);
                    text.transform.localPosition = new(size * 0.96f, size * -0.96f);

                    var canvas = icon.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 255;
                }

                if (error)
                    icon.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;

                WindowSetCounts();
                AddToWindow();
            });
        }

        void AddToWindow()
        {
            if (window)
            {
                var content = window.transform.Find("Root/Window-App/GRP-Body/Content");
                /*GameObject ws = new();
                var wst = ws.AddComponent<TextMeshProUGUI>();
                wst.font = font;
                wst.fontSize = 16;
                wst.color = error ? Color.red : Color.yellow;
                wst.SetText("⚠");
                ws.transform.SetParent();
                ws.transform.localPosition = new(-18, 0, 0);*/
                AddLine(content, LocalizationManager.GetTranslation($"Planety/Log/Error/{error}"), error ? Color.red : Color.yellow, 12, HorizontalAlignmentOptions.Center);
                AddLine(content, message, Color.white, 16);
                /*var ws = AddLine(content, message, Color.white, 16);
                ws = (RectTransform)GameObject.Instantiate(ws.gameObject, ws).transform;
                Component.Destroy(ws.GetComponent<LayoutElement>());
                var wst = ws.GetComponent<TextMeshProUGUI>();
                wst.color = error ? Color.red : Color.yellow;
                wst.SetText("⚠");
                ws.anchorMin = Vector2.zero;
                ws.anchorMax = new(0, -3);
                ws.offsetMin = new(-18, 0);*/
                if (!string.IsNullOrWhiteSpace(moreLines))
                    foreach (var line in moreLines.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        AddLine(content, line, Color.gray, 14);
                GameObject.Instantiate(content.parent.Find("-----").gameObject, content);
            }
        }

        public override int GetHashCode() => message.GetHashCode();

        public override bool Equals(object obj) => obj is PopMessage other && message == other.message && moreLines == other.moreLines && error == other.error;

        static RectTransform AddLine(Transform to, string text, Color color, float size, HorizontalAlignmentOptions hal = HorizontalAlignmentOptions.Left)
        {
            var vlg = to.childCount == 0 ? null : to.GetChild(to.childCount - 1).GetComponent<VerticalLayoutGroup>();
            if (!vlg)
            {
                GameObject ggo = new("Group", typeof(LayoutElement));
                vlg = ggo.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandHeight = false;
                vlg.padding.left = vlg.padding.right = 58;
                vlg.padding.top = vlg.padding.bottom = 6;
                ggo.transform.SetParent(to);
            }
            GameObject line = new("Line", typeof(LayoutElement));
            var txt = line.AddComponent<TextMeshProUGUI>();
            txt.font = font;
            txt.fontSize = size;
            txt.color = color;
            txt.richText = false;
            txt.horizontalAlignment = hal;
            txt.SetText(text);
            line.transform.SetParent(vlg.transform);
            return txt.rectTransform;
        }

        public static void ClearAll()
        {
            s.Clear();
            if (icon)
                GameObject.Destroy(icon);
            if (window)
                GameObject.Destroy(window);
            icon = null;
        }

        static void WindowSetCounts()
        {
            if (window)
            {
                int errors = s.Count(m => m.error);
                var tf = window.transform.Find("Root/Window-App/GRP-Body/CurrentMenuTitle");
                tf.GetComponent<TextMeshProUGUI>().SetText(LocalizationManager.GetTranslation("Planety/Log/NWarnings", s.Count - errors));
                tf.GetChild(0).GetComponent<TextMeshProUGUI>().SetText(LocalizationManager.GetTranslation("Planety/Log/NErrors", errors));
            }
        }

        public static void OpenWindow(bool over)
        {
            if (window)
            {
                window.GetComponent<CanvasGroup>().SetVisible(true);
                if (over)
                    window.GetComponentInParent<Canvas>().sortingOrder = 111;
                return;
            }

            var cheats = GameManager.Instance.GetComponentInChildren<CheatsMenu>();
            window = GameObject.Instantiate(cheats.gameObject, cheats.transform.parent);
            var cg = window.GetComponent<CanvasGroup>();
            cg.SetVisible(true);
            Component.Destroy(window.GetComponent<CheatsMenu>());
            window.transform.Find("Root/Window-App/GRP-Header-App/Main Row/TXT-Title").GetComponent<Localize>().Term = "Planety/Log/Title";
            DelegateAction close = new();
            close.BindDelegate(() => cg.SetVisible(false));
            window.transform.Find("Root/Window-App/GRP-Header-App/Main Row/BTN-Close").GetComponent<UIAction_Void_Button>().BindAction(close);
            foreach (Transform tf in window.transform.Find("Root/Window-App/GRP-Body"))
            {
                if (tf.name == "Content")
                {
                    font = tf.Find("GeneralMenu/OverlaysSection/ToggleLocalization/ToggleLocalization/Label").GetComponent<TextMeshProUGUI>().font;
                    foreach (Transform tf2 in tf)
                        GameObject.Destroy(tf2.gameObject);
                    var vlg = tf.gameObject.AddComponent<VerticalLayoutGroup>();
                    vlg.childForceExpandHeight = false;
                    GameObject stopper = new();
                    stopper.transform.SetParent(tf);
                    GameObject.Destroy(stopper);
                }
                else if (tf.name == "CurrentMenuTitle")
                {
                    tf.SetAsFirstSibling();
                    Component.Destroy(tf.GetComponent<UIValue_ReadString_Text>());                    
                    var clone = GameObject.Instantiate(tf.gameObject, tf);
                    Component.Destroy(clone.GetComponent<LayoutElement>());
                    clone.transform.localPosition = Vector3.zero;
                    tf.GetComponent<TextMeshProUGUI>().color = Color.yellow;
                    var text = clone.GetComponent<TextMeshProUGUI>();
                    text.color = Color.red;
                    text.horizontalAlignment = HorizontalAlignmentOptions.Right;
                    WindowSetCounts();
                }
                else if (tf.name != "-----")
                    GameObject.Destroy(tf.gameObject);
            }
            foreach (var message in s)
                message.AddToWindow();
            if (over)
                window.GetComponentInParent<Canvas>().sortingOrder = 111;
        }

        class Behaviour : MonoBehaviour
        {
            Image im;
            int screenWidth;
            float age;
            bool wasMouseDown;
            bool hover;
            bool over = true;
            Curtain curtain = FindObjectOfType<Curtain>();

            public void Start()
            {
                im = GetComponent<Image>();
                screenWidth = Screen.width;
            }

            public void Update()
            {
                if (screenWidth != Screen.width)
                {
                    Destroy(icon);
                    return;
                }

                if (im.rectTransform.rect.Contains(Mouse.Position - (Vector2)transform.position))
                {
                    if (!hover)
                        Audio.Play(Audio.Sound.Hover, this);
                    hover = true;
                    bool isMouseDown = Mouse.Left.IsPressed();
                    if (isMouseDown && !wasMouseDown)
                        Audio.Play(Audio.Sound.Select, this);
                    if (wasMouseDown && !isMouseDown)
                        OpenWindow(over);
                    wasMouseDown = isMouseDown;
                }
                else
                {
                    hover = false;
                    wasMouseDown = false;
                }

                age += Time.unscaledDeltaTime;
                int t = (int)((Time.unscaledTime - lastPopTime) * 10f);
                im.color = hover ? wasMouseDown ? Color.green : new(0f, 0.7f, 1f) : t < 9 && t % 2 == 0 ? Color.yellow : new(0.6f, 0.6f, 0.6f);
                transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, age * 5f);

                if (over && (!curtain || !curtain.IsShowing))
                {
                    over = false;
                    transform.parent.GetComponent<Canvas>().sortingOrder = 109;
                }
            }
        }
    }
}
