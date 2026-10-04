using System.Collections.Generic;

namespace Jinhyeong_SkillSystem
{

    public class SkillDefinition
    {
        public SkillData Meta;

        public readonly Dictionary<int, SkillLevelData> Levels =
            new Dictionary<int, SkillLevelData>(8);

        public SkillNodeData Root;

        public SkillLevelData GetLevel(int level)
        {
            return Levels.TryGetValue(level, out SkillLevelData data) ? data : null;
        }
    }
}
