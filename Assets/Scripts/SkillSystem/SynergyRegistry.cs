using System;
using System.Collections.Generic;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    /// <summary>
    /// 시너지 테이블 — 조합의 보상.
    ///
    /// 두 축이 있고 **둘 다 자동 발동**이다. 3택1 을 없앤 게임에서 시너지만 고르라고
    /// 창을 띄우면 앞뒤가 안 맞는다.
    ///   넓게(계열) — 같은 계열 N개를 모으면 그 계열 전체가 세진다
    ///   깊게(특화) — 한 스킬의 증강을 N개 모으면 그 스킬만 대폭 세진다
    ///
    /// 특화가 한 스킬 기준으로 계열보다 세다. 그래야 「하나만 몰빵」이 실제 선택지가 된다.
    /// </summary>
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

        /// <summary>
        /// 지금 구성으로 발동 중인 시너지를 모은다.
        /// </summary>
        /// <param name="familyCount">계열별 보유 스킬 수</param>
        /// <param name="optionCountBySkill">스킬별로 붙어 있는 증강 수</param>
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

                // 계열 조건 — 같은 계열을 N개 이상 들었나
                if (s.ReqFamilyCount > 0)
                {
                    int have = 0;
                    if (familyCount != null)
                        familyCount.TryGetValue(s.Family, out have);
                    if (have < s.ReqFamilyCount)
                        continue;
                }

                // 특화 조건 — 그 스킬에 증강이 N개 이상 붙었나
                if (s.ReqSkillId > 0)
                {
                    int opts = 0;
                    if (optionCountBySkill != null)
                        optionCountBySkill.TryGetValue(s.ReqSkillId, out opts);
                    if (opts < Mathf.Max(1, s.ReqOwnOptions))
                        continue;
                }

                // 연계 조건 — 그 상태이상을 **거는 스킬**을 하나라도 들고 있나.
                // 거는 쪽과 때리는 쪽이 갈려야 조합이 생긴다.
                if (s.ReqDebuff != EDebuffType.None)
                {
                    if (appliedDebuffs == null || appliedDebuffs.Contains(s.ReqDebuff) == false)
                        continue;
                }

                buffer.Add(s);
            }
        }

        /// <summary>이 시너지가 해당 스킬에 걸리는지. 계열 시너지는 같은 계열 전체, 특화는 그 스킬만.</summary>
        public static bool AppliesTo(SynergyData s, int skillId, ESkillFamily family)
        {
            if (s == null)
                return false;
            // 연계는 특정 스킬이 아니라 **모든 스킬**에 걸린다 — 조건은 맞는 쪽 상태다.
            if (s.VsDebuff != EDebuffType.None)
                return true;
            if (s.ReqSkillId > 0)
                return s.ReqSkillId == skillId;
            return s.Family == family;
        }
    }
}
