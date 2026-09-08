using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Common
{
    /// <summary>
    /// 런 밖에 남는 것들 — 기록 · 재화 · 해금.
    ///
    /// 파츠는 런 한정이지만 **무엇이 나올 수 있는지**는 영구 성장이다(기획서 「아웃게임」).
    /// 그 영구분이 저장될 자리가 없어서 껐다 켜면 전부 초기화됐다.
    ///
    /// `PlayerPrefs` + JSON 한 덩이로 둔다. 저장할 것이 몇 개 안 되는데 파일 포맷과 버전 관리를
    /// 먼저 세우면 그게 다 비용이 된다. 항목이 늘어 한 덩이가 무거워지면 그때 쪼갠다.
    /// </summary>
    public static class SaveData
    {
        private const string Key = "save.v1";

        /// <summary>
        /// 스킬 하나를 여는 값. 슈퍼 DNA 는 런당 「도달 라운드(+클리어 5)」로 들어오므로
        /// 초반엔 판당 3~5 개다. 8 이면 두세 판에 하나씩 열린다.
        /// </summary>
        public const int UnlockCost = 8;

        /// <summary>
        /// 처음부터 열려 있는 스킬 — 계열마다 하나씩.
        ///
        /// 전부 잠가 두면 **드랍 풀이 비어 첫 판이 성립하지 않는다.**
        /// 계열별로 하나씩 열어 두면 어떤 캐릭터를 골라도 자기 계열 스킬이 나온다.
        /// </summary>
        private static readonly int[] StarterSkills =
        {
            2001,   // 개튼튼 치아 — 근접
            2002,   // 왈왈 — 파동
            2005,   // 뒹굴기 — 장판
            2007,   // 우다다 모드 — 각성
            2013,   // 개눈빛 레이저 — 시선
            2020    // 사료 유성우 — 투척
        };

        [Serializable]
        private class Blob
        {
            public int BestSeconds;
            public int BestRound;
            public int BestKills;
            public int ClearCount;
            public int PlayCount;

            public int Kibble;      // 사료 — 스탯 강화용
            public int SuperDna;    // 슈퍼 DNA — 스킬 해금용

            public int[] UnlockedSkills = new int[0];
            public int Character = 1;

            // 성좌 — 산 노드의 id 와 레벨을 «자리 맞춰» 두 배열로 든다.
            // JsonUtility 는 Dictionary 를 못 담고, 사전을 담자고 저장 포맷을 갈아엎을 이유가 없다.
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

        /// <summary>
        /// 성좌가 산 것을 통째로 덮어쓴다. 부분 갱신을 받지 않는 이유는 성좌 쪽이 이미
        /// 「소유 전체」를 들고 있어서다 — 두 곳이 각자 일부만 알면 어긋난 순간을 잡을 방법이 없다.
        /// </summary>
        public static void SetStarNodes(List<int> ids, List<int> levels)
        {
            Blob b = Ensure();
            b.StarNodes = ids != null ? ids.ToArray() : new int[0];
            b.StarLevels = levels != null ? levels.ToArray() : new int[0];
            Save();
        }

        /// <summary>슈퍼 DNA 지출. 모자라면 아무것도 쓰지 않고 false.</summary>
        public static bool SpendDna(int amount)
        {
            Blob b = Ensure();
            if (amount < 0 || b.SuperDna < amount)
                return false;

            b.SuperDna -= amount;
            Save();
            return true;
        }

        /// <summary>
        /// 값을 치르지 않고 여는 해금. 성좌가 이미 노드 값으로 받아 갔을 때 쓴다 —
        /// <see cref="Unlock"/> 을 0원으로 부르면 「공짜 해금」이 일반 경로처럼 보인다.
        /// </summary>
        public static void GrantSkill(int skillId)
        {
            Ensure();
            if (_unlocked.Add(skillId))
                Save();
        }

        /// <summary>
        /// 사료로 사는 영구 강화.
        ///
        /// 한 단계가 크면 상점이 곧 난이도가 되고, 파츠로 만든 성장 곡선이 무의미해진다.
        /// 작게 여러 번 — 「조금씩 수월해진다」가 목적이지 「강해져서 이긴다」가 아니다.
        /// </summary>
        public enum EStat
        {
            Hp = 0,
            Attack = 1,
            Speed = 2
        }

        public const int StatMaxLevel = 20;

        /// <summary>단계당 상승률. 20단계를 다 올려도 체력 +60% 정도다.</summary>
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

        /// <summary>다음 단계 값. 올릴수록 비싸져야 초반 판이 의미를 갖는다.</summary>
        public static int StatCost(EStat s)
        {
            return 60 + StatLevel(s) * 40;
        }

        /// <summary>강화. 재화가 모자라거나 상한이면 false — 아무것도 쓰지 않는다.</summary>
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

        /// <summary>해당 스탯의 최종 배수.</summary>
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
                    // 저장이 깨졌다고 게임이 안 켜지면 안 된다. 새로 시작하고 사실만 알린다.
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

            // 기본 해금은 저장에 없어도 항상 열려 있다 — 목록이 늘어도 옛 저장이 그대로 따라온다.
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

        /// <summary>게임을 켤 때 한 번. 저장된 캐릭터 선택까지 복원한다.</summary>
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

        /// <summary>해금. 이미 열려 있으면 false 를 돌려주고 재화를 쓰지 않는다.</summary>
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

        /// <summary>
        /// 런이 끝났다. 기록을 갱신하고 재화를 준다.
        ///
        /// 죽어도 준다 — 기획서의 「사망: 그때까지 모은 사료·슈퍼 DNA 는 유지」와 같은 뜻이다.
        /// 아무것도 안 남으면 진 판이 통째로 버려진 시간이 된다.
        /// </summary>
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

            // 사료는 처치 수, 슈퍼 DNA 는 도달 라운드 + 처치 + 클리어 보너스.
            //
            // [실측] 라운드만 셌더니 판당 3~5 였다. 성좌 노드가 200개인데 그 수급이면
            // 갈래 하나를 여는 데만 수십 판이 걸려 「확장되어 간다」가 체감되지 않는다.
            // 처치를 섞어 잘한 판이 더 주도록 하고, 클리어 보너스를 10 으로 올렸다.
            b.Kibble += kills;
            b.SuperDna += round + kills / 40 + (cleared ? 10 : 0);

            Save();
        }

        /// <summary>테스트·초기화용. 사용자 조작으로만 부른다.</summary>
        public static void Wipe()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            _data = null;
            _unlocked.Clear();
            StarWiped?.Invoke();
        }

        /// <summary>초기화가 일어났다. 성좌처럼 «저장에서 파생된 캐시»를 든 쪽이 스스로 비운다.</summary>
        public static event Action StarWiped;
    }
}
