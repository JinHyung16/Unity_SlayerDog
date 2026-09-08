using System.Collections.Generic;

namespace Jinhyeong_SkillSystem
{

    public class SkillDefinition
    {
        public SkillData Meta;

        public readonly Dictionary<int, SkillLevelData> Levels =
            new Dictionary<int, SkillLevelData>(8);

        /// <summary>DB에서 조립이 끝난 BT 트리의 루트. null 이면 실행할 게 없는 스킬.</summary>
        public SkillNodeData Root;

        public SkillLevelData GetLevel(int level)
        {
            return Levels.TryGetValue(level, out SkillLevelData data) ? data : null;
        }
    }
}
