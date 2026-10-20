using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Constellation
{

    public static class ConstellationNaming
    {
        public static bool HoldsOwnName(EConstellationNodeType type)
        {
            return type == EConstellationNodeType.Core
                   || type == EConstellationNodeType.Keystone;
        }

        public static void Apply(ConstellationNode node)
        {
            if (node == null || HoldsOwnName(node.Type))
            {
                return;
            }

            if (node.Type == EConstellationNodeType.Skill)
            {
                PartData part = PartRegistry.GetBySkillId(node.SkillId);
                node.DisplayName = part != null ? part.DisplayName : Loc.F("Star_SkillFallbackName", "스킬 {0}", node.SkillId);
                node.Description = Loc.T("Star_SkillUnlockDesc", "해금 — 필드에서 이 파츠가 나오기 시작합니다");
                return;
            }

            if (node.Type == EConstellationNodeType.Gate)
            {
                node.DisplayName = Loc.T("Star_GateName", "관문");
                node.Description = Loc.F("Star_GateDesc", "클리어 {0}회 이상이어야 지나갈 수 있습니다", node.NeedClear);
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
