using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Common
{
    public static class SaveData
    {
        private const string Key = "save.v1";

        public const int UnlockCost = 8;

        private static readonly int[] StarterSkills =
        {
            2001,
            2002,
            2005,
            2007,
            2013,
            2020,
            // 음악 전투 (10-04): 새 무기 2 · 예전 고유 3 (이제 무기 풀)
            2025,
            2026,
            2101,
            2102,
            2103
        };

        [Serializable]
        private class Blob
        {
            public int BestSeconds;
            public int BestRound;
            public int BestKills;
            public int ClearCount;
            public int PlayCount;

            public int Kibble;
            public int SuperDna;

            public int[] UnlockedSkills = new int[0];
            public int Character = 1;

            public int[] StarNodes = new int[0];
            public int[] StarLevels = new int[0];

            public int StatHp;
            public int StatAtk;
            public int StatSpd;
        }

        private static Blob _data;
        private static readonly HashSet<int> _unlocked = new HashSet<int>();

        public static int BestSeconds { get { return Ensure().BestSeconds; } }
        public static int BestRound { get { return Ensure().BestRound; } }
        public static int BestKills { get { return Ensure().BestKills; } }
        public static int ClearCount { get { return Ensure().ClearCount; } }
        public static int PlayCount { get { return Ensure().PlayCount; } }
        public static int Kibble { get { return Ensure().Kibble; } }
        public static int SuperDna { get { return Ensure().SuperDna; } }

        public static IReadOnlyCollection<int> UnlockedSkills { get { Ensure(); return _unlocked; } }

        public static IReadOnlyList<int> StarNodes { get { return Ensure().StarNodes; } }
        public static IReadOnlyList<int> StarLevels { get { return Ensure().StarLevels; } }

        public static void SetStarNodes(List<int> ids, List<int> levels)
        {
            Blob b = Ensure();
            b.StarNodes = ids != null ? ids.ToArray() : new int[0];
            b.StarLevels = levels != null ? levels.ToArray() : new int[0];
            Save();
        }

        public static bool SpendDna(int amount)
        {
            Blob b = Ensure();
            if (amount < 0 || b.SuperDna < amount)
                return false;

            b.SuperDna -= amount;
            Save();
            return true;
        }

        public static void GrantSkill(int skillId)
        {
            Ensure();
            if (_unlocked.Add(skillId))
                Save();
        }

        public enum EStat
        {
            Hp = 0,
            Attack = 1,
            Speed = 2
        }

        public const int StatMaxLevel = 20;

        public static float StatGain(EStat s)
        {
            switch (s)
            {
                case EStat.Hp: return 0.03f;
                case EStat.Attack: return 0.02f;
                default: return 0.01f;
            }
        }

        public static int StatLevel(EStat s)
        {
            Blob b = Ensure();
            switch (s)
            {
                case EStat.Hp: return b.StatHp;
                case EStat.Attack: return b.StatAtk;
                default: return b.StatSpd;
            }
        }

        public static int StatCost(EStat s)
        {
            return 60 + StatLevel(s) * 40;
        }

        public static bool BuyStat(EStat s)
        {
            Blob b = Ensure();
            int lv = StatLevel(s);
            if (lv >= StatMaxLevel)
                return false;

            int cost = StatCost(s);
            if (b.Kibble < cost)
                return false;

            b.Kibble -= cost;
            if (s == EStat.Hp)
                b.StatHp = lv + 1;
            else if (s == EStat.Attack)
                b.StatAtk = lv + 1;
            else
                b.StatSpd = lv + 1;

            Save();
            return true;
        }

        public static float StatMultiplier(EStat s)
        {
            return 1f + StatGain(s) * StatLevel(s);
        }

        private static Blob Ensure()
        {
            if (_data != null)
                return _data;

            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                _data = new Blob();
            }
            else
            {
                try
                {
                    _data = JsonUtility.FromJson<Blob>(json) ?? new Blob();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveData] 저장을 읽지 못해 새로 시작합니다 — {e.Message}");
                    _data = new Blob();
                }
            }

            _unlocked.Clear();
            if (_data.UnlockedSkills != null)
            {
                for (int i = 0; i < _data.UnlockedSkills.Length; i++)
                    _unlocked.Add(_data.UnlockedSkills[i]);
            }

            for (int i = 0; i < StarterSkills.Length; i++)
                _unlocked.Add(StarterSkills[i]);

            RunSelection.CharacterIndex = _data.Character;
            return _data;
        }

        public static void Save()
        {
            Blob b = Ensure();

            int[] arr = new int[_unlocked.Count];
            _unlocked.CopyTo(arr);
            b.UnlockedSkills = arr;
            b.Character = RunSelection.CharacterIndex;

            PlayerPrefs.SetString(Key, JsonUtility.ToJson(b));
            PlayerPrefs.Save();
        }

        public static void Load()
        {
            _data = null;
            Ensure();
        }

        public static bool IsUnlocked(int skillId)
        {
            Ensure();
            return _unlocked.Contains(skillId);
        }

        public static bool Unlock(int skillId, int cost)
        {
            Blob b = Ensure();
            if (_unlocked.Contains(skillId))
                return false;
            if (b.SuperDna < cost)
                return false;

            b.SuperDna -= cost;
            _unlocked.Add(skillId);
            Save();
            return true;
        }

        public static void RecordRun(int seconds, int round, int kills, bool cleared)
        {
            Blob b = Ensure();

            b.PlayCount++;
            if (seconds > b.BestSeconds)
                b.BestSeconds = seconds;
            if (round > b.BestRound)
                b.BestRound = round;
            if (kills > b.BestKills)
                b.BestKills = kills;
            if (cleared)
                b.ClearCount++;

            b.Kibble += kills;
            b.SuperDna += round + kills / 40 + (cleared ? 10 : 0);

            Save();
        }

        public static void Wipe()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            _data = null;
            _unlocked.Clear();
            StarWiped?.Invoke();
        }

        public static event Action StarWiped;
    }
}
