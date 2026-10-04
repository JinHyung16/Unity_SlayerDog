using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Jinhyeong_UI.Framework
{

    public static class UIBuild
    {
        // 팔레트: 진보라 밤 바탕 · 마젠타 테두리 · 시안 선택 · 레몬 보상 · 레드오렌지 위험 (_Ref/ArtReboot/기획서.md)
        public static readonly Color ColorBg = new Color(0.09f, 0.05f, 0.16f, 1f);
        public static readonly Color ColorRail = new Color(0.14f, 0.07f, 0.25f, 1f);
        public static readonly Color ColorSky = new Color(0.20f, 0.09f, 0.34f, 1f);
        public static readonly Color ColorCard = new Color(0.17f, 0.09f, 0.30f, 1f);
        public static readonly Color ColorCardSoft = new Color(0.26f, 0.14f, 0.42f, 1f);
        public static readonly Color ColorCardOn = new Color(0.10f, 0.72f, 0.92f, 1f);
        public static readonly Color ColorTabOff = new Color(0.13f, 0.07f, 0.22f, 1f);
        public static readonly Color ColorLocked = new Color(0.08f, 0.05f, 0.12f, 1f);
        public static readonly Color ColorUnique = new Color(0.36f, 0.14f, 0.52f, 1f);
        public static readonly Color ColorStart = new Color(1f, 0.20f, 0.60f, 1f);
        public static readonly Color ColorDanger = new Color(0.38f, 0.10f, 0.14f, 1f);
        public static readonly Color ColorDangerArmed = new Color(1f, 0.32f, 0.22f, 1f);
        public static readonly Color ColorGold = new Color(1f, 0.93f, 0.32f, 1f);
        public static readonly Color ColorMuted = new Color(0.80f, 0.72f, 0.95f, 1f);
        public static readonly Color ColorSubHead = new Color(0.92f, 0.84f, 1f, 1f);
        public static readonly Color ColorWarn = new Color(1f, 0.45f, 0.38f, 1f);

        public static readonly Color ColorOutline = new Color(1f, 0.30f, 0.72f, 0.85f);

        public const float CardCorner = 14f;
        public const float BigCardCorner = 20f;
        public const float PanelCorner = 24f;
        public static readonly Color ColorDim = new Color(0.64f, 0.55f, 0.80f, 1f);
        public static readonly Color ColorText = Color.white;
        public static readonly Color ColorTextLocked = new Color(0.50f, 0.42f, 0.60f, 1f);

        public static RectTransform MakeRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Band(RectTransform rt, float bottom, float top, float width)
        {
            if (width <= 0f)
            {
                rt.anchorMin = new Vector2(0f, bottom);
                rt.anchorMax = new Vector2(1f, top);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                return;
            }

            rt.anchorMin = new Vector2(0.5f, bottom);
            rt.anchorMax = new Vector2(0.5f, top);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 0f);
            rt.anchoredPosition = Vector2.zero;
        }

        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }

        public static Image MakeImage(Transform parent, string name, Color color)
        {
            RectTransform rt = MakeRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Image MakePanel(Transform parent, string name, Color color, string spriteName, float corner)
        {
            Image img = MakeImage(parent, name, color);
            UISkin.ApplySliced(img, spriteName, corner);
            return img;
        }

        public static Image Outline(RectTransform parent, Color color, string spriteName, float corner)
        {
            Image img = MakeImage(parent, "Outline", color);
            Stretch(img.rectTransform);
            img.raycastTarget = false;
            UISkin.ApplyFrame(img, spriteName, corner);

            LayoutElement ignore = img.gameObject.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            return img;
        }

        public static TMP_Text MakeText(Transform parent, string name, string content, float size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }
            text.text = content;
            text.fontSize = size;
            text.alignment = align;
            text.color = ColorText;
            text.raycastTarget = false;
            return text;
        }

        public static Button MakeButton(Transform parent, string name, string label, float fontSize, Color color, out TMP_Text text)
        {
            RectTransform rt = MakeRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            UISkin.ApplySliced(img, UISkin.CardBg, CardCorner);

            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;

            Outline(rt, ColorOutline, UISkin.CardFrame, CardCorner);

            text = MakeText(rt, "Label", label, fontSize, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            Stretch(text.rectTransform);
            return b;
        }

        public static VerticalLayoutGroup Vertical(RectTransform rt, float spacing, bool expandHeight)
        {
            VerticalLayoutGroup g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = expandHeight;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.spacing = spacing;
            return g;
        }

        public static HorizontalLayoutGroup Horizontal(RectTransform rt, float spacing)
        {
            HorizontalLayoutGroup g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = true;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.spacing = spacing;
            return g;
        }

        public static LayoutElement PreferredHeight(RectTransform rt, float height)
        {
            LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            return le;
        }
    }
}
