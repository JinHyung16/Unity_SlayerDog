using System.Collections.Generic;
using System.Globalization;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem
{

    /// <summary>
    /// BT 노드 한 개의 데이터. DB(_DataExporter)가 뱉은 중첩 트리를 그대로 담는다.
    /// 부모/순서 링크가 없는 이유는 트리가 이미 중첩으로 들어오기 때문 — 평면 표를 재조립하던 시절 잔재는 없앴다.
    /// </summary>
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

        /// <summary>
        /// 레벨 수치가 있으면 그게 이긴다.
        /// params 에는 빌드 타임에 1레벨 기준값이 이미 박혀 있어서, 레벨을 먼저 보지 않으면 성장이 통째로 무시된다.
        /// </summary>
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
