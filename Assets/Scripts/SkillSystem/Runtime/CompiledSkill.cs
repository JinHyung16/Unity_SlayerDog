using Jinhyeong_SkillSystem.BT;

namespace Jinhyeong_SkillSystem
{

    public class CompiledSkill
    {
        public SkillDefinition Source;
        public int Level;
        public SkillLevelData LevelData;

        public BTNode Root;

        /// <summary>이 스킬을 준 파츠의 옵션 보정. 파츠를 교체하면 통째로 갈아끼운다.</summary>
        public SkillModifier Modifier = SkillModifier.None;

        /// <summary>파츠가 준 스킬인지. 로드아웃 재장착(EquipAll)이 이걸 지우면 안 된다.</summary>
        public bool FromPart;

        public int SkillId
        {
            get { return Source != null && Source.Meta != null ? Source.Meta.Id : 0; }
        }

        public string Name
        {
            get { return Source != null && Source.Meta != null ? Source.Meta.Name : string.Empty; }
        }
    }
}
