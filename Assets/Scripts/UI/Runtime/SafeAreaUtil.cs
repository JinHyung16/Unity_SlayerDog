using System;
using UnityEngine;

namespace Jinhyeong_UI
{

    public static class SafeAreaUtil
    {
        [Flags]
        public enum ApplySideType
        {
            None = 0,
            Top = 1 << 0,
            Bottom = 1 << 1,
            Left = 1 << 2,
            Right = 1 << 3,

            Vertical = Top | Bottom,
            Horizontal = Left | Right,
            All = Vertical | Horizontal,
        }

        public static void ApplySafeArea(RectTransform rect, ApplySideType type)
        {
            if (rect == null)
                return;

            Rect safeArea = Screen.safeArea;
            float width = Screen.width;
            float height = Screen.height;

            if (width <= 0f || height <= 0f)
                return;

            rect.anchorMin = new Vector2(
                (type & ApplySideType.Left) != 0 ? safeArea.xMin / width : 0f,
                (type & ApplySideType.Bottom) != 0 ? safeArea.yMin / height : 0f);

            rect.anchorMax = new Vector2(
                (type & ApplySideType.Right) != 0 ? safeArea.xMax / width : 1f,
                (type & ApplySideType.Top) != 0 ? safeArea.yMax / height : 1f);

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
