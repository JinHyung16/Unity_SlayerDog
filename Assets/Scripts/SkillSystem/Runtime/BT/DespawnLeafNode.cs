using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.DespawnAfterTime,
             ESkillNodeType.DespawnAfterHits,
             ESkillNodeType.DespawnAfterBounces,
             ESkillNodeType.DespawnOnWall,
             Inert = new[] { ESkillNodeType.DespawnAfterBounces, ESkillNodeType.DespawnOnWall })]
    public sealed class DespawnLeafNode : BTNode
    {
        public DespawnLeafNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            ctx.DespawnNode = Data;
            return EBTStatus.Success;
        }
    }
}
