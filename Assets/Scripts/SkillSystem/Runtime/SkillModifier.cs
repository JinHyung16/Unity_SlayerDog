using UnityEngine;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem
{
    public class SkillModifier
    {
        public static readonly SkillModifier None = new SkillModifier();

        public float DamageMul = 1f;
        public float CooldownMul = 1f;
        public float AreaMul = 1f;
        public float DurationMul = 1f;
        public float SpeedMul = 1f;

        public int CountAdd = 0;

        public float IntervalAdd = 0f;

        public float CritChanceAdd = 0f;
        public float CritDamageAdd = 0f;

        public float StatusValueAdd = 0f;

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
            CooldownMul = Mathf.Max(0.2f, CooldownMul);
        }

        public float Apply(ESkillParamKey key, float value)
        {
            switch (key)
            {
                case ESkillParamKey.Damage:
                    return value * DamageMul;

                case ESkillParamKey.Cooldown:
                    return value * CooldownMul;

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
                case ESkillParamKey.BurstCount:
                    // 값이 없어 int.MaxValue(무제한)로 들어온 것은 그대로 둔다 — 더하면 넘쳐서 1 이 된다
                    if (value == int.MaxValue)
                        return value;
                    return Mathf.Max(1, value + CountAdd);

                default:
                    return Mathf.RoundToInt(Apply(key, value));
            }
        }
    }
}
