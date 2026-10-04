using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    public static class BTNodeRegistry
    {
        private static Dictionary<ESkillNodeType, Func<SkillNodeData, BTNode>> _factories;

        private static List<ESkillNodeType> _unimplemented;

        public static Func<SkillNodeData, BTNode> Find(ESkillNodeType type)
        {
            EnsureScanned();
            return _factories.TryGetValue(type, out Func<SkillNodeData, BTNode> f) ? f : null;
        }

        public static IReadOnlyList<ESkillNodeType> Unimplemented
        {
            get
            {
                EnsureScanned();
                return _unimplemented;
            }
        }

        public static string Report()
        {
            EnsureScanned();

            StringBuilder sb = new StringBuilder();
            sb.Append("[BTNodeRegistry] 구현 ").Append(_factories.Count)
              .Append(" / enum ").Append(Enum.GetValues(typeof(ESkillNodeType)).Length - 1)
              .AppendLine("종 (None 제외)");

            if (_unimplemented.Count == 0)
            {
                sb.AppendLine("  미구현 없음");
                return sb.ToString();
            }

            sb.Append("  효과 없음 ").Append(_unimplemented.Count).AppendLine("종 — 데이터로 쓰면 조용히 아무 일도 안 한다:");
            for (int i = 0; i < _unimplemented.Count; i++)
            {
                sb.Append("    ").AppendLine(_unimplemented[i].ToString());
            }
            return sb.ToString();
        }

        private static void EnsureScanned()
        {
            if (_factories != null)
                return;

            _factories = new Dictionary<ESkillNodeType, Func<SkillNodeData, BTNode>>(48);
            _unimplemented = new List<ESkillNodeType>();
            HashSet<ESkillNodeType> inert = new HashSet<ESkillNodeType>();

            Type[] types = typeof(BTNode).Assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];
                if (t.IsAbstract || typeof(BTNode).IsAssignableFrom(t) == false)
                    continue;

                BTNodeAttribute attr = t.GetCustomAttribute<BTNodeAttribute>(false);
                if (attr == null)
                    continue;

                ConstructorInfo ctor = t.GetConstructor(new[] { typeof(SkillNodeData) });
                if (ctor == null)
                {
                    Debug.LogError($"[BTNodeRegistry] {t.Name} 에 (SkillNodeData) 생성자가 없다 — 등록하지 않는다");
                    continue;
                }

                if (attr.Inert != null)
                {
                    for (int k = 0; k < attr.Inert.Length; k++)
                        inert.Add(attr.Inert[k]);
                }

                Func<SkillNodeData, BTNode> factory = data => (BTNode)ctor.Invoke(new object[] { data });
                for (int k = 0; k < attr.Types.Length; k++)
                {
                    ESkillNodeType type = attr.Types[k];
                    if (_factories.TryGetValue(type, out Func<SkillNodeData, BTNode> already))
                    {
                        Debug.LogError($"[BTNodeRegistry] '{type}' 을 {t.Name} 이 중복 신고했다 — 먼저 등록된 쪽을 쓴다");
                        continue;
                    }
                    _factories[type] = factory;
                }
            }

            foreach (ESkillNodeType type in Enum.GetValues(typeof(ESkillNodeType)))
            {
                if (type == ESkillNodeType.None)
                    continue;
                if (_factories.ContainsKey(type) == false || inert.Contains(type))
                    _unimplemented.Add(type);
            }
        }
    }
}
