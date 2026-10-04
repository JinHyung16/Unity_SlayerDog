using System;
using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Parts
{
    public class PartLoadout : BaseBehaviour
    {
        public const int SlotCount = 6;

        [SerializeField] private SkillObject _skills;

        [Tooltip("이 캐릭터의 고유 스킬 소유 번호. 1=시바 2=코기 3=그레이하운드. 0 이면 고유 스킬 없음.")]
        [SerializeField] private int _characterIndex = 1;

        private readonly Dictionary<int, PartInstance> _equipped = new Dictionary<int, PartInstance>(SlotCount);
        private readonly List<PartInstance> _order = new List<PartInstance>(SlotCount);

        private int _uniqueSkillId;

        private readonly List<SynergyData> _activeSynergies = new List<SynergyData>(8);
        private readonly Dictionary<ESkillFamily, int> _familyCount = new Dictionary<ESkillFamily, int>(6);
        private readonly Dictionary<int, int> _optionCount = new Dictionary<int, int>(8);
        private readonly HashSet<EDebuffType> _appliedDebuffs = new HashSet<EDebuffType>();

        private Damageable _hp;
        private Jinhyeong_Character.CharacterMotor _motor;

        public float LifestealPerKill { get; private set; }

        public const float LifestealCapPerSecond = 0.0145f;

        public const float BaseLifestealPerKill = 1.0f;

        private float _healBudget;
        private float _budgetStamp;

        public float MaxHpMul { get; private set; } = 1f;

        public float MoveSpeedMul { get; private set; } = 1f;

        public event Action<PartInstance> OnPartChanged;

        public int CharacterIndex { get { return _characterIndex; } set { _characterIndex = value; } }
        public int EquippedCount { get { return _order.Count; } }
        public IReadOnlyList<PartInstance> Equipped { get { return _order; } }
        public IReadOnlyList<SynergyData> ActiveSynergies { get { return _activeSynergies; } }

        public ESkillFamily PreferredFamily { get; private set; } = ESkillFamily.None;

        public bool IsFull { get { return _order.Count >= SlotCount; } }

        public bool Has(int skillId)
        {
            return _equipped.ContainsKey(skillId);
        }

        // 레벨업 3택1 능력치 카드로 쌓은 몸 보너스 (판 하나 동안). ApplyBodyTraits 가 옵션 · 시너지 · 성좌와 함께 더한다
        private float _runHpAdd;
        private float _runSpeedAdd;
        private float _runSteal;

        public void AddRunBonus(EPartTrait trait, float value)
        {
            switch (trait)
            {
                case EPartTrait.MaxHp: _runHpAdd += value; break;
                case EPartTrait.MoveSpeed: _runSpeedAdd += value; break;
                case EPartTrait.Lifesteal: _runSteal += value; break;
                default: return;
            }
            ApplyBodyTraits();
        }

        public PartInstance GetBySkill(int skillId)
        {
            return _equipped.TryGetValue(skillId, out PartInstance v) ? v : null;
        }

        private void Awake()
        {
            if (_skills == null)
                _skills = GetComponent<SkillObject>();
            if (_hp == null)
                _hp = GetComponent<Damageable>();
            if (_motor == null)
                _motor = GetComponent<Jinhyeong_Character.CharacterMotor>();
        }

        public void BeginRun()
        {
            ClearAll();

            // 고른 주인공의 월드 스프라이트로 갈아 끼운다 (프리팹은 하나)
            Jinhyeong_View.HeroSkin skin = GetComponent<Jinhyeong_View.HeroSkin>();
            if (skin != null)
                skin.Apply(_characterIndex);

            if (PartRegistry.Count == 0)
            {
                PartRegistry.Load();
                PartOptionRegistry.Load();
                SynergyRegistry.Load();
            }

            PartData unique = PartRegistry.GetUniqueFor(_characterIndex);
            if (unique == null)
                return;

            PartInstance inst = PartInstance.Create(unique, EPartGrade.Legendary);
            _uniqueSkillId = unique.SkillId;
            PreferredFamily = unique.Family;
            EquipInternal(inst);
        }

        public void ClearAll()
        {
            for (int i = 0; i < _order.Count; i++)
            {
                if (_skills != null && _order[i] != null)
                    _skills.RemoveAutoSkill(_order[i].SkillId);
            }
            _equipped.Clear();
            _order.Clear();
            _activeSynergies.Clear();
            _uniqueSkillId = 0;
            PreferredFamily = ESkillFamily.None;
            _runHpAdd = 0f;
            _runSpeedAdd = 0f;
            _runSteal = 0f;
        }

        // 기본 공격(고유 파츠) — 스피커가 «앰프»로 다시 줄 때 옵션이 아직 들어갈 자리가 있는지
        public PartInstance UniquePart
        {
            get
            {
                return _uniqueSkillId != 0 && _equipped.TryGetValue(_uniqueSkillId, out PartInstance u) ? u : null;
            }
        }

        public bool TryEquip(PartInstance inst)
        {
            if (inst == null || inst.Data == null)
                return false;

            if (_equipped.TryGetValue(inst.SkillId, out PartInstance same) && same != null)
            {
                same.Merge(inst);
                RebuildSynergies();
                if (OnPartChanged != null)
                    OnPartChanged.Invoke(same);
                return true;
            }

            if (IsFull)
            {
                PartInstance weakest = FindWeakestReplaceable();
                if (weakest == null)
                    return false;
                if (weakest.Options.Count >= inst.Options.Count)
                    return false;

                if (_skills != null)
                    _skills.RemoveAutoSkill(weakest.SkillId);
                _equipped.Remove(weakest.SkillId);
                _order.Remove(weakest);
            }

            EquipInternal(inst);
            return true;
        }

        private PartInstance FindWeakestReplaceable()
        {
            PartInstance best = null;
            for (int i = 0; i < _order.Count; i++)
            {
                PartInstance p = _order[i];
                if (p == null || p.SkillId == _uniqueSkillId)
                    continue;
                if (best == null || p.Options.Count < best.Options.Count)
                    best = p;
            }
            return best;
        }

        private void EquipInternal(PartInstance inst)
        {
            _equipped[inst.SkillId] = inst;
            _order.Add(inst);

            if (_skills != null)
                _skills.AddPartSkill(inst.SkillId, inst.BuildModifier());

            RebuildSynergies();

            if (OnPartChanged != null)
                OnPartChanged.Invoke(inst);
        }

        private void RebuildSynergies()
        {
            if (_skills == null)
                return;

            _familyCount.Clear();
            _optionCount.Clear();
            _appliedDebuffs.Clear();
            for (int i = 0; i < _order.Count; i++)
            {
                PartInstance inst = _order[i];
                if (inst == null || inst.Data == null)
                    continue;
                ESkillFamily f = inst.Data.Family;
                _familyCount.TryGetValue(f, out int c);
                _familyCount[f] = c + 1;
                _optionCount[inst.SkillId] = inst.Options.Count;
                if (inst.Data.AppliesDebuff != EDebuffType.None)
                    _appliedDebuffs.Add(inst.Data.AppliesDebuff);
            }

            SynergyRegistry.Collect(_familyCount, _optionCount, _activeSynergies, _appliedDebuffs);

            for (int i = 0; i < _order.Count; i++)
            {
                PartInstance inst = _order[i];
                if (inst == null || inst.Data == null)
                    continue;

                SkillModifier m = inst.BuildModifier();

                AddStarTraits(m, inst.Data.Family);
                for (int k = 0; k < _activeSynergies.Count; k++)
                {
                    SynergyData sy = _activeSynergies[k];
                    if (SynergyRegistry.AppliesTo(sy, inst.SkillId, inst.Data.Family) == false)
                        continue;

                    if (sy.VsDebuff != EDebuffType.None)
                    {
                        m.VsDebuff = sy.VsDebuff;
                        m.VsDebuffDamageMul += sy.Value;
                    }
                    else
                    {
                        m.AddTrait(sy.Trait, sy.Value);
                    }
                }
                _skills.SetPartModifier(inst.SkillId, m);
            }

            ApplyBodyTraits();
        }

        private static void AddStarTraits(SkillModifier m, ESkillFamily family)
        {
            for (int i = 0; i < StarSkillTraits.Length; i++)
            {
                EPartTrait t = StarSkillTraits[i];
                float v = Jinhyeong_Constellation.ConstellationManager.Global(t)
                          + Jinhyeong_Constellation.ConstellationManager.Family(family, t);
                if (v != 0f)
                    m.AddTrait(t, v);
            }
        }

        private static readonly EPartTrait[] StarSkillTraits =
        {
            EPartTrait.Damage, EPartTrait.Cooldown, EPartTrait.CritChance, EPartTrait.CritDamage,
            EPartTrait.Area, EPartTrait.Count, EPartTrait.Duration
        };

        private void ApplyBodyTraits()
        {
            float hpAdd = 0f;
            float speedAdd = 0f;
            float steal = 0f;

            for (int i = 0; i < _order.Count; i++)
            {
                PartInstance inst = _order[i];
                if (inst == null)
                    continue;
                for (int k = 0; k < inst.Options.Count; k++)
                {
                    PartOptionData o = inst.Options[k];
                    if (o == null)
                        continue;
                    switch (o.Trait)
                    {
                        case EPartTrait.MaxHp: hpAdd += o.Value; break;
                        case EPartTrait.MoveSpeed: speedAdd += o.Value; break;
                        case EPartTrait.Lifesteal: steal += o.Value; break;
                    }
                }
            }

            for (int k = 0; k < _activeSynergies.Count; k++)
            {
                SynergyData sy = _activeSynergies[k];
                if (sy == null)
                    continue;
                switch (sy.Trait)
                {
                    case EPartTrait.MaxHp: hpAdd += sy.Value; break;
                    case EPartTrait.MoveSpeed: speedAdd += sy.Value; break;
                    case EPartTrait.Lifesteal: steal += sy.Value; break;
                }
            }

            hpAdd += _runHpAdd;
            speedAdd += _runSpeedAdd;
            steal += _runSteal;

            hpAdd += Jinhyeong_Constellation.ConstellationManager.Body(EPartTrait.MaxHp);
            speedAdd += Jinhyeong_Constellation.ConstellationManager.Body(EPartTrait.MoveSpeed);
            steal += Jinhyeong_Constellation.ConstellationManager.Body(EPartTrait.Lifesteal);

            MaxHpMul = 1f + hpAdd;
            MoveSpeedMul = 1f + speedAdd;
            LifestealPerKill = BaseLifestealPerKill + steal;

            if (_hp != null)
                _hp.SetBonusMaxHpMultiplier(MaxHpMul);
            if (_motor != null)
                _motor.SpeedMultiplier = MoveSpeedMul;
        }

        public void GrantLifesteal()
        {
            if (LifestealPerKill <= 0f || _hp == null)
                return;

            if (LifestealPerKill < BaseLifestealPerKill)
                LifestealPerKill = BaseLifestealPerKill;

            float now = Time.time;
            float cap = _hp.MaxHp * LifestealCapPerSecond;
            float elapsed = now - _budgetStamp;
            if (elapsed > 0f)
            {
                _healBudget = Mathf.Min(cap, _healBudget + cap * elapsed);
                _budgetStamp = now;
            }

            float amount = Mathf.Min(LifestealPerKill, _healBudget);
            if (amount <= 0f)
                return;

            _healBudget -= amount;
            _hp.Heal(amount);
        }

        public bool IsUpgrade(PartInstance candidate)
        {
            if (candidate == null)
                return false;

            if (_equipped.TryGetValue(candidate.SkillId, out PartInstance same) && same != null)
                return same.Options.Count < PartInstance.MaxOptions;

            if (IsFull == false)
                return true;

            PartInstance weakest = FindWeakestReplaceable();
            return weakest != null && candidate.Options.Count > weakest.Options.Count;
        }

        public bool CanUse(PartData data)
        {
            return data != null && data.Owner == 0;
        }
    }
}
