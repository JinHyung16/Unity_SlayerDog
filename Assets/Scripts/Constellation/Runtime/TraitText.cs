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
                return $"{familyName} 계열 {Name(trait)} {amount}";
            }
            return $"모든 스킬 {Name(trait)} {amount}";
        }

        public static string Name(EPartTrait trait)
        {
            switch (trait)
            {
                case EPartTrait.Damage: return "피해";
                case EPartTrait.Cooldown: return "재사용 단축";
                case EPartTrait.CritChance: return "치명타 확률";
                case EPartTrait.CritDamage: return "치명타 피해";
                case EPartTrait.Area: return "범위";
                case EPartTrait.Count: return "발사체";
                case EPartTrait.Duration: return "지속";
                case EPartTrait.MaxHp: return "최대 체력";
                case EPartTrait.Lifesteal: return "처치 회복";
                case EPartTrait.MoveSpeed: return "이동 속도";
                default: return "특성";
            }
        }
    }
}
