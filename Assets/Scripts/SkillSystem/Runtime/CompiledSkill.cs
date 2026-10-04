using Jinhyeong_SkillSystem.BT;

namespace Jinhyeong_SkillSystem
{

    public class CompiledSkill
    {
        public SkillDefinition Source;
        public int Level;
        public SkillLevelData LevelData;

        public BTNode Root;

        public SkillModifier Modifier = SkillModifier.None;

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
