using System;
using System.Collections.Generic;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    public static class ConstellationRegionRegistry
    {
        public const string ResourceRegion = "GameData/ConstellationRegion";

        private static readonly List<ConstellationRegionData> _all = new List<ConstellationRegionData>(8);

        public static int Count { get { return _all.Count; } }
        public static IReadOnlyList<ConstellationRegionData> All { get { return _all; } }

        public static void Clear()
        {
            _all.Clear();
        }

        public static void Load()
        {
            Clear();

            TextAsset asset = Resources.Load<TextAsset>(ResourceRegion);
            if (asset == null)
            {
                Debug.LogWarning($"[ConstellationRegionRegistry] '{ResourceRegion}' 을 찾을 수 없습니다");
                return;
            }

            JsonValue rows;
            try
            {
                rows = JsonValue.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[ConstellationRegionRegistry] 파싱 실패 — {e.Message}");
                return;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                ConstellationRegionData d = ConstellationRegionData.FromJson(rows[i]);
                if (d == null || d.Id == 0)
                {
                    continue;
                }
                _all.Add(d);
            }

            _all.Sort(CompareById);
        }

        private static int CompareById(ConstellationRegionData a, ConstellationRegionData b)
        {
            return a.Id.CompareTo(b.Id);
        }
    }
}
