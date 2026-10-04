using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jinhyeong_UI.Framework
{

    public static class UISkin
    {
        private const string Root = "UI/";

        public const string CardBg = "ui_rectangle_small_01";
        public const string CardFrame = "ui_rectangle_frame_small_01";

        public const string PanelBg = "ui_rectangle_big_02";
        public const string PanelFrame = "ui_rectangle_frame_big_02";

        public const string CircleBg = "ui_circle_bg_small_01";
        public const string CircleFrame = "ui_circle_frame_small_01";

        public const string BarBg = "ui_slider_bg_small_01";
        public const string Heart = "ui_heart";

        public static Sprite SkillIcon(int skillId)
        {
            return Get("Icons/" + skillId);
        }

        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>(12);

        public static Sprite Get(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }
            if (_cache.TryGetValue(spriteName, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = Resources.Load<Sprite>(Root + spriteName);
            if (sprite == null)
            {
                Debug.LogWarning($"[UISkin] '{Root}{spriteName}' 을 찾을 수 없습니다 — 단색으로 그립니다");
            }
            _cache[spriteName] = sprite;
            return sprite;
        }

        public static void ApplySliced(Image image, string spriteName, float cornerPixels)
        {
            if (image == null)
            {
                return;
            }
            Sprite sprite = Get(spriteName);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;

            Vector4 b = sprite.border;
            float border = Mathf.Max(Mathf.Max(b.x, b.y), Mathf.Max(b.z, b.w));
            if (border <= 0.01f)
            {
                image.type = Image.Type.Simple;
                return;
            }

            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = Mathf.Max(0.01f, border / Mathf.Max(1f, cornerPixels));
        }

        public static void ApplyFrame(Image image, string spriteName, float cornerPixels)
        {
            ApplySliced(image, spriteName, cornerPixels);
            if (image != null && image.type == Image.Type.Sliced)
            {
                image.fillCenter = false;
            }
        }

        public static void ApplySimple(Image image, string spriteName)
        {
            if (image == null)
            {
                return;
            }
            Sprite sprite = Get(spriteName);
            if (sprite == null)
            {
                return;
            }
            image.sprite = sprite;
            image.type = Image.Type.Simple;
        }
    }
}
