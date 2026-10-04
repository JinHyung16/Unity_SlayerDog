using System;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem
{
    public class RunProgress : BaseBehaviour
    {
        public static RunProgress Instance { get; private set; }

        public static RunProgress Ensure()
        {
            if (Instance != null)
                return Instance;
            GameObject go = new GameObject("_RunProgress");
            return go.AddComponent<RunProgress>();
        }

        public event Action<int> OnLevelUp;

        public int Level { get; private set; } = 1;
        public float Xp { get; private set; }

        public float XpToNext { get { return XpRequired(Level); } }

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

            int guard = 0;
            while (Xp >= XpToNext && guard++ < 32)
            {
                Xp -= XpToNext;
                Level++;
                // 레벨업은 능력을 저절로 올리지 않는다 — 3택1 카드(LevelUpDraft)가 대신한다 (10-04 사용자 지시)
                if (OnLevelUp != null)
                    OnLevelUp.Invoke(Level);
            }
        }
    }
}
