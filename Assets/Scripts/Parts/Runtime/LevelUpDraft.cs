using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Parts
{
    // 레벨업 3택1 — 뱀서류의 «내가 고르는 성장». 레벨업이 저절로 능력을 올리던 것을 대신한다 (10-04 사용자 지시).
    // 카드 종류: 새 무기 · 가진 무기 강화 · 기본 공격 앰프 · 능력치. 무기 · 앰프는 스피커와 같은 병합 경로(PartLoadout.TryEquip)를 탄다
    public enum ELevelUpCardKind
    {
        NewWeapon,
        Upgrade,
        Amp,
        Stat
    }

    public class LevelUpCard
    {
        public ELevelUpCardKind Kind;
        public PartInstance Part;
        public EPartTrait StatTrait;
        public float StatValue;

        public int SkillId { get { return Part != null ? Part.SkillId : 0; } }
        public ESkillFamily Family { get { return Part != null && Part.Data != null ? Part.Data.Family : ESkillFamily.None; } }
    }

    public static class LevelUpDraft
    {
        public const int CardCount = 3;

        // 능력치 카드 — 예전 자동 상승(레벨당 체력 +4% · 공격력 +2%)보다 한 장이 크다. 대신 골라야 오른다
        public const float StatDamage = 0.12f;
        public const float StatMaxHp = 0.20f;
        public const float StatMoveSpeed = 0.06f;
        public const float StatLifesteal = 1.0f;

        private const float WeightNewWeapon = 3f;
        private const float WeightUpgrade = 3f;
        private const float WeightAmp = 2.5f;
        private const float WeightStat = 1f;

        private static readonly List<LevelUpCard> _pool = new List<LevelUpCard>(16);
        private static readonly List<float> _weights = new List<float>(16);

        public static List<LevelUpCard> Build(PartLoadout loadout)
        {
            List<LevelUpCard> result = new List<LevelUpCard>(CardCount);
            if (loadout == null)
                return result;

            _pool.Clear();
            _weights.Clear();

            // 새 무기 — 빈 칸이 있을 때만. 가진 것 · 고유는 뺀다
            if (loadout.IsFull == false)
            {
                HashSet<int> seen = new HashSet<int>();
                for (int i = 0; i < 12 && seen.Count < 3; i++)
                {
                    PartData d = PartRegistry.Roll(loadout.PreferredFamily);
                    if (d == null || loadout.CanUse(d) == false || loadout.Has(d.SkillId) || seen.Add(d.SkillId) == false)
                        continue;
                    Add(new LevelUpCard { Kind = ELevelUpCardKind.NewWeapon, Part = PartInstance.Create(d, PartInstance.RollGrade()) }, WeightNewWeapon);
                }
            }

            // 가진 무기 강화 — 옵션 자리가 남은 것
            IReadOnlyList<PartInstance> owned = loadout.Equipped;
            for (int i = 0; i < owned.Count; i++)
            {
                PartInstance p = owned[i];
                if (p == null || p.Data == null || p.Data.Owner != 0 || p.Options.Count >= PartInstance.MaxOptions)
                    continue;
                Add(new LevelUpCard { Kind = ELevelUpCardKind.Upgrade, Part = PartInstance.Create(p.Data, EPartGrade.Common) }, WeightUpgrade / Mathf.Max(1, owned.Count - 1));
            }

            // 기본 공격 앰프
            PartInstance basic = loadout.UniquePart;
            if (basic != null && basic.Options.Count < PartInstance.MaxOptions)
                Add(new LevelUpCard { Kind = ELevelUpCardKind.Amp, Part = PartInstance.Create(basic.Data, EPartGrade.Common) }, WeightAmp);

            // 능력치 — 항상 후보에 있다 (뽑을 무기가 없어도 카드 3장이 찬다)
            Add(new LevelUpCard { Kind = ELevelUpCardKind.Stat, StatTrait = EPartTrait.Damage, StatValue = StatDamage }, WeightStat);
            Add(new LevelUpCard { Kind = ELevelUpCardKind.Stat, StatTrait = EPartTrait.MaxHp, StatValue = StatMaxHp }, WeightStat);
            Add(new LevelUpCard { Kind = ELevelUpCardKind.Stat, StatTrait = EPartTrait.MoveSpeed, StatValue = StatMoveSpeed }, WeightStat);
            Add(new LevelUpCard { Kind = ELevelUpCardKind.Stat, StatTrait = EPartTrait.Lifesteal, StatValue = StatLifesteal }, WeightStat);

            for (int n = 0; n < CardCount && _pool.Count > 0; n++)
            {
                int pick = PickWeighted();
                result.Add(_pool[pick]);
                _pool.RemoveAt(pick);
                _weights.RemoveAt(pick);
            }
            return result;
        }

        public static void Apply(LevelUpCard card, PartLoadout loadout, SkillObject skills)
        {
            if (card == null || loadout == null)
                return;
            if (card.Kind == ELevelUpCardKind.Stat)
            {
                if (card.StatTrait == EPartTrait.Damage)
                {
                    if (skills != null)
                        skills.AttackPower *= 1f + card.StatValue;
                    return;
                }
                loadout.AddRunBonus(card.StatTrait, card.StatValue);
                return;
            }
            if (card.Part != null)
                loadout.TryEquip(card.Part);
        }

        // 하네스 자동 선택 — 보통 플레이어처럼: 체력이 절반 아래면 체력, 세 번에 한 번은 능력(공격 · 체력 중 덜 받은 쪽),
        // 나머지는 앰프 > 새 무기 > 강화. 능력 카드만 남으면 공격력 우선
        public static int AutoPick(List<LevelUpCard> cards, int level, float hpRatio, int damagePicks, int hpPicks)
        {
            if (hpRatio < 0.5f)
            {
                int hp = Find(cards, EPartTrait.MaxHp);
                if (hp >= 0)
                    return hp;
            }
            if (level % 3 == 0)
            {
                int s = Find(cards, damagePicks <= hpPicks ? EPartTrait.Damage : EPartTrait.MaxHp);
                if (s < 0)
                    s = Find(cards, damagePicks <= hpPicks ? EPartTrait.MaxHp : EPartTrait.Damage);
                if (s >= 0)
                    return s;
            }
            int best = 0;
            int bestScore = -1;
            for (int i = 0; i < cards.Count; i++)
            {
                int sc = Score(cards[i]);
                if (sc > bestScore)
                {
                    best = i;
                    bestScore = sc;
                }
            }
            return best;
        }

        private static int Find(List<LevelUpCard> cards, EPartTrait trait)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].Kind == ELevelUpCardKind.Stat && cards[i].StatTrait == trait)
                    return i;
            }
            return -1;
        }

        private static int Score(LevelUpCard c)
        {
            switch (c.Kind)
            {
                case ELevelUpCardKind.Amp: return 5;
                case ELevelUpCardKind.NewWeapon: return 4;
                case ELevelUpCardKind.Upgrade: return 3;
                default: return c.StatTrait == EPartTrait.Damage ? 2 : (c.StatTrait == EPartTrait.MaxHp ? 2 : 1);
            }
        }

        private static void Add(LevelUpCard card, float weight)
        {
            if (card.Kind != ELevelUpCardKind.Stat && card.Part == null)
                return;
            _pool.Add(card);
            _weights.Add(weight);
        }

        private static int PickWeighted()
        {
            float total = 0f;
            for (int i = 0; i < _weights.Count; i++)
                total += _weights[i];
            float r = Random.value * total;
            for (int i = 0; i < _weights.Count; i++)
            {
                r -= _weights[i];
                if (r <= 0f)
                    return i;
            }
            return _weights.Count - 1;
        }
    }
}
