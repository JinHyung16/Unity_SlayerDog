using UnityEngine;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.Chance)]
    public sealed class ChanceDecoratorNode : BTNode
    {
        public ChanceDecoratorNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            if (ctx.ManualCast == false)
            {
                float chance = Data != null ? Data.GetFloat(ESkillParamKey.Chance, ctx.LevelData, 100f) : 100f;
                if (chance < 100f && Random.value * 100f > chance)
                    return EBTStatus.Failure;
            }
            return RunChildrenAsSequence(ctx);
        }
    }
}
