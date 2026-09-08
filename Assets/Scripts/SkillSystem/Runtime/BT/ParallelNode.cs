using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [BTNode(ESkillNodeType.Parallel)]
    public sealed class ParallelNode : BTNode
    {
        public ParallelNode(SkillNodeData data) : base(data) { }

        public override EBTStatus Tick(SkillContext ctx)
        {
            bool requireAll = Data == null || Data.GetBool(ESkillParamKey.RequireAll, true);
            int success = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Tick(ctx) == EBTStatus.Success)
                    success++;
            }
            if (requireAll)
                return success == Children.Count ? EBTStatus.Success : EBTStatus.Failure;
            return success > 0 ? EBTStatus.Success : EBTStatus.Failure;
        }
    }
}
