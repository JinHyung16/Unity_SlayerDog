using System;
using System.Collections.Generic;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    /// <summary>
    /// 성좌 계열 표 — 갈래 하나가 한 행이다.
    ///
    /// 성좌의 노드는 표에 없다. 표에는 「이 갈래가 어느 각도로 몇 성단 뻗는가」와
    /// 「그 갈래의 잔가지가 무슨 특성을 주는가」만 있고, 나머지는 생성기가 만든다.
    /// 그래서 **계열을 하나 늘리면 파일 하나로 갈래가 하나 자란다** — C# 을 고치지 않는다.
    /// </summary>
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

            // 생성 순서가 곧 노드 id 다. 파일 나열 순서에 기대면 OS 마다 성좌가 달라진다 — id 로 못 박는다.
            _all.Sort(CompareById);
        }

        private static int CompareById(ConstellationRegionData a, ConstellationRegionData b)
        {
            return a.Id.CompareTo(b.Id);
        }
    }
}
