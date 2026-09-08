using UnityEngine;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.TriggerOnTick, ESkillNodeType.TriggerOnAttack, ESkillNodeType.TriggerOnOreBreak,
             Inert = new[] { ESkillNodeType.TriggerOnOreBreak })]
    public sealed class TriggerNode : BTNode
    {
        private float _nextReady;

        public TriggerNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {

            if (ctx.ManualCast)
                return EBTStatus.Success;

            switch (Data.NodeType)
            {
                case ESkillNodeType.TriggerOnAttack:
                    if (ctx.AttackPending == false)
                        return EBTStatus.Failure;
                    return RollChance(ctx);

                case ESkillNodeType.TriggerOnTick:
                    if (Time.time < _nextReady)
                        return EBTStatus.Failure;
                    float cd = Data.GetFloat(ESkillParamKey.Cooldown, ctx.LevelData, 1f);
                    _nextReady = Time.time + Mathf.Max(0.05f, cd);
                    return RollChance(ctx);

                default:

                    return EBTStatus.Failure;
            }
        }

        private EBTStatus RollChance(SkillContext ctx)
        {
            if (Data.HasParam(ESkillParamKey.Chance) == false)
                return EBTStatus.Success;
            float chance = Data.GetFloat(ESkillParamKey.Chance, ctx.LevelData, 100f);
            if (chance < 100f && Random.value * 100f > chance)
                return EBTStatus.Failure;
            return EBTStatus.Success;
        }
    }
}
