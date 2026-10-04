using System.Collections.Generic;
using System.Globalization;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem
{

    public class SkillNodeData
    {
        public int NodeId;
        public ESkillNodeType NodeType;

        public readonly Dictionary<ESkillParamKey, string> Params =
            new Dictionary<ESkillParamKey, string>(8);

        public readonly List<SkillNodeData> Children = new List<SkillNodeData>(2);

        public bool HasParam(ESkillParamKey key)
        {
            return Params.ContainsKey(key);
        }

        public float GetFloat(ESkillParamKey key, SkillLevelData level = null, float fallback = 0f, SkillModifier mod = null)
        {
            if (Params.ContainsKey(key) == false)
            {
                return mod != null ? mod.Apply(key, fallback) : fallback;
            }
            if (level != null && level.TryGet(key, out float scaled))
            {
                return mod != null ? mod.Apply(key, scaled) : scaled;
            }
            if (float.TryParse(Params[key], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return mod != null ? mod.Apply(key, parsed) : parsed;
            }
            return mod != null ? mod.Apply(key, fallback) : fallback;
        }

        public int GetInt(ESkillParamKey key, SkillLevelData level = null, int fallback = 0, SkillModifier mod = null)
        {
            if (Params.ContainsKey(key) == false)
            {
                return mod != null ? mod.ApplyInt(key, fallback) : fallback;
            }
            if (level != null && level.TryGet(key, out float scaled))
            {
                return mod != null ? mod.ApplyInt(key, (int)scaled) : (int)scaled;
            }

            string raw = Params[key];
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return mod != null ? mod.ApplyInt(key, parsed) : parsed;
            }
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float asFloat))
            {
                return mod != null ? mod.ApplyInt(key, (int)asFloat) : (int)asFloat;
            }
            return mod != null ? mod.ApplyInt(key, fallback) : fallback;
        }

        public bool GetBool(ESkillParamKey key, bool fallback = false)
        {
            if (Params.TryGetValue(key, out string raw) == false || string.IsNullOrEmpty(raw))
            {
                return fallback;
            }
            if (bool.TryParse(raw, out bool parsed))
            {
                return parsed;
            }
            if (raw == "1")
            {
                return true;
            }
            if (raw == "0")
            {
                return false;
            }
            return fallback;
        }

        public string GetString(ESkillParamKey key, string fallback = "")
        {
            if (Params.TryGetValue(key, out string raw))
            {
                return raw ?? fallback;
            }
            return fallback;
        }
    }
}
