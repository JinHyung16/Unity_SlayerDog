using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_Constellation.Editor
{

    /// <summary>
    /// 편집기의 색과 크기.
    ///
    /// 크기와 「이름을 다나」는 게임 쪽 팔레트를 그대로 빌려 쓴다 — 두 곳에 적으면
    /// 편집기에서 맞춰 놓은 간격이 게임에서는 안 맞는다. 편집기에만 필요한 것(글자 모양)만 여기 있다.
    ///
    /// 색은 게임과 «일부러» 다르다. 게임은 진행도(산 것/살 수 있는 것)를 색으로 말하지만,
    /// 편집기에서는 그런 상태가 없고 대신 «종류»가 한눈에 보여야 한다.
    /// </summary>
    public static class ConstellationPaletteEditor
    {
        private static GUIStyle _labelStyle;

        public static GUIStyle LabelStyle
        {
            get
            {
                if (_labelStyle == null)
                {
                    _labelStyle = new GUIStyle(EditorStyles.miniLabel);
                    _labelStyle.alignment = TextAnchor.UpperCenter;
                    _labelStyle.normal.textColor = new Color(0.80f, 0.82f, 0.90f, 1f);
                }
                return _labelStyle;
            }
        }

        public static float SizeOf(EConstellationNodeType type)
        {
            return Jinhyeong_UI.Lobby.ConstellationPalette.SizeOf(type);
        }

        public static bool HasLabel(EConstellationNodeType type)
        {
            return Jinhyeong_UI.Lobby.ConstellationPalette.HasLabel(type);
        }

        /// <summary>종류로 갈리는 색. 계열은 테두리가 아니라 색조로 섞어 둘 다 보이게 한다.</summary>
        public static Color ColorOf(ConstellationNode node)
        {
            Color baseColor;
            switch (node.Type)
            {
                case EConstellationNodeType.Core: baseColor = new Color(1f, 0.88f, 0.55f); break;
                case EConstellationNodeType.Keystone: baseColor = new Color(1f, 0.70f, 0.25f); break;
                case EConstellationNodeType.Skill: baseColor = new Color(0.45f, 0.80f, 1f); break;
                case EConstellationNodeType.Gate: baseColor = new Color(0.90f, 0.35f, 0.35f); break;
                case EConstellationNodeType.Notable: baseColor = new Color(0.72f, 0.55f, 1f); break;
                case EConstellationNodeType.Level: baseColor = new Color(0.45f, 0.90f, 0.75f); break;
                default: baseColor = new Color(0.62f, 0.66f, 0.78f); break;
            }

            Color family = FamilyColor(node.Family);
            return Color.Lerp(baseColor, family, 0.35f);
        }

        private static Color FamilyColor(ESkillFamily family)
        {
            for (int i = 0; i < ConstellationRegionRegistry.Count; i++)
            {
                ConstellationRegionData r = ConstellationRegionRegistry.All[i];
                if (r.Family != family)
                {
                    continue;
                }
                if (ColorUtility.TryParseHtmlString(r.Color, out Color c))
                {
                    return c;
                }
                break;
            }
            return new Color(0.6f, 0.62f, 0.7f);
        }
    }
}
