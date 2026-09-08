using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.HitSingle,
             ESkillNodeType.HitArea,
             ESkillNodeType.HitBeam,
             ESkillNodeType.HitChain,
             ESkillNodeType.HitDeathBurst)]
    public sealed class HitLeafNode : BTNode
    {
        public HitLeafNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            ctx.HitNode = Data;
            return EBTStatus.Success;
        }
    }
}
