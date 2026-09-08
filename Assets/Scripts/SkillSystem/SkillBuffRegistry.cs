using System;
using System.Collections.Generic;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{

    public static class SkillBuffRegistry
    {
        public const string ResourceBuff = "GameData/SkillBuff";
        public const string ResourceDebuff = "GameData/SkillDebuff";

        private static readonly Dictionary<int, SkillBuffData> _buffs = new Dictionary<int, SkillBuffData>(16);
        private static readonly Dictionary<int, SkillDebuffData> _debuffs = new Dictionary<int, SkillDebuffData>(16);

        public static int BuffCount { get { return _buffs.Count; } }
        public static int DebuffCount { get { return _debuffs.Count; } }

        public static SkillBuffData GetBuff(int id)
        {
            return _buffs.TryGetValue(id, out SkillBuffData v) ? v : null;
        }

        public static SkillDebuffData GetDebuff(int id)
        {
            return _debuffs.TryGetValue(id, out SkillDebuffData v) ? v : null;
        }

        public static void Clear()
        {
            _buffs.Clear();
            _debuffs.Clear();
        }

        public static void Load()
        {
            Clear();
            BuildBuffs(LoadTable(ResourceBuff));
            BuildDebuffs(LoadTable(ResourceDebuff));
        }

        private static JsonValue LoadTable(string resourcePath)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[SkillBuffRegistry] '{resourcePath}' 을 찾을 수 없습니다");
                return JsonValue.Null;
            }

            try
            {
                return JsonValue.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[SkillBuffRegistry] {resourcePath}.json 파싱 실패 — {e.Message}");
                return JsonValue.Null;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }
        }


        private static void BuildBuffs(JsonValue rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                SkillBuffData data = SkillBuffData.FromJson(rows[i]);
                if (data.Id <= 0)
                    continue;
                _buffs[data.Id] = data;
            }
        }

        private static void BuildDebuffs(JsonValue rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                SkillDebuffData data = SkillDebuffData.FromJson(rows[i]);
                if (data.Id <= 0)
                    continue;
                _debuffs[data.Id] = data;
            }
        }
    }
}
