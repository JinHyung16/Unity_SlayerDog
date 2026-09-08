using System;
using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Parts
{
    /// <summary>
    /// 들고 있는 스킬 6개.
    ///
    /// 스킬을 얻으면 자동 발동하고, 붙은 증강이 그 스킬만 강화한다. 같은 스킬을 또 얻으면
    /// 증강이 **합쳐져** 깊어진다 — 이게 「1~2 스킬로 밀고 가는」 축이다.
    ///
    /// [사고] 처음엔 몸 부위(입·발·꼬리·머리·목·등) 6칸으로 제한했다. 그런데 계열이 부위에
    /// 몰려 있어서(시선 3종이 전부 머리) **계열 시너지가 원천적으로 도달 불가능**해졌다.
    /// 조합을 만들라면서 조합을 막는 제약이라 부위 제한을 없애고 슬롯 수만 남겼다.
    /// 부위는 이제 표시용 분류일 뿐이다.
    /// </summary>
    public class PartLoadout : BaseBehaviour
    {
        public const int SlotCount = 6;

        [SerializeField] private SkillObject _skills;

        [Tooltip("이 캐릭터의 고유 스킬 소유 번호. 1=시바 2=코기 3=그레이하운드. 0 이면 고유 스킬 없음.")]
        [SerializeField] private int _characterIndex = 1;

        // 스킬 id 로 건다 — 같은 스킬을 두 번 들 일이 없고, 합치기 판정이 곧 이 키의 존재 확인이다.
        private readonly Dictionary<int, PartInstance> _equipped = new Dictionary<int, PartInstance>(SlotCount);
        private readonly List<PartInstance> _order = new List<PartInstance>(SlotCount);

        private int _uniqueSkillId;

        private readonly List<SynergyData> _activeSynergies = new List<SynergyData>(8);
        private readonly Dictionary<ESkillFamily, int> _familyCount = new Dictionary<ESkillFamily, int>(6);
        private readonly Dictionary<int, int> _optionCount = new Dictionary<int, int>(8);
        private readonly HashSet<EDebuffType> _appliedDebuffs = new HashSet<EDebuffType>();

        // 캐릭터 전역 특성. 스킬별 보정(SkillModifier)과 달리 몸에 한 번만 걸린다 —
        // 6개 스킬에 각각 곱하면 최대 체력이 6배가 된다.
        private Damageable _hp;
        private Jinhyeong_Character.CharacterMotor _motor;

        /// <summary>적 1마리 처치당 회복량. 공격 성장이 곧 생존이 되는 축이다.</summary>
        public float LifestealPerKill { get; private set; }

        /// <summary>
        /// 흡혈의 초당 회복 상한 — 최대 체력의 몇 배까지 1초에 채울 수 있나.
        ///
        /// [실측] 상한이 없으면 흡혈이 처치 수에 그대로 비례한다. 후반엔 분당 처치가
        /// 240 을 넘어 회복이 분당 480 이 되고, 라운드 7 까지 피해를 한 번도 안 입었다.
        /// 상한을 걸면 회복이 항상 「최대 체력 대비 일정 비율」이라, 적 피해를 올리는 것만으로
        /// 난이도가 정직하게 움직인다.
        /// </summary>
        public const float LifestealCapPerSecond = 0.0145f;

        /// <summary>
        /// 아무것도 안 뽑아도 들어가는 기본 처치 회복.
        ///
        /// [실측] 이게 없을 때 흡혈은 **0 아니면 100** 이었다. 옵션 하나만 나와도 초당 상한을
        /// 거의 채워 HP 가 100% 에 고정되고, 못 뽑으면 라운드 9~10 에서 무너진다.
        /// 같은 데이터로 세 번 재면 클리어·사망·무풍이 다 나왔다 — 결과가 뽑기 운에 갈린다.
        /// 바닥을 깔아 굶는 판을 없애고, 옵션당 값과 상한을 낮춰 면역인 판도 없앤다.
        /// </summary>
        public const float BaseLifestealPerKill = 1.0f;

        private float _healBudget;
        private float _budgetStamp;

        /// <summary>지금 걸려 있는 최대 체력 배수. HUD/실측용.</summary>
        public float MaxHpMul { get; private set; } = 1f;

        /// <summary>지금 걸려 있는 이동속도 배수. HUD/실측용.</summary>
        public float MoveSpeedMul { get; private set; } = 1f;

        /// <summary>구성이 바뀔 때마다 발생. HUD 가 구독한다.</summary>
        public event Action<PartInstance> OnPartChanged;

        public int CharacterIndex { get { return _characterIndex; } set { _characterIndex = value; } }
        public int EquippedCount { get { return _order.Count; } }
        public IReadOnlyList<PartInstance> Equipped { get { return _order; } }
        public IReadOnlyList<SynergyData> ActiveSynergies { get { return _activeSynergies; } }

        /// <summary>
        /// 이 캐릭터가 밀어주는 계열. **고유 스킬의 계열을 그대로 쓴다** —
        /// 캐릭터 정체성이 곧 빌드 방향이고, 별도 데이터를 만들 필요가 없다.
        /// </summary>
        public ESkillFamily PreferredFamily { get; private set; } = ESkillFamily.None;

        public bool IsFull { get { return _order.Count >= SlotCount; } }

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

        /// <summary>런 시작. 고유 스킬 1개만 들고 출발한다.</summary>
        public void BeginRun()
        {
            ClearAll();

            // 스킬 레지스트리 로드가 비동기라 테이블이 아직 비어 있을 수 있다.
            if (PartRegistry.Count == 0)
            {
                PartRegistry.Load();
                PartOptionRegistry.Load();
                SynergyRegistry.Load();
            }

            PartData unique = PartRegistry.GetUniqueFor(_characterIndex);
            if (unique == null)
                return;

            // 고유 스킬은 항상 전설(증강 3개)로 준다 — 캐릭터 정체성이라 매판 흔들리면 안 된다.
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
        }

        /// <summary>
        /// 주운 스킬을 장착한다.
        ///   이미 들고 있으면 → 증강을 **합친다**(깊어진다)
        ///   빈 슬롯이 있으면 → 새로 든다(넓어진다)
        ///   꽉 찼으면      → 가장 얕은 것과 교체한다. 고유 스킬은 절대 버리지 않는다
        /// </summary>
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
                    return false;   // 지금 것이 더 깊으면 굳이 바꾸지 않는다

                if (_skills != null)
                    _skills.RemoveAutoSkill(weakest.SkillId);
                _equipped.Remove(weakest.SkillId);
                _order.Remove(weakest);
            }

            EquipInternal(inst);
            return true;
        }

        /// <summary>교체 대상 중 가장 얕은 것. 고유 스킬은 후보에서 뺀다.</summary>
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

            // 구성이 바뀌면 시너지 조건이 바뀐다. 새로 든 것만이 아니라 **전부** 다시 계산해야 한다 —
            // 계열 시너지는 이미 들고 있던 스킬에도 걸리기 때문이다.
            RebuildSynergies();

            if (OnPartChanged != null)
                OnPartChanged.Invoke(inst);
        }

        /// <summary>
        /// 발동 중인 시너지를 다시 구하고, 들고 있는 모든 스킬의 보정을 새로 만든다.
        /// 스킬이 6개뿐이라 통째로 다시 만드는 게 부분 갱신보다 안전하고 충분히 싸다.
        /// </summary>
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

                // 성좌는 런 밖에서 산 것이라 파츠보다 «먼저» 실린다. 잔가지는 전역, 성단·키스톤은 계열 한정이다.
                AddStarTraits(m, inst.Data.Family);
                for (int k = 0; k < _activeSynergies.Count; k++)
                {
                    SynergyData sy = _activeSynergies[k];
                    if (SynergyRegistry.AppliesTo(sy, inst.SkillId, inst.Data.Family) == false)
                        continue;

                    // 연계는 특성이 아니라 **대상 조건부 배수**로 실린다.
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

        /// <summary>
        /// 성좌가 주는 스킬 특성. 전역분과 그 계열분을 더해 보정에 싣는다.
        /// 몸 특성(체력·이동속도·회복)은 여기서 빼고 <see cref="ApplyBodyTraits"/> 가 따로 센다 —
        /// 스킬 보정에 최대 체력을 실으면 아무 데도 안 걸리고 조용히 사라진다.
        /// </summary>
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

        /// <summary>성좌가 스킬 보정으로 실을 수 있는 특성. 몸 특성은 여기 없다.</summary>
        private static readonly EPartTrait[] StarSkillTraits =
        {
            EPartTrait.Damage, EPartTrait.Cooldown, EPartTrait.CritChance, EPartTrait.CritDamage,
            EPartTrait.Area, EPartTrait.Count, EPartTrait.Duration
        };

        /// <summary>
        /// 몸에 걸리는 특성(체력·회복·이동속도)을 합쳐 한 번만 적용한다.
        ///
        /// 매번 0 에서 다시 합산한다 — 누적하면 구성이 바뀔 때마다 공짜로 불어난다.
        /// 시너지도 같은 특성을 줄 수 있어 여기서 함께 센다.
        /// </summary>
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

            // 몸에 붙는 특성은 계열을 안 탄다 — 스킬이 아니라 개가 튼튼해지는 것이다.
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

        /// <summary>
        /// 처치 회복을 지급한다. 초당 예산을 넘는 분은 버린다 —
        /// 예산은 최대 체력 비례라, 체력이 커지면 회복도 같이 커진다.
        /// </summary>
        public void GrantLifesteal()
        {
            if (LifestealPerKill <= 0f || _hp == null)
                return;

            // 런이 시작되기 전(BeginRun 전)에는 보정이 아직 안 돌아 기본값이 비어 있다.
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

        /// <summary>주울 가치가 있는지. 같은 스킬이면 합쳐져 깊어지므로 상한 전까지 언제나 이득이다.</summary>
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

        /// <summary>드랍 후보로 쓸 수 있는지. 남의 캐릭터 고유 스킬은 나오지 않는다.</summary>
        public bool CanUse(PartData data)
        {
            return data != null && data.Owner == 0;
        }
    }
}
