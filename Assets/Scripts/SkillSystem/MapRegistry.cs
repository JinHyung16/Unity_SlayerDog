using System;
using System.Collections.Generic;
using Jinhyeong_Common;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    // 맵 10 (Tables/map). 순서(Order) 1 부터. 지금 고른 맵은 RunSelection.MapIndex
    public static class MapRegistry
    {
        public const string ResourceMap = "GameData/Map";

        private static readonly List<MapData> _all = new List<MapData>(10);

        public static int Count { get { EnsureLoaded(); return _all.Count; } }
        public static IReadOnlyList<MapData> All { get { EnsureLoaded(); return _all; } }

        public static MapData Current { get { return Get(RunSelection.MapIndex); } }

        public static MapData Get(int order)
        {
            EnsureLoaded();
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].Order == order)
                    return _all[i];
            }
            return _all.Count > 0 ? _all[0] : null;
        }

        // 이 캐릭터가 출신인 맵의 순서. 없으면 0
        public static int OriginMapOf(int heroIndex)
        {
            EnsureLoaded();
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].Hero == heroIndex)
                    return _all[i].Order;
            }
            return 0;
        }

        public static void Load()
        {
            _all.Clear();

            TextAsset asset = Resources.Load<TextAsset>(ResourceMap);
            if (asset == null)
            {
                Debug.LogWarning($"[MapRegistry] '{ResourceMap}' 을 찾을 수 없습니다");
                return;
            }

            JsonValue rows;
            try
            {
                rows = JsonValue.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[MapRegistry] 파싱 실패 — {e.Message}");
                return;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                MapData d = MapData.FromJson(rows[i]);
                if (d == null || d.Order <= 0)
                    continue;
                _all.Add(d);
            }
            _all.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        private static void EnsureLoaded()
        {
            if (_all.Count == 0)
                Load();
        }
    }
}
