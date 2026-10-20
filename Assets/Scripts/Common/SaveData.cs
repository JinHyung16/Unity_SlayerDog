using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Common
{
    public static class SaveData
    {
        public const string Key = "save.v1";

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

        // 세이브 형식 버전. 1 = 버전 칸이 없던 세이브(음악 · 개많이잡아 시절), 2 = SlayerDog 맵 · 해금 · 새 경제
        public const int CurrentVersion = 2;
        private const string BackupKey = "save.v1.bak";

        [Serializable]
        private class Blob
        {
            public int Version;
            public int BestSeconds;
            public int BestRound;
            public int BestKills;
            public int ClearCount;
            public int PlayCount;

            public int Kibble;
            public int SuperDna;

            public int[] UnlockedSkills = new int[0];
            public int Character = 1;
            public int Map = 1;
            public int MapCleared;

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
        public static int MapCleared { get { return Ensure().MapCleared; } }

        // 맵은 앞 맵을 클리어하면 열린다. 맵 1 은 처음부터
        public static bool IsMapUnlocked(int order)
        {
            return order <= Ensure().MapCleared + 1;
        }

        // 캐릭터: 시작 3명(시바 · 셰퍼드 · 푸들), 나머지는 출신 맵(Tables/map hero)을 클리어하면 합류
        private static readonly int[] StarterHeroes = { 1, 6, 7 };

        public static bool IsHeroUnlocked(int heroIndex)
        {
            for (int i = 0; i < StarterHeroes.Length; i++)
            {
                if (StarterHeroes[i] == heroIndex)
                    return true;
            }
            int originMap = Jinhyeong_SkillSystem.MapRegistry.OriginMapOf(heroIndex);
            return originMap > 0 && Ensure().MapCleared >= originMap;
        }

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

        // 3종 Lv20 까지 사료 약 8.3만 — 판당 약 1,500 으로 약 55판 (10챕터 완주 ≈ 65판에 맞춘다)
        public static int StatCost(EStat s)
        {
            int lv = StatLevel(s);
            return 100 + lv * 70 + lv * lv * 5;
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
            // 스팀 클라우드에 더 많이 진행한 세이브가 있으면 그쪽을 쓴다 (다른 PC 에서 이어 하기)
            string cloud = SteamService.ReadCloud();
            if (string.IsNullOrEmpty(cloud) == false && CloudIsAhead(cloud, json))
                json = cloud;
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

            Migrate(json);

            _unlocked.Clear();
            if (_data.UnlockedSkills != null)
            {
                for (int i = 0; i < _data.UnlockedSkills.Length; i++)
                    _unlocked.Add(_data.UnlockedSkills[i]);
            }

            for (int i = 0; i < StarterSkills.Length; i++)
                _unlocked.Add(StarterSkills[i]);

            RunSelection.CharacterIndex = IsHeroUnlocked(_data.Character) ? _data.Character : 1;
            RunSelection.MapIndex = _data.Map;
            return _data;
        }

        // 옛 세이브를 지금 형식으로. 고치기 전에 원문을 BackupKey 에 한 번 남긴다
        private static void Migrate(string rawJson)
        {
            if (_data.Version >= CurrentVersion)
                return;
            if (_data.Version > 0 || string.IsNullOrEmpty(rawJson) == false)
            {
                if (string.IsNullOrEmpty(rawJson) == false && PlayerPrefs.HasKey(BackupKey) == false)
                    PlayerPrefs.SetString(BackupKey, rawJson);
            }
            if (_data.Version < 2)
            {
                // 맵은 클리어한 다음 맵까지만, 캐릭터는 1~10 안 (Ensure 가 잠긴 캐릭터를 시바로 돌린다)
                _data.Map = Mathf.Clamp(_data.Map, 1, _data.MapCleared + 1);
                _data.Character = Mathf.Clamp(_data.Character, 1, RunSelection.Heroes.Length);
            }
            _data.Version = CurrentVersion;
            if (string.IsNullOrEmpty(rawJson) == false)
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(_data));
        }

        public static void Save()
        {
            Blob b = Ensure();

            int[] arr = new int[_unlocked.Count];
            _unlocked.CopyTo(arr);
            b.UnlockedSkills = arr;
            b.Character = RunSelection.CharacterIndex;
            b.Map = RunSelection.MapIndex;

            string json = JsonUtility.ToJson(b);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
            SteamService.WriteCloud(json);
        }

        private static bool CloudIsAhead(string cloud, string local)
        {
            if (string.IsNullOrEmpty(local))
                return true;
            try
            {
                Blob c = JsonUtility.FromJson<Blob>(cloud);
                Blob l = JsonUtility.FromJson<Blob>(local);
                return c != null && l != null && c.PlayCount > l.PlayCount;
            }
            catch (Exception)
            {
                return false;
            }
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

        // 마지막 판 보상 (결과 화면이 읽는다). 새 맵 = 이번 판에 처음 클리어한 맵 순서, 없으면 0
        public static int LastKibble { get; private set; }
        public static int LastDna { get; private set; }
        public static int LastNewMap { get; private set; }

        public static void RecordRun(int seconds, int round, int kills, bool cleared)
        {
            Blob b = Ensure();
            LastNewMap = 0;

            b.PlayCount++;
            if (seconds > b.BestSeconds)
                b.BestSeconds = seconds;
            if (round > b.BestRound)
                b.BestRound = round;
            if (kills > b.BestKills)
                b.BestKills = kills;
            bool firstClear = false;
            if (cleared)
            {
                b.ClearCount++;
                if (RunSelection.MapIndex > b.MapCleared)
                {
                    b.MapCleared = RunSelection.MapIndex;
                    firstClear = true;
                    bool all = true;
                    for (int i = 1; i <= RunSelection.Heroes.Length; i++)
                        all &= IsHeroUnlocked(i);
                    SteamService.OnMapCleared(b.MapCleared, all);
                }
            }

            // 판당 DNA 약 31 + 챕터 첫 클리어 30 — 스킬 트리(1,944) 약 53판
            LastKibble = kills;
            LastDna = DnaReward(round, kills, cleared, firstClear);
            if (firstClear)
                LastNewMap = RunSelection.MapIndex;
            b.Kibble += LastKibble;
            b.SuperDna += LastDna;

            Save();
        }

        public const int FirstClearDna = 30;

        public static int DnaReward(int round, int kills, bool cleared, bool firstClear)
        {
            return round + kills / 120 + (cleared ? 8 : 0) + (firstClear ? FirstClearDna : 0);
        }

        // 하네스 성장 재현용 (RunLoopCheck 7번째 칸). 기록 · 강화 · 스킬 트리를 비우고 재화 · 클리어만 채운다
        public static void DebugSetProgress(int kibble, int dna, int clears, int mapCleared)
        {
            Blob b = Ensure();
            b.Kibble = kibble;
            b.SuperDna = dna;
            b.ClearCount = clears;
            b.MapCleared = mapCleared;
            b.StatHp = 0;
            b.StatAtk = 0;
            b.StatSpd = 0;
            b.StarNodes = new int[0];
            b.StarLevels = new int[0];
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
