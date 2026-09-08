using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.LaunchInstant,
             ESkillNodeType.LaunchStraight,
             ESkillNodeType.LaunchArc,
             ESkillNodeType.LaunchCurve,
             ESkillNodeType.LaunchHoming)]
    public sealed class LaunchLeafNode : BTNode
    {
        public LaunchLeafNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            return LaunchExecutor.Execute(Data, ctx) ? EBTStatus.Success : EBTStatus.Failure;
        }
    }
}
