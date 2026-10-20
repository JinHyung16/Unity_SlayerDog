using Jinhyeong_Common;
using Jinhyeong_GameData;

namespace Jinhyeong_Constellation
{

    public static class TraitText
    {
        public static string Short(EPartTrait trait, float value)
        {
            if (trait == EPartTrait.Count)
            {
                return $"{Name(trait)} +{value:0}";
            }
            return $"{Name(trait)} +{value * 100f:0.#}%";
        }

        public static string Long(EPartTrait trait, float value, bool familyScoped, string familyName)
        {
            string amount = trait == EPartTrait.Count
                ? $"+{value:0}"
                : $"+{value * 100f:0.#}%";

            if (familyScoped)
            {
                return Loc.F("Trait_LongFamily", "{0} 계열 {1} {2}", familyName, Name(trait), amount);
            }
            return Loc.F("Trait_LongAll", "모든 스킬 {0} {1}", Name(trait), amount);
        }

        public static string Name(EPartTrait trait)
        {
            switch (trait)
            {
                case EPartTrait.Damage: return Loc.T("Trait_Damage", "피해");
                case EPartTrait.Cooldown: return Loc.T("Trait_Cooldown", "재사용 단축");
                case EPartTrait.CritChance: return Loc.T("Trait_CritChance", "치명타 확률");
                case EPartTrait.CritDamage: return Loc.T("Trait_CritDamage", "치명타 피해");
                case EPartTrait.Area: return Loc.T("Trait_Area", "범위");
                case EPartTrait.Count: return Loc.T("Trait_Count", "발사체");
                case EPartTrait.Duration: return Loc.T("Trait_Duration", "지속");
                case EPartTrait.MaxHp: return Loc.T("Trait_MaxHp", "최대 체력");
                case EPartTrait.Lifesteal: return Loc.T("Trait_Lifesteal", "처치 회복");
                case EPartTrait.MoveSpeed: return Loc.T("Trait_MoveSpeed", "이동 속도");
                default: return Loc.T("Trait_Default", "특성");
            }
        }
    }
}
