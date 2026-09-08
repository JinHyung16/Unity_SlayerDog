using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Constellation
{

    /// <summary>
    /// 노드의 «표시 정보»를 한 곳에서 만든다.
    ///
    /// 노드가 특성을 가리키면 그 특성이 이미 이름과 설명을 갖고 있다 — 노드에 또 적지 않는다.
    /// 두 곳에 적으면 값을 한 번 조정할 때마다 이름이 조용히 거짓말이 된다.
    /// 특성이 없는 노드(코어 · 키스톤)만 자기 이름을 직접 든다.
    /// </summary>
    public static class ConstellationNaming
    {
        /// <summary>이 종류는 이름을 «직접» 든다. 저장된 값을 덮어쓰지 않는다.</summary>
        public static bool HoldsOwnName(EConstellationNodeType type)
        {
            return type == EConstellationNodeType.Core
                   || type == EConstellationNodeType.Keystone;
        }

        /// <summary>
        /// 노드의 이름·설명을 다시 만든다. 도구에서 특성이나 값을 고친 직후,
        /// 그리고 저장을 읽어 들인 직후에 부른다.
        /// </summary>
        public static void Apply(ConstellationNode node)
        {
            if (node == null || HoldsOwnName(node.Type))
            {
                return;
            }

            if (node.Type == EConstellationNodeType.Skill)
            {
                PartData part = PartRegistry.GetBySkillId(node.SkillId);
                node.DisplayName = part != null ? part.DisplayName : $"스킬 {node.SkillId}";
                node.Description = "해금 — 필드에서 이 파츠가 나오기 시작한다";
                return;
            }

            if (node.Type == EConstellationNodeType.Gate)
            {
                node.DisplayName = "관문";
                node.Description = $"클리어 {node.NeedClear}회 이상이어야 지나간다";
                return;
            }

            if (node.Trait == EPartTrait.None)
            {
                return;
            }

            string family = FamilyName(node.Family);
            node.DisplayName = node.FamilyScoped
                ? $"{family} {TraitText.Short(node.Trait, node.Value)}"
                : TraitText.Short(node.Trait, node.Value);
            node.Description = TraitText.Long(node.Trait, node.Value, node.FamilyScoped, family);
        }

        /// <summary>계열의 한글 이름. 계열 표가 들고 있다 — 코드에 또 적지 않는다.</summary>
        public static string FamilyName(ESkillFamily family)
        {
            for (int i = 0; i < ConstellationRegionRegistry.Count; i++)
            {
                ConstellationRegionData r = ConstellationRegionRegistry.All[i];
                if (r.Family == family)
                {
                    return r.DisplayName;
                }
            }
            return family.ToString();
        }
    }
}
