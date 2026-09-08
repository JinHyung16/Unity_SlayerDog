using Jinhyeong_GameData;

namespace Jinhyeong_Constellation
{

    /// <summary>
    /// 특성을 사람 말로 옮긴다. 노드 이름은 «값이 무엇인지»를 말해야 한다 —
    /// 「잔가지 A」 같은 이름은 트리를 보면서 무엇을 사는지 알 수 없게 만든다.
    ///
    /// 노드 이름을 데이터로 들고 있지 않는 이유가 이것이다. 200개의 이름을 표에 적으면
    /// 값을 한 번 조정할 때마다 이름 200개가 거짓말이 된다.
    /// </summary>
    public static class TraitText
    {
        /// <summary>노드 위에 얹는 짧은 표기. 칸이 작아 길면 잘린다.</summary>
        public static string Short(EPartTrait trait, float value)
        {
            if (trait == EPartTrait.Count)
            {
                return $"{Name(trait)} +{value:0}";
            }
            return $"{Name(trait)} +{value * 100f:0.#}%";
        }

        /// <summary>설명 줄. 계열 한정인지 아닌지가 여기서 갈린다 — 이게 안 보이면 왜 약한지 알 수 없다.</summary>
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
