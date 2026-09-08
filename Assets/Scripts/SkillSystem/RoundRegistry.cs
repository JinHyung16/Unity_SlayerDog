using System;
using System.Collections.Generic;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    /// <summary>
    /// 라운드 테이블. 1분마다 한 라운드가 넘어가고 마지막 라운드에 보스가 나온다.
    ///
    /// 스폰 간격·묶음·동시 상한이 여기 있다 — 난이도의 본체가 이 세 숫자다.
    /// 클라 코드에 램프 수식을 박아 두면 기획이 곡선을 못 만지므로 전부 데이터로 뺐다.
    /// </summary>
    public static class RoundRegistry
    {
        public const string ResourceRound = "GameData/Round";

        /// <summary>한 라운드의 길이(초). 라운드 판정의 유일한 기준이다.</summary>
        public const float RoundSeconds = 60f;

        private static readonly List<RoundData> _all = new List<RoundData>(12);

        public static int Count { get { return _all.Count; } }
        public static IReadOnlyList<RoundData> All { get { return _all; } }

        /// <summary>마지막 라운드 번호. 이 라운드에 보스가 나오고, 처치하면 클리어다.</summary>
        public static int LastRound { get { return _all.Count > 0 ? _all[_all.Count - 1].Round : 0; } }

        /// <summary>클리어까지 걸리는 총 시간(초).</summary>
        public static float TotalSeconds { get { return _all.Count * RoundSeconds; } }

        public static void Clear()
        {
            _all.Clear();
        }

        /// <summary>경과 시간이 몇 라운드인지. 1부터 시작하고 마지막 라운드에서 멈춘다.</summary>
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
