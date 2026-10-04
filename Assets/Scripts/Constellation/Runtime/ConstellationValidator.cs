using System.Collections.Generic;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using UnityEngine;

namespace Jinhyeong_Constellation
{

    public static class ConstellationValidator
    {
        public const float OverlapDistance = 26f;

        public class Result
        {
            public readonly List<string> Errors = new List<string>(16);
            public readonly List<string> Warnings = new List<string>(16);

            public bool Ok
            {
                get { return Errors.Count == 0; }
            }
        }

        public static Result Validate(ConstellationGraph graph)
        {
            Result r = new Result();
            if (graph == null || graph.Nodes.Count == 0)
            {
                r.Errors.Add("노드가 하나도 없다");
                return r;
            }

            CheckNodes(graph, r);
            CheckEdges(graph, r);
            CheckReach(graph, r);
            CheckOverlap(graph, r);
            return r;
        }

        private static void CheckNodes(ConstellationGraph graph, Result r)
        {
            int core = 0;
            HashSet<int> seenIds = new HashSet<int>();
            HashSet<int> seenSkills = new HashSet<int>();

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                ConstellationNode n = graph.Nodes[i];

                if (seenIds.Add(n.Id) == false)
                {
                    r.Errors.Add($"id {n.Id} 이 두 번 쓰였다 — 세이브가 id 로 걸려 있어 둘이 같이 켜진다");
                }
                if (n.Type == EConstellationNodeType.Core)
                {
                    core++;
                }
                if (n.MaxLevel < 1)
                {
                    r.Errors.Add($"#{n.Id} 최대 레벨이 {n.MaxLevel} — 살 수 없는 노드다");
                }
                if (n.Cost < 0)
                {
                    r.Errors.Add($"#{n.Id} 값이 음수다");
                }
                if (n.Type != EConstellationNodeType.Core && n.Cost == 0)
                {
                    r.Warnings.Add($"#{n.Id}({n.DisplayName}) 값이 0 — 공짜로 열린다");
                }

                if (n.Type == EConstellationNodeType.Skill)
                {
                    if (PartRegistry.GetBySkillId(n.SkillId) == null)
                    {
                        r.Errors.Add($"#{n.Id} 스킬 노드가 없는 스킬 {n.SkillId} 을 가리킨다");
                    }
                    else if (seenSkills.Add(n.SkillId) == false)
                    {
                        r.Errors.Add($"스킬 {n.SkillId} 이 성좌에 두 번 있다 — 어느 쪽을 사도 같은 것이 열린다");
                    }
                }

                if (n.Type == EConstellationNodeType.Gate && n.NeedClear <= 0)
                {
                    r.Warnings.Add($"#{n.Id} 관문 조건이 0 — 막지 않는다");
                }
                if (n.FamilyScoped && n.Family == ESkillFamily.None)
                {
                    r.Errors.Add($"#{n.Id} 계열 한정인데 계열이 없다 — 아무 스킬에도 안 걸린다");
                }
                if (n.Trait == EPartTrait.None
                    && n.Type != EConstellationNodeType.Core
                    && n.Type != EConstellationNodeType.Skill
                    && n.Type != EConstellationNodeType.Gate)
                {
                    r.Warnings.Add($"#{n.Id} 특성이 없다 — 사도 아무 일도 안 일어난다");
                }
                if (n.Neighbors.Count == 0)
                {
                    r.Errors.Add($"#{n.Id}({n.DisplayName}) 에 이어진 선이 없다 — 영원히 못 산다");
                }
            }

            if (core == 0)
            {
                r.Errors.Add("코어가 없다 — 모든 경로의 출발점이 없으면 아무것도 살 수 없다");
            }
            else if (core > 1)
            {
                r.Errors.Add($"코어가 {core}개다 — 시작점은 하나여야 한다");
            }
        }

        private static void CheckEdges(ConstellationGraph graph, Result r)
        {
            HashSet<long> seen = new HashSet<long>();
            for (int i = 0; i < graph.EdgeCount; i++)
            {
                int a = graph.EdgeA[i];
                int b = graph.EdgeB[i];

                if (a == b)
                {
                    r.Errors.Add($"#{a} 이 자기 자신에 이어져 있다");
                    continue;
                }
                if (graph.Get(a) == null || graph.Get(b) == null)
                {
                    r.Errors.Add($"간선 {a}-{b} 의 한쪽 노드가 없다");
                    continue;
                }

                int lo = a < b ? a : b;
                int hi = a < b ? b : a;
                long key = ((long)lo << 32) | (uint)hi;
                if (seen.Add(key) == false)
                {
                    r.Errors.Add($"간선 {lo}-{hi} 이 두 번 있다 — 방향 없는 쌍이라 한 번이면 된다");
                }
            }
        }

        private static void CheckReach(ConstellationGraph graph, Result r)
        {
            int coreId = 0;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i].Type == EConstellationNodeType.Core)
                {
                    coreId = graph.Nodes[i].Id;
                    break;
                }
            }
            if (coreId == 0)
            {
                return;
            }

            HashSet<int> seen = new HashSet<int> { coreId };
            List<int> queue = new List<int> { coreId };
            for (int head = 0; head < queue.Count; head++)
            {
                ConstellationNode n = graph.Get(queue[head]);
                if (n == null)
                {
                    continue;
                }
                for (int i = 0; i < n.Neighbors.Count; i++)
                {
                    if (seen.Add(n.Neighbors[i]))
                    {
                        queue.Add(n.Neighbors[i]);
                    }
                }
            }

            if (seen.Count == graph.Nodes.Count)
            {
                return;
            }

            int missing = graph.Nodes.Count - seen.Count;
            r.Errors.Add($"코어에서 못 닿는 노드 {missing}개 — 갈래가 끊겼다");
            int shown = 0;
            for (int i = 0; i < graph.Nodes.Count && shown < 5; i++)
            {
                if (seen.Contains(graph.Nodes[i].Id) == false)
                {
                    r.Errors.Add($"    끊긴 자리 #{graph.Nodes[i].Id}({graph.Nodes[i].DisplayName})");
                    shown++;
                }
            }
        }

        private static void CheckOverlap(ConstellationGraph graph, Result r)
        {
            float sqr = OverlapDistance * OverlapDistance;
            int count = 0;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                for (int k = i + 1; k < graph.Nodes.Count; k++)
                {
                    ConstellationNode a = graph.Nodes[i];
                    ConstellationNode b = graph.Nodes[k];
                    float dx = a.X - b.X;
                    float dy = a.Y - b.Y;
                    if (dx * dx + dy * dy >= sqr)
                    {
                        continue;
                    }
                    count++;
                    if (count <= 5)
                    {
                        r.Warnings.Add($"#{a.Id}({a.Type}) 과 #{b.Id}({b.Type}) 이 {Mathf.Sqrt(dx * dx + dy * dy):0.0} 만큼 붙어 있다");
                    }
                }
            }
            if (count > 5)
            {
                r.Warnings.Add($"    … 겹친 쌍 {count}개");
            }
        }

        public static long TotalCost(ConstellationGraph graph)
        {
            long total = 0;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                ConstellationNode n = graph.Nodes[i];
                for (int lv = 0; lv < n.MaxLevel; lv++)
                {
                    if (n.Type == EConstellationNodeType.Level)
                    {
                        float raw = n.Cost * Mathf.Pow(CommonConfig.Constellation.LevelCostGrowth, lv);
                        total += Mathf.Max(1, Mathf.FloorToInt(raw + 0.5f));
                    }
                    else
                    {
                        total += n.Cost;
                    }
                }
            }
            return total;
        }
    }
}
