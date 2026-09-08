using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.Inverter)]
    public sealed class InverterNode : BTNode
    {
        public InverterNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            if (Children.Count == 0)
                return EBTStatus.Failure;
            EBTStatus r = RunChildrenAsSequence(ctx);
            return r == EBTStatus.Success ? EBTStatus.Failure : EBTStatus.Success;
        }
    }
}
