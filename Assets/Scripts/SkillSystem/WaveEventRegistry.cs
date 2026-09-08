using System;
using System.Collections.Generic;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{

    /// <summary>정예/보스 웨이브 이벤트 테이블.</summary>
    public static class WaveEventRegistry
    {
        public const string ResourceWave = "GameData/WaveEvent";

        private static readonly List<WaveEventData> _all = new List<WaveEventData>(8);

        public static int Count { get { return _all.Count; } }
        public static IReadOnlyList<WaveEventData> All { get { return _all; } }

        public static void Clear()
        {
            _all.Clear();
        }

        public static void Load()
        {
            Clear();

            TextAsset asset = Resources.Load<TextAsset>(ResourceWave);
            if (asset == null)
            {
                Debug.LogWarning($"[WaveEventRegistry] '{ResourceWave}' 을 찾을 수 없습니다");
                return;
            }

            JsonValue rows;
            try
            {
                rows = JsonValue.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[WaveEventRegistry] 파싱 실패 — {e.Message}");
                return;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                WaveEventData data = WaveEventData.FromJson(rows[i]);
                if (data.Id <= 0)
                    continue;
                _all.Add(data);
            }
            _all.Sort((a, b) => a.Time.CompareTo(b.Time));
        }
    }
}
