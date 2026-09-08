using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.Sequence)]
    public sealed class SequenceNode : BTNode
    {
        public SequenceNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Tick(ctx) == EBTStatus.Failure)
                    return EBTStatus.Failure;
            }
            return EBTStatus.Success;
        }
    }
}
