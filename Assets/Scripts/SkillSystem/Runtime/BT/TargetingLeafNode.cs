using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.TargetSelf,
             ESkillNodeType.TargetNearest,
             ESkillNodeType.TargetFarthest,
             ESkillNodeType.TargetRandom,
             ESkillNodeType.TargetAll,
             ESkillNodeType.TargetNearestForward,
             ESkillNodeType.TargetCone)]
    public sealed class TargetingLeafNode : BTNode
    {
        public TargetingLeafNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            TargetingResolver.Resolve(Data, ctx.LevelData, ctx);
            return EBTStatus.Success;
        }
    }
}
