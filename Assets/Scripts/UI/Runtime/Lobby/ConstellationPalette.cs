using System.Collections.Generic;
using Jinhyeong_Constellation;
using Jinhyeong_SkillSystem;
using UnityEngine;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 성좌의 색과 크기. 계열 색은 «데이터»에서 오고(계열 표의 color), 상태 색은 여기 있다 —
    /// 계열은 늘어날 수 있지만 「보유·구매가능·잠김」이라는 상태는 늘지 않는다.
    ///
    /// 색으로 상태를 말한다. 잠긴 것과 살 수 있는 것이 같은 색이면 트리를 봐도 다음에 뭘 할지 모른다.
    /// </summary>
    public static class ConstellationPalette
    {
        public static readonly Color EdgeIdle = new Color(0.22f, 0.24f, 0.33f, 1f);
        public static readonly Color EdgeOwned = new Color(0.55f, 0.72f, 1f, 1f);
        public static readonly Color EdgePath = new Color(1f, 0.82f, 0.38f, 1f);

        /// <summary>
        /// 선 굵기(그래프 유닛). 굵기와 색이 «같이» 달라져야 눈에 걸린다 —
        /// 색만 바꾸면 선 240개가 얽힌 화면에서 어느 것이 이어져 있는지 안 보인다.
        /// </summary>
        public const float EdgeWidthIdle = 4.5f;
        public const float EdgeWidthOwned = 9f;
        public const float EdgeWidthPath = 11f;

        /// <summary>안 산 선에 계열색을 섞는 정도. 진하면 산 선과 구분이 안 된다.</summary>
        public const float EdgeIdleTint = 0.16f;

        /// <summary>
        /// 테두리는 «상태»를 말한다. 바탕이 계열색을 맡으므로 역할이 겹치지 않게 무채색으로 간다.
        /// 살 수 있는 것만 밝게 서고 나머지는 뒤로 물러난다.
        /// </summary>
        public static readonly Color FrameOwned = new Color(1f, 1f, 1f, 0.92f);
        public static readonly Color FrameReachable = new Color(1f, 0.88f, 0.55f, 0.95f);
        public static readonly Color FrameLocked = new Color(0.50f, 0.54f, 0.66f, 0.55f);
        public static readonly Color FrameGateShut = new Color(1f, 0.42f, 0.40f, 0.95f);
        public static readonly Color FrameSelected = new Color(0.55f, 1f, 0.75f, 1f);

        /// <summary>아이콘 밝기도 상태를 따라간다. 못 가는 스킬의 그림이 또렷하면 살 수 있는 줄 안다.</summary>
        public static readonly Color IconOwned = new Color(1f, 1f, 1f, 1f);
        public static readonly Color IconReachable = new Color(1f, 1f, 1f, 0.88f);
        public static readonly Color IconLocked = new Color(0.82f, 0.85f, 0.94f, 0.60f);

        /// <summary>못 사는 노드의 바탕. 여기에 계열 색을 «조금» 섞는다.</summary>
        public static readonly Color NodeLocked = new Color(0.15f, 0.16f, 0.22f, 1f);

        /// <summary>
        /// 계열 색을 섞는 정도 — 못 삼 / 살 수 있음 / 보유.
        ///
        /// [사고] 미보유를 계열과 무관한 단색으로 뒀더니 성좌가 통째로 회색 한 덩어리였다.
        /// 여섯 갈래가 색으로 갈리지 않으면 「어느 갈래를 탈까」라는 질문 자체가 안 생긴다.
        /// 진행도가 색의 «진하기»로 보이게, 상태 셋을 같은 색의 세 단계로 둔다.
        /// </summary>
        public const float LockedTint = 0.30f;
        public const float ReachableTint = 0.62f;

        /// <summary>관문이 닫혀 있을 때.</summary>
        public static readonly Color NodeGateShut = new Color(0.38f, 0.18f, 0.18f, 1f);

        public static readonly Color CoreColor = new Color(1f, 0.88f, 0.55f, 1f);

        /// <summary>지름(그래프 유닛). 종류가 클수록 크다 — 크기가 곧 중요도다.</summary>
        public static float SizeOf(EConstellationNodeType type)
        {
            switch (type)
            {
                case EConstellationNodeType.Core: return 104f;
                case EConstellationNodeType.Keystone: return 88f;
                case EConstellationNodeType.Skill: return 72f;
                case EConstellationNodeType.Gate: return 62f;
                case EConstellationNodeType.Notable: return 58f;
                case EConstellationNodeType.Level: return 44f;
                default: return 34f;
            }
        }

        /// <summary>
        /// 이름을 띄우는 종류인가. 잔가지 150개에 전부 글자를 달면 성좌가 글자밭이 된다.
        /// 게임 화면은 이름을 아예 안 단다(누르면 설명 패널이 뜬다) — 이건 «편집기»가 쓴다.
        /// </summary>
        public static bool HasLabel(EConstellationNodeType type)
        {
            return type == EConstellationNodeType.Skill
                   || type == EConstellationNodeType.Notable
                   || type == EConstellationNodeType.Keystone
                   || type == EConstellationNodeType.Gate;
        }

        /// <summary>계열 표의 color 문자열을 읽어 색표를 만든다. 파싱에 실패하면 무채색으로 둔다.</summary>
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
