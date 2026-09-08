using System;
using UnityEngine;

namespace Jinhyeong_SkillSystem.BT
{

    public static class BTBuilder
    {
        /// <summary>
        /// DB가 이미 중첩 트리로 주므로 그대로 따라 내려가며 실행 노드를 만든다.
        /// (예전 parent_id/order 재조립 단계는 없어졌다)
        /// </summary>
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

        /// <summary>
        /// 어느 클래스가 이 타입을 맡는지는 <see cref="BTNodeRegistry"/> 가 안다.
        /// 예전 switch 는 노드를 늘릴 때마다 같이 늘어나야 했고, 안 늘려도 컴파일은 통과했다.
        /// </summary>
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
