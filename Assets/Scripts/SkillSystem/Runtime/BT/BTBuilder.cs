using System;
using UnityEngine;

namespace Jinhyeong_SkillSystem.BT
{

    public static class BTBuilder
    {
        public static BTNode Build(SkillNodeData root)
        {
            if (root == null)
                return null;

            BTNode self = Create(root);
            if (self == null)
                return null;

            for (int i = 0; i < root.Children.Count; i++)
            {
                BTNode child = Build(root.Children[i]);
                if (child == null)
                    continue;
                self.Children.Add(child);
            }
            return self;
        }

        private static BTNode Create(SkillNodeData n)
        {
            Func<SkillNodeData, BTNode> factory = BTNodeRegistry.Find(n.NodeType);
            if (factory != null)
                return factory(n);

            Debug.LogWarning($"[BTBuilder] 미지원 node_type '{n.NodeType}' (node {n.NodeId}) — 빈 노드로 통과 처리");
            return new SequenceNode(n);
        }
    }
}
