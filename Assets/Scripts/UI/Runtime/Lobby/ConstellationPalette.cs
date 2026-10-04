using System.Collections.Generic;
using Jinhyeong_Constellation;
using Jinhyeong_SkillSystem;
using UnityEngine;

namespace Jinhyeong_UI.Lobby
{

    public static class ConstellationPalette
    {
        public static readonly Color EdgeIdle = new Color(0.26f, 0.18f, 0.40f, 1f);
        public static readonly Color EdgeOwned = new Color(0.18f, 0.90f, 1f, 1f);
        public static readonly Color EdgePath = new Color(1f, 0.93f, 0.32f, 1f);

        public const float EdgeWidthIdle = 3.5f;
        public const float EdgeWidthOwned = 7f;
        public const float EdgeWidthPath = 9f;

        public const float EdgeIdleTint = 0.35f;

        public static readonly Color FrameOwned = new Color(1f, 1f, 1f, 0.92f);
        public static readonly Color FrameReachable = new Color(1f, 0.93f, 0.32f, 0.95f);
        public static readonly Color FrameLocked = new Color(0.58f, 0.48f, 0.76f, 0.55f);
        public static readonly Color FrameGateShut = new Color(1f, 0.32f, 0.22f, 0.95f);
        public static readonly Color FrameSelected = new Color(1f, 0.30f, 0.72f, 1f);

        public static readonly Color IconOwned = new Color(1f, 1f, 1f, 1f);
        public static readonly Color IconReachable = new Color(1f, 1f, 1f, 0.88f);
        public static readonly Color IconLocked = new Color(0.86f, 0.80f, 0.98f, 0.60f);

        public static readonly Color NodeLocked = new Color(0.16f, 0.10f, 0.26f, 1f);

        public const float LockedTint = 0.30f;
        public const float ReachableTint = 0.62f;

        public static readonly Color NodeGateShut = new Color(0.38f, 0.10f, 0.14f, 1f);

        public static readonly Color CoreColor = new Color(1f, 0.30f, 0.72f, 1f);

        // 10-04 사용자: 노드 · 엣지가 다닥다닥 — 크기를 약 25% 줄이고 그래프를 넓혔다 (가장 가까운 두 노드 사이 18 이상, 노드를 지나는 엣지 0)
        public static float SizeOf(EConstellationNodeType type)
        {
            switch (type)
            {
                case EConstellationNodeType.Core: return 80f;
                case EConstellationNodeType.Keystone: return 66f;
                case EConstellationNodeType.Skill: return 54f;
                case EConstellationNodeType.Gate: return 46f;
                case EConstellationNodeType.Notable: return 42f;
                case EConstellationNodeType.Level: return 32f;
                default: return 24f;
            }
        }

        public static bool HasLabel(EConstellationNodeType type)
        {
            return type == EConstellationNodeType.Skill
                   || type == EConstellationNodeType.Notable
                   || type == EConstellationNodeType.Keystone
                   || type == EConstellationNodeType.Gate;
        }

        public static Dictionary<int, Color> BuildFamilyColors()
        {
            Dictionary<int, Color> map = new Dictionary<int, Color>(8);
            IReadOnlyList<ConstellationRegionData> regions = ConstellationRegionRegistry.All;
            for (int i = 0; i < regions.Count; i++)
            {
                Color c;
                if (ColorUtility.TryParseHtmlString(regions[i].Color, out c) == false)
                {
                    c = EdgeOwned;
                }
                map[(int)regions[i].Family] = c;
            }
            return map;
        }
    }
}
