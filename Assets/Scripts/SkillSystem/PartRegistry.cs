using System;
using System.Collections.Generic;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    /// <summary>
    /// 파츠 테이블. 파츠 = 스킬 1개 + 옵션 슬롯이고, 착용하면 SkillId 스킬이 자동 발동한다.
    ///
    /// Owner 가 0 이 아니면 캐릭터 고유 파츠다 — 필드 드랍 풀에서 빠지고 그 캐릭터만 들고 시작한다.
    /// </summary>
    public static class PartRegistry
    {
        public const string ResourcePart = "GameData/Part";

        private static readonly Dictionary<int, PartData> _byId = new Dictionary<int, PartData>(32);
        private static readonly List<PartData> _all = new List<PartData>(32);
        private static readonly List<PartData> _dropPool = new List<PartData>(32);

        public static int Count { get { return _all.Count; } }
        public static IReadOnlyList<PartData> All { get { return _all; } }

        /// <summary>필드 드랍 대상 (고유 파츠 제외, Weight 0 초과).</summary>
        public static IReadOnlyList<PartData> DropPool { get { return _dropPool; } }

        public static PartData Get(int id)
        {
            return _byId.TryGetValue(id, out PartData v) ? v : null;
        }

        public static void Clear()
        {
            _byId.Clear();
            _all.Clear();
            _dropPool.Clear();
        }

        public static void Load()
        {
            Clear();

            TextAsset asset = Resources.Load<TextAsset>(ResourcePart);
            if (asset == null)
            {
                Debug.LogWarning($"[PartRegistry] '{ResourcePart}' 을 찾을 수 없습니다");
                return;
            }

            JsonValue rows;
            try
            {
                rows = JsonValue.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[PartRegistry] 파싱 실패 — {e.Message}");
                return;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                PartData d = PartData.FromJson(rows[i]);
                if (d == null || d.Id == 0)
                    continue;
                _byId[d.Id] = d;
                _all.Add(d);
                if (d.Owner == 0 && d.Weight > 0f)
                    _dropPool.Add(d);
            }
        }

        /// <summary>
        /// 이 스킬을 물고 있는 파츠. 스킬의 **한글 표시명**이 여기 있으므로
        /// UI 가 스킬 이름을 보여줄 때 거쳐 간다 — 스킬 메타에는 내부 영문명뿐이다.
        /// </summary>
        public static PartData GetBySkillId(int skillId)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].SkillId == skillId)
                    return _all[i];
            }
            return null;
        }

        /// <summary>해당 캐릭터의 고유 파츠. 없으면 null.</summary>
        public static PartData GetUniqueFor(int characterIndex)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].Owner == characterIndex)
                    return _all[i];
            }
            return null;
        }

        /// <summary>선호 계열에 걸리는 가중치 배수. 이 값이 1 이면 계열 시너지는 순전히 운이 된다.</summary>
        public const float PreferredFamilyBonus = 2.5f;

        /// <summary>
        /// 가중치 추첨. 드랍 풀이 비면 null.
        ///
        /// <paramref name="preferred"/> 계열은 더 자주 나온다. [실측] 이게 없으면 6부위를 다 채워도
        /// 계열이 골고루 1개씩 되어 **계열 시너지가 한 번도 안 뜬다** — 조합이 운에만 달리면
        /// 「조합을 이뤄가며 싸운다」가 성립하지 않는다.
        /// 캐릭터의 고유 파츠 계열을 선호 계열로 쓰면, 캐릭터 선택이 곧 빌드 방향이 된다.
        /// </summary>
        public static PartData Roll(ESkillFamily preferred = ESkillFamily.None)
        {
            float total = 0f;
            for (int i = 0; i < _dropPool.Count; i++)
                total += WeightOf(_dropPool[i], preferred);
            if (total <= 0f)
                return null;

            float r = UnityEngine.Random.value * total;
            for (int i = 0; i < _dropPool.Count; i++)
            {
                r -= WeightOf(_dropPool[i], preferred);
                if (r <= 0f)
                    return _dropPool[i];
            }
            return _dropPool[_dropPool.Count - 1];
        }

        private static float WeightOf(PartData d, ESkillFamily preferred)
        {
            // 아직 안 연 스킬은 필드에 안 나온다. 「무엇이 나올 수 있는지」가 곧 영구 성장이다.
            if (Jinhyeong_Common.SaveData.IsUnlocked(d.SkillId) == false)
                return 0f;

            float w = d.Weight;
            if (preferred != ESkillFamily.None && d.Family == preferred)
                w *= PreferredFamilyBonus;
            return w;
        }
    }
}
