using UnityEngine;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.BuffSelf)]
    public sealed class BuffSelfLeafNode : BTNode
    {
        public BuffSelfLeafNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            if (ctx.Caster == null)
                return EBTStatus.Failure;

            int buffId = Data.GetInt(ESkillParamKey.BuffId, ctx.LevelData, 0);
            if (buffId <= 0)
                return EBTStatus.Failure;

            SkillBuffData data = SkillBuffRegistry.GetBuff(buffId);
            if (data == null)
            {
                Debug.LogWarning($"[BuffSelfLeafNode] BuffId={buffId} not in registry");
                return EBTStatus.Failure;
            }
            Damageable self = ctx.Caster.GetComponent<Damageable>();
            if (self == null)
            {
                Debug.LogWarning("[BuffSelfLeafNode] 캐스터에 Damageable 이 없어 버프를 걸 수 없습니다");
                return EBTStatus.Failure;
            }

            self.Status.ApplyBuff(data, ctx.Caster);
            return EBTStatus.Success;
        }
    }
}
