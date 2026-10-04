using System;
using System.Collections.Generic;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    public static class SynergyRegistry
    {
        public const string ResourceSynergy = "GameData/Synergy";

        private static readonly List<SynergyData> _all = new List<SynergyData>(48);

        public static int Count { get { return _all.Count; } }
        public static IReadOnlyList<SynergyData> All { get { return _all; } }

        public static void Clear()
        {
            _all.Clear();
        }

        public static void Load()
        {
            Clear();

            TextAsset asset = Resources.Load<TextAsset>(ResourceSynergy);
            if (asset == null)
            {
                Debug.LogWarning($"[SynergyRegistry] '{ResourceSynergy}' 을 찾을 수 없습니다");
                return;
            }

            JsonValue rows;
            try
            {
                rows = JsonValue.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[SynergyRegistry] 파싱 실패 — {e.Message}");
                return;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                SynergyData d = SynergyData.FromJson(rows[i]);
                if (d == null || d.Id == 0)
                    continue;
                _all.Add(d);
            }
        }

        public static void Collect(
            IReadOnlyDictionary<ESkillFamily, int> familyCount,
            IReadOnlyDictionary<int, int> optionCountBySkill,
            List<SynergyData> buffer,
            ICollection<EDebuffType> appliedDebuffs = null)
        {
            buffer.Clear();
            for (int i = 0; i < _all.Count; i++)
            {
                SynergyData s = _all[i];

                if (s.ReqFamilyCount > 0)
                {
                    int have = 0;
                    if (familyCount != null)
                        familyCount.TryGetValue(s.Family, out have);
                    if (have < s.ReqFamilyCount)
                        continue;
                }

                if (s.ReqSkillId > 0)
                {
                    int opts = 0;
                    if (optionCountBySkill != null)
                        optionCountBySkill.TryGetValue(s.ReqSkillId, out opts);
                    if (opts < Mathf.Max(1, s.ReqOwnOptions))
                        continue;
                }

                if (s.ReqDebuff != EDebuffType.None)
                {
                    if (appliedDebuffs == null || appliedDebuffs.Contains(s.ReqDebuff) == false)
                        continue;
                }

                buffer.Add(s);
            }
        }

        public static bool AppliesTo(SynergyData s, int skillId, ESkillFamily family)
        {
            if (s == null)
                return false;
            if (s.VsDebuff != EDebuffType.None)
                return true;
            if (s.ReqSkillId > 0)
                return s.ReqSkillId == skillId;
            return s.Family == family;
        }
    }
}
