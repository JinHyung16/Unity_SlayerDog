using System.Collections.Generic;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem
{

    public class SkillLevelData
    {
        public int Id;
        public int SkillId;
        public int Level;

        public readonly Dictionary<ESkillParamKey, float> Modifiers =
            new Dictionary<ESkillParamKey, float>(8);

        public bool TryGet(ESkillParamKey key, out float value)
        {
            return Modifiers.TryGetValue(key, out value);
        }

        public float GetOrDefault(ESkillParamKey key, float fallback = 0f)
        {
            return Modifiers.TryGetValue(key, out float v) ? v : fallback;
        }
    }
}
