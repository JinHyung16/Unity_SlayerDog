using UnityEngine;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem
{
    /// <summary>
    /// 파츠 옵션이 만들어내는 「이 스킬만」 보정치.
    ///
    /// 왜 필요한가: 기존 증강은 전역 StatBuff 뿐이라 「개튼튼 치아 피해만 +30%」를 표현할 수 없었다.
    /// 파츠 옵션 100여 종이 전부 이 구조 위에 얹힌다.
    ///
    /// 적용 지점은 <see cref="SkillNodeData.GetFloat"/> / <see cref="SkillNodeData.GetInt"/> 한 곳뿐이다.
    /// 노드 데이터는 스킬 정의(공유물)라 여기에 값을 굽지 않고, 조회할 때 곱해서 내보낸다.
    ///
    /// 발사체처럼 프레임을 넘겨 살아 있는 것들은 <see cref="SkillConfig"/> 가 이 인스턴스를
    /// 들고 있다가 착탄 시점에 같은 보정을 쓴다 — 그래야 발사 후 파츠를 바꿔도 날아가던 탄이 안 변한다.
    /// </summary>
    public class SkillModifier
    {
        /// <summary>보정이 하나도 없을 때 쓰는 공유 인스턴스. null 체크를 줄이려고 둔다.</summary>
        public static readonly SkillModifier None = new SkillModifier();

        public float DamageMul = 1f;
        public float CooldownMul = 1f;
        public float AreaMul = 1f;
        public float DurationMul = 1f;
        public float SpeedMul = 1f;

        /// <summary>투사체·설치·연쇄 대상 등 「개수」에 더한다. 곱하면 1개짜리가 안 늘어난다.</summary>
        public int CountAdd = 0;

        /// <summary>지속 장판의 틱 간격을 줄인다(초). 「신나게」 같은 고유 옵션이 쓴다.</summary>
        public float IntervalAdd = 0f;

        public float CritChanceAdd = 0f;
        public float CritDamageAdd = 0f;

        /// <summary>디버프/버프 수치에 더하는 값 (슬로우 +10%p, 흡혈 +1%p 등 고유 옵션).</summary>
        public float StatusValueAdd = 0f;

        /// <summary>
        /// **대상 조건부** 피해 배수. 「둔화된 적에게 +35%」 같은 연계가 여기 실린다.
        /// 다른 보정과 달리 대상마다 달라지므로 조회 시점이 아니라 **피해를 줄 때** 곱한다.
        /// </summary>
        public EDebuffType VsDebuff = EDebuffType.None;
        public float VsDebuffDamageMul = 0f;

        public bool IsEmpty
        {
            get
            {
                return DamageMul == 1f && CooldownMul == 1f && AreaMul == 1f
                    && DurationMul == 1f && SpeedMul == 1f && CountAdd == 0
                    && IntervalAdd == 0f && CritChanceAdd == 0f
                    && CritDamageAdd == 0f && StatusValueAdd == 0f;
            }
        }

        public void Reset()
        {
            DamageMul = 1f;
            CooldownMul = 1f;
            AreaMul = 1f;
            DurationMul = 1f;
            SpeedMul = 1f;
            CountAdd = 0;
            IntervalAdd = 0f;
            CritChanceAdd = 0f;
            CritDamageAdd = 0f;
            StatusValueAdd = 0f;
            VsDebuff = EDebuffType.None;
            VsDebuffDamageMul = 0f;
        }

        /// <summary>
        /// 특성 하나를 더한다. 파츠 옵션과 시너지가 **같은 문**을 쓴다 —
        /// 문이 둘이면 한쪽에만 새 특성을 넣는 실수가 난다.
        /// </summary>
        public void AddTrait(EPartTrait trait, float value)
        {
            switch (trait)
            {
                case EPartTrait.Damage: DamageMul += value; break;
                case EPartTrait.Cooldown: CooldownMul -= value; break;
                case EPartTrait.CritChance: CritChanceAdd += value; break;
                case EPartTrait.CritDamage: CritDamageAdd += value; break;
                case EPartTrait.Area: AreaMul += value; break;
                case EPartTrait.Count: CountAdd += Mathf.RoundToInt(value); break;
                case EPartTrait.Duration: DurationMul += value; break;
            }
            // 쿨 감소가 100% 를 넘으면 매 프레임 발동해 게임이 터진다.
            CooldownMul = Mathf.Max(0.2f, CooldownMul);
        }

        /// <summary>파라미터 키별로 어떤 보정을 먹일지. 여기가 「특성 → 수치」 매핑의 유일한 정의처다.</summary>
        public float Apply(ESkillParamKey key, float value)
        {
            switch (key)
            {
                case ESkillParamKey.Damage:
                    return value * DamageMul;

                case ESkillParamKey.Cooldown:
                    return value * CooldownMul;

                // 범위 계열 — 반경/길이/폭/부채꼴 각도를 한 특성으로 묶는다
                case ESkillParamKey.Radius:
                case ESkillParamKey.Length:
                case ESkillParamKey.Width:
                case ESkillParamKey.Range:
                    return value * AreaMul;

                case ESkillParamKey.HalfAngle:
                case ESkillParamKey.Angle:
                    return value * AreaMul;

                case ESkillParamKey.Duration:
                    return value * DurationMul;

                case ESkillParamKey.Speed:
                case ESkillParamKey.MaxDistance:
                    return value * SpeedMul;

                case ESkillParamKey.DamageInterval:
                    // 간격은 줄어야 세진다. 0 이하로 내려가면 프레임마다 때려 게임이 터진다.
                    return Mathf.Max(0.05f, value + IntervalAdd);

                case ESkillParamKey.Value:
                    return value + StatusValueAdd;

                default:
                    return value;
            }
        }

        public int ApplyInt(ESkillParamKey key, int value)
        {
            switch (key)
            {
                case ESkillParamKey.Index:
                case ESkillParamKey.MaxPerTarget:
                case ESkillParamKey.MaxBounces:
                    return Mathf.Max(1, value + CountAdd);

                default:
                    return Mathf.RoundToInt(Apply(key, value));
            }
        }
    }
}
