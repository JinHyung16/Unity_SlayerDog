using System.Collections.Generic;
using Jinhyeong_GameData;

namespace Jinhyeong_Constellation
{

    /// <summary>
    /// 성좌의 «배치» 하나. 정의(무슨 효과냐)와 배치(어디에 있냐)를 나누지 않는 이유는
    /// 여기서는 **모든 배치가 절차 생성으로 한 번에 만들어지기 때문**이다 —
    /// 같은 정의를 손으로 여러 자리에 놓는 일이 없어서 층을 나눌 이득이 없다.
    ///
    /// 대신 <see cref="Id"/> 가 세이브 키다. 생성 순서가 바뀌면 id 가 밀리므로
    /// 시드와 생성 순서는 함부로 건드리지 않는다 (<see cref="MulberryRandom"/> 주석).
    /// </summary>
    public class ConstellationNode
    {
        public int Id;
        public EConstellationNodeType Type;

        /// <summary>속한 계열. 코어와 길 노드는 <see cref="ESkillFamily.None"/> 일 수 있다.</summary>
        public ESkillFamily Family;

        /// <summary>그래프 유닛 좌표. 픽셀이 아니다 — 화면 변환은 뷰가 한다.</summary>
        public float X;
        public float Y;

        /// <summary>링 번호(0이 안쪽). 비용과 값이 여기 비례한다.</summary>
        public int Ring;

        /// <summary>1레벨 값. <see cref="EConstellationNodeType.Level"/> 은 레벨마다 배수가 붙는다.</summary>
        public int Cost;

        public int MaxLevel = 1;

        public EPartTrait Trait;
        public float Value;

        /// <summary>참이면 <see cref="Family"/> 계열 스킬에만 걸린다. 거짓이면 전역.</summary>
        public bool FamilyScoped;

        /// <summary><see cref="EConstellationNodeType.Skill"/> 이 여는 스킬.</summary>
        public int SkillId;

        /// <summary><see cref="EConstellationNodeType.Gate"/> 를 여는 데 필요한 클리어 횟수.</summary>
        public int NeedClear;

        public string DisplayName = string.Empty;
        public string Description = string.Empty;

        /// <summary>이웃 노드 id. 방향이 없다 — 구매 판정이 방향을 쓰지 않기 때문이다.</summary>
        public readonly List<int> Neighbors = new List<int>(4);
    }
}
