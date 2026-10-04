using System;
using System.Collections.Generic;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    public static class RoundRegistry
    {
        public const string ResourceRound = "GameData/Round";

        public const float RoundSeconds = 60f;

        private static readonly List<RoundData> _all = new List<RoundData>(12);

        public static int Count { get { return _all.Count; } }
        public static IReadOnlyList<RoundData> All { get { return _all; } }

        public static int LastRound { get { return _all.Count > 0 ? _all[_all.Count - 1].Round : 0; } }

        public static float TotalSeconds { get { return _all.Count * RoundSeconds; } }

        public static void Clear()
        {
            _all.Clear();
        }

        public static int RoundOf(float elapsed)
        {
            if (_all.Count == 0)
                return 1;
            int r = Mathf.FloorToInt(elapsed / RoundSeconds) + 1;
            return Mathf.Clamp(r, 1, LastRound);
        }

        public static RoundData Get(int round)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i].Round == round)
                    return _all[i];
            }
            return _all.Count > 0 ? _all[_all.Count - 1] : null;
        }

        public static RoundData At(float elapsed)
        {
            return Get(RoundOf(elapsed));
        }

        public static void Load()
        {
            Clear();

            TextAsset asset = Resources.Load<TextAsset>(ResourceRound);
            if (asset == null)
            {
                Debug.LogWarning($"[RoundRegistry] '{ResourceRound}' 을 찾을 수 없습니다");
                return;
            }

            JsonValue rows;
            try
            {
                rows = JsonValue.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[RoundRegistry] 파싱 실패 — {e.Message}");
                return;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                RoundData d = RoundData.FromJson(rows[i]);
                if (d == null || d.Round <= 0)
                    continue;
                _all.Add(d);
            }
            _all.Sort((a, b) => a.Round.CompareTo(b.Round));
        }
    }
}
