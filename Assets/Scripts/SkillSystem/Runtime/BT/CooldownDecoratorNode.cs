using UnityEngine;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.Cooldown)]
    public sealed class CooldownDecoratorNode : BTNode
    {
        private float _nextReady;

        public CooldownDecoratorNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            if (ctx.ManualCast == false && Time.time < _nextReady)
                return EBTStatus.Failure;

            EBTStatus r = RunChildrenAsSequence(ctx);
            if (r == EBTStatus.Success)
            {
                float cd = Data != null ? Data.GetFloat(ESkillParamKey.Cooldown, ctx.LevelData, 0f, ctx.Modifier) : 0f;
                _nextReady = Time.time + cd;
            }
            return r;
        }
    }
}
