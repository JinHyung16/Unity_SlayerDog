using System;
using System.Collections.Generic;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    public static class PartRegistry
    {
        public const string ResourcePart = "GameData/Part";

        private static readonly Dictionary<int, PartData> _byId = new Dictionary<int, PartData>(32);
        private static readonly List<PartData> _all = new List<PartData>(32);
        private static readonly List<PartData> _dropPool = new List<PartData>(32);

        public static int Count { get { return _all.Count; } }
        public static IReadOnlyList<PartData> All { get { return _all; } }

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

        public static PartData GetBySkillId(int skillId)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].SkillId == skillId)
                    return _all[i];
            }
            return null;
        }

        public static PartData GetUniqueFor(int characterIndex)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].Owner == characterIndex)
                    return _all[i];
            }
            return null;
        }

        public const float PreferredFamilyBonus = 2.5f;

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
            if (Jinhyeong_Common.SaveData.IsUnlocked(d.SkillId) == false)
                return 0f;

            float w = d.Weight;
            if (preferred != ESkillFamily.None && d.Family == preferred)
                w *= PreferredFamilyBonus;
            return w;
        }
    }
}
