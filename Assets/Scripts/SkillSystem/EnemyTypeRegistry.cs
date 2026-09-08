using System;
using System.Collections.Generic;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{

    /// <summary>
    /// 적 타입 테이블. 경과 시간에 따라 등장 가능한 타입을 가중치로 뽑는다.
    /// </summary>
    public static class EnemyTypeRegistry
    {
        public const string ResourceEnemy = "GameData/EnemyType";

        private static readonly Dictionary<int, EnemyTypeData> _byId = new Dictionary<int, EnemyTypeData>(8);
        private static readonly List<EnemyTypeData> _all = new List<EnemyTypeData>(8);
        private static readonly List<EnemyTypeData> _scratch = new List<EnemyTypeData>(8);

        public static int Count { get { return _all.Count; } }
        public static IReadOnlyList<EnemyTypeData> All { get { return _all; } }

        public static EnemyTypeData Get(int id)
        {
            return _byId.TryGetValue(id, out EnemyTypeData v) ? v : null;
        }

        public static void Clear()
        {
            _byId.Clear();
            _all.Clear();
        }

        public static void Load()
        {
            Clear();

            TextAsset asset = Resources.Load<TextAsset>(ResourceEnemy);
            if (asset == null)
            {
                Debug.LogWarning($"[EnemyTypeRegistry] '{ResourceEnemy}' 을 찾을 수 없습니다");
                return;
            }

            JsonValue rows;
            try
            {
                rows = JsonValue.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[EnemyTypeRegistry] 파싱 실패 — {e.Message}");
                return;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                EnemyTypeData data = EnemyTypeData.FromJson(rows[i]);
                if (data.Id <= 0)
                    continue;
                _byId[data.Id] = data;
                _all.Add(data);
            }
        }

        /// <summary>
        /// 경과 시간에 등장 가능한(minTime 이하) 타입 중 가중치 추첨. 후보가 없으면 null.
        /// </summary>
        public static EnemyTypeData PickWeighted(float elapsedSeconds)
        {
            _scratch.Clear();
            float total = 0f;
            for (int i = 0; i < _all.Count; i++)
            {
                EnemyTypeData t = _all[i];
                if (t.MinTime > elapsedSeconds || t.Weight <= 0f)
                    continue;
                _scratch.Add(t);
                total += t.Weight;
            }

            if (_scratch.Count == 0 || total <= 0f)
                return null;

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < _scratch.Count; i++)
            {
                roll -= _scratch[i].Weight;
                if (roll <= 0f)
                    return _scratch[i];
            }
            return _scratch[_scratch.Count - 1];
        }
    }
}
