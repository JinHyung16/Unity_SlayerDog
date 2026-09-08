using System;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem
{
    /// <summary>
    /// 런 진행 — 경험치와 레벨.
    ///
    /// 레벨업에 선택지가 없다. 성장은 파츠가 담당하고, 레벨은 체력·공격력을 조용히 올리기만 한다.
    /// 이유: 선택의 재미는 「무엇을 주우러 갈까」 한 곳에만 둔다. 3택1 카드까지 겹치면
    /// 어느 쪽도 기억에 안 남고, 카드는 화면에 보이지도 않아 성장 실감이 없다.
    ///
    /// 레벨 보정을 키우면 안 된다. 공격력 +2%/Lv 이상으로 올리는 순간 레벨이 주 성장축이 되어
    /// 파츠를 주울 이유가 사라진다 — 파밍 게임이 스스로 핵심을 죽이는 실수다.
    /// </summary>
    public class RunProgress : BaseBehaviour
    {
        /// <summary>레벨당 최대 체력 증가율.</summary>
        public const float HpGainPerLevel = 0.04f;

        /// <summary>레벨당 공격력 증가율. 이 값을 키우면 파츠가 무의미해진다.</summary>
        public const float DamageGainPerLevel = 0.02f;

        public static RunProgress Instance { get; private set; }

        public static RunProgress Ensure()
        {
            if (Instance != null)
                return Instance;
            GameObject go = new GameObject("_RunProgress");
            return go.AddComponent<RunProgress>();
        }

        /// <summary>레벨업 순간. HUD 가 연출용으로 구독한다.</summary>
        public event Action<int> OnLevelUp;

        public int Level { get; private set; } = 1;
        public float Xp { get; private set; }

        public float XpToNext { get { return XpRequired(Level); } }

        /// <summary>
        /// 필요 경험치 곡선. 실측 기준선: 1분 Lv6 · 3분 Lv13 · 5분 Lv19.
        /// </summary>
        public static float XpRequired(int level)
        {
            int l = Mathf.Max(1, level);
            return 10f + (l - 1) * 8f + (l - 1) * (l - 1);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ResetRun()
        {
            Level = 1;
            Xp = 0f;
        }

        public void AddXp(float amount)
        {
            if (amount <= 0f)
                return;

            Xp += amount;

            // 한 번에 큰 경험치가 들어오면 여러 레벨이 연달아 오를 수 있다.
            // 선택지가 없으니 겹쳐도 문제되지 않는다 — 그냥 다 올려준다.
            int guard = 0;
            while (Xp >= XpToNext && guard++ < 32)
            {
                Xp -= XpToNext;
                Level++;
                ApplyLevelStats();
                if (OnLevelUp != null)
                    OnLevelUp.Invoke(Level);
            }
        }

        /// <summary>레벨업 보상은 스탯뿐. 즉시 반영해야 체감이 난다.</summary>
        private void ApplyLevelStats()
        {
            Jinhyeong_Character.Player player = GameEvents.CurrentPlayer;
            if (player == null)
                return;

            Damageable hp = player.Damageable;
            if (hp != null)
            {
                // 최대 체력이 오르면 그만큼 회복시켜 준다. 안 그러면 레벨업이 체감상 손해다.
                float add = hp.MaxHp * HpGainPerLevel;
                hp.AddMaxHp(add, true);
            }

            SkillObject skills = player.Skills;
            if (skills != null)
                skills.AttackPower *= 1f + DamageGainPerLevel;
        }
    }
}
