namespace Jinhyeong_Constellation
{

    /// <summary>
    /// 성좌 노드의 종류. 값은 저장에 들어가지 않지만(저장은 노드 id) 색과 크기가 여기 걸린다.
    /// </summary>
    public enum EConstellationNodeType
    {
        /// <summary>한가운데. 처음부터 보유하고 있고 살 수 없다 — 모든 경로의 출발점이다.</summary>
        Core = 0,

        /// <summary>잔가지. 작은 전역 특성 하나. 수가 많아 성좌의 «모양»은 사실상 이게 만든다.</summary>
        Small = 1,

        /// <summary>여러 번 살 수 있는 잔가지. 같은 특성을 최대 레벨까지 쌓는다.</summary>
        Level = 2,

        /// <summary>성단의 중심. 계열 한정 큰 특성.</summary>
        Notable = 3,

        /// <summary>스킬 해금. 사면 그 파츠가 필드 드랍 풀에 들어간다.</summary>
        Skill = 4,

        /// <summary>관문. 클리어 횟수를 채우기 전엔 지나갈 수 없다 — 실제로 길을 막는다.</summary>
        Gate = 5,

        /// <summary>갈래 끝. 계열을 규정하는 한 방.</summary>
        Keystone = 6
    }
}
