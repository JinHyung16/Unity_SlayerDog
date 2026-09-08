using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// UI 노드를 만드는 손. 런타임(패널이 동적 내용을 채울 때)과 에디터 빌더(골격 프리팹을 구울 때)가 같은 함수를 쓴다 —
    /// 두 곳이 다른 방식으로 만들면 룩이 갈린다.
    ///
    /// Image 에 스프라이트를 주지 않는다. 스프라이트 없는 Image 는 색 사각형으로 그려진다 —
    /// 런타임 생성 텍스처는 프리팹에 직렬화되지 않으므로 여기서는 애초에 쓰지 않는다.
    /// </summary>
    public static class UIBuild
    {
        // ── 팔레트. 창마다 색을 지어내지 않는다 ──
        public static readonly Color ColorBg = new Color(0.07f, 0.07f, 0.10f, 1f);
        public static readonly Color ColorRail = new Color(0.125f, 0.13f, 0.185f, 1f);
        public static readonly Color ColorSky = new Color(0.13f, 0.15f, 0.24f, 1f);
        public static readonly Color ColorCard = new Color(0.14f, 0.14f, 0.20f, 1f);
        public static readonly Color ColorCardSoft = new Color(0.20f, 0.21f, 0.30f, 1f);
        public static readonly Color ColorCardOn = new Color(0.25f, 0.45f, 0.85f, 1f);
        public static readonly Color ColorTabOff = new Color(0.12f, 0.12f, 0.16f, 1f);
        public static readonly Color ColorLocked = new Color(0.09f, 0.09f, 0.11f, 1f);
        public static readonly Color ColorUnique = new Color(0.24f, 0.20f, 0.34f, 1f);
        public static readonly Color ColorStart = new Color(0.95f, 0.50f, 0.18f, 1f);
        public static readonly Color ColorDanger = new Color(0.35f, 0.16f, 0.16f, 1f);
        public static readonly Color ColorDangerArmed = new Color(0.75f, 0.20f, 0.20f, 1f);
        public static readonly Color ColorGold = new Color(1f, 0.86f, 0.42f, 1f);
        public static readonly Color ColorMuted = new Color(0.70f, 0.72f, 0.80f, 1f);
        public static readonly Color ColorSubHead = new Color(0.80f, 0.82f, 0.90f, 1f);
        public static readonly Color ColorWarn = new Color(0.92f, 0.45f, 0.42f, 1f);

        /// <summary>테두리. 바탕보다 살짝 밝게 — 세게 주면 화면이 격자처럼 보인다.</summary>
        public static readonly Color ColorOutline = new Color(0.38f, 0.41f, 0.54f, 0.55f);

        /// <summary>
        /// 모서리 반지름(픽셀). 스프라이트 고유 경계(카드 15 · 패널 30 · 막대 7)와 가깝게 잡는다 —
        /// 멀어질수록 모서리 아트를 늘리거나 줄이게 되어 곡률이 뭉개진다.
        /// </summary>
        public const float CardCorner = 14f;
        public const float BigCardCorner = 20f;
        public const float PanelCorner = 24f;
        public static readonly Color ColorDim = new Color(0.55f, 0.57f, 0.65f, 1f);
        public static readonly Color ColorText = Color.white;
        public static readonly Color ColorTextLocked = new Color(0.45f, 0.45f, 0.50f, 1f);

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

        /// <summary>
        /// 세로 구간(0~1)을 차지하는 띠. 높이가 화면 비율로 정해지므로 화면비가 바뀌어도 위아래와 안 겹친다.
        /// width 0 이면 부모 폭을 그대로 쓴다.
        /// </summary>
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

        /// <summary>모양이 있는 바탕. 스프라이트는 흰색이라 색은 여기서 그대로 먹는다.</summary>
        public static Image MakePanel(Transform parent, string name, Color color, string spriteName, float corner)
        {
            Image img = MakeImage(parent, name, color);
            UISkin.ApplySliced(img, spriteName, corner);
            return img;
        }

        /// <summary>
        /// 바탕 위에 테두리를 «한 겹» 얹는다.
        ///
        /// 테두리를 바탕과 같은 이미지로 못 그리는 이유는, 하나의 <see cref="Image"/> 가
        /// 스프라이트 하나만 들기 때문이다. 그래서 자식으로 얹고 레이캐스트를 끈다 —
        /// 켜 두면 테두리가 버튼 클릭을 가로챈다.
        /// </summary>
        public static Image Outline(RectTransform parent, Color color, string spriteName, float corner)
        {
            Image img = MakeImage(parent, "Outline", color);
            Stretch(img.rectTransform);
            img.raycastTarget = false;
            UISkin.ApplyFrame(img, spriteName, corner);

            // [사고] 레이아웃 그룹이 붙은 자리에 테두리를 얹었더니 **테두리가 칸을 하나 차지해**
            // 내비 레일 맨 위에 빈 상자가 생기고 탭이 전부 아래로 밀렸다.
            // 테두리는 «부모를 덮는 장식»이지 목록의 한 칸이 아니다 — 레이아웃에서 빼 둔다.
            LayoutElement ignore = img.gameObject.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            return img;
        }

        public static TMP_Text MakeText(Transform parent, string name, string content, float size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            // 기본 폰트를 명시한다. 프리팹으로 구울 때 비어 있으면 런타임에 다른 폰트가 잡힐 수 있다.
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

            // 테두리가 «글자보다 아래»에 와야 한다. 글자를 먼저 만들면 테두리가 글자를 덮는다.
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
