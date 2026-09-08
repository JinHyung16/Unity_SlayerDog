using System;
using System.Collections.Generic;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using UnityEngine;

namespace Jinhyeong_Constellation
{

    /// <summary>
    /// 성좌의 «상태» — 무엇을 샀고, 무엇을 살 수 있고, 그래서 무슨 효과가 붙는가.
    ///
    /// 그래프(모양)와 소유(진행)를 나눠 둔다. 그래프는 시드에서 매번 똑같이 나오므로 저장하지 않고,
    /// 저장되는 것은 노드 id 와 레벨뿐이다. 저장 크기가 노드 수에 비례하지 않는다.
    ///
    /// 효과는 **누적하지 않고 매번 0에서 다시 합산한다**. 누적하면 초기화·환불·마이그레이션마다
    /// 「지금 값이 맞나」를 검증할 방법이 없어진다. 노드 200개를 다시 도는 비용은 로비에서 한 번이다.
    /// </summary>
    public static class ConstellationManager
    {
        private static readonly ConstellationGraph _graph = new ConstellationGraph();
        private static readonly Dictionary<int, int> _owned = new Dictionary<int, int>(64);

        private static readonly Dictionary<EPartTrait, float> _global = new Dictionary<EPartTrait, float>(12);
        private static readonly Dictionary<long, float> _family = new Dictionary<long, float>(24);

        private static bool _built;
        private static bool _hooked;

        /// <summary>산 노드가 바뀌었다. 화면이 다시 그리는 신호.</summary>
        public static event Action OnChanged;

        public static ConstellationGraph Graph
        {
            get
            {
                Ensure();
                return _graph;
            }
        }

        /// <summary>산 노드 수(코어 제외). 실측 리포트가 이 값을 찍는다.</summary>
        public static int OwnedCount
        {
            get
            {
                Ensure();
                return _owned.Count - 1;
            }
        }

        /// <summary>
        /// 그래프를 세우고 저장을 얹는다. 여러 곳에서 불려도 한 번만 돈다 —
        /// 로비도, 런 중의 파츠 계산도 같은 문을 쓴다.
        /// </summary>
        public static void Ensure()
        {
            if (_built)
            {
                return;
            }
            _built = true;

            if (_hooked == false)
            {
                _hooked = true;
                SaveData.StarWiped += HandleWiped;
            }

            if (ConstellationRegionRegistry.Count == 0)
            {
                ConstellationRegionRegistry.Load();
            }
            if (PartRegistry.Count == 0)
            {
                PartRegistry.Load();
            }

            // 손으로 만든 성좌가 있으면 그쪽이 이긴다. 없으면 시드에서 초안을 만든다 —
            // 도구를 안 쓰는 동안에도 게임은 돌아야 하고, 도구는 그 초안 위에서 시작한다.
            if (ConstellationGraphIO.TryLoadBaked(_graph) == false)
            {
                ConstellationGenerator.Build(_graph);
            }

            LoadOwned();
            Recalculate();
        }

        /// <summary>초기화가 돌면 캐시를 버린다. 다음에 누가 물어볼 때 저장에서 새로 읽는다.</summary>
        private static void HandleWiped()
        {
            _built = false;
            _owned.Clear();
            _global.Clear();
            _family.Clear();
            if (OnChanged != null)
            {
                OnChanged.Invoke();
            }
        }

        private static void LoadOwned()
        {
            _owned.Clear();

            // 코어는 값 없이 처음부터 갖고 있다. 모든 경로의 출발점이 없으면 아무것도 살 수 없다.
            for (int i = 0; i < _graph.Nodes.Count; i++)
            {
                if (_graph.Nodes[i].Type == EConstellationNodeType.Core)
                {
                    _owned[_graph.Nodes[i].Id] = 1;
                }
            }

            IReadOnlyList<int> ids = SaveData.StarNodes;
            IReadOnlyList<int> levels = SaveData.StarLevels;
            for (int i = 0; i < ids.Count; i++)
            {
                ConstellationNode node = _graph.Get(ids[i]);
                if (node == null)
                {
                    // 상수를 바꿔 노드가 밀렸다. 조용히 버린다 — 엉뚱한 자리를 켜 두는 것보다 낫다.
                    continue;
                }
                int lv = i < levels.Count ? levels[i] : 1;
                _owned[ids[i]] = Mathf.Clamp(lv, 1, node.MaxLevel);
            }
        }

        private static void SaveOwned()
        {
            List<int> ids = new List<int>(_owned.Count);
            List<int> levels = new List<int>(_owned.Count);
            foreach (KeyValuePair<int, int> kv in _owned)
            {
                ConstellationNode node = _graph.Get(kv.Key);
                if (node == null || node.Type == EConstellationNodeType.Core)
                {
                    continue;
                }
                ids.Add(kv.Key);
                levels.Add(kv.Value);
            }
            SaveData.SetStarNodes(ids, levels);
        }

        // ───────────────────────── 질의 ─────────────────────────

        public static int LevelOf(int nodeId)
        {
            Ensure();
            return _owned.TryGetValue(nodeId, out int lv) ? lv : 0;
        }

        public static bool IsOwned(int nodeId)
        {
            return LevelOf(nodeId) > 0;
        }

        /// <summary>다음 한 단계의 값. 레벨 노드는 올릴수록 비싸진다.</summary>
        public static int NextCost(ConstellationNode node)
        {
            if (node == null)
            {
                return 0;
            }
            int lv = LevelOf(node.Id);
            if (node.Type != EConstellationNodeType.Level)
            {
                return node.Cost;
            }
            float raw = node.Cost * Mathf.Pow(CommonConfig.Constellation.LevelCostGrowth, lv);
            return Mathf.Max(1, Mathf.FloorToInt(raw + 0.5f));
        }

        /// <summary>관문이 열렸나. 관문이 아니면 항상 열려 있다.</summary>
        public static bool IsGateOpen(ConstellationNode node)
        {
            if (node == null || node.Type != EConstellationNodeType.Gate)
            {
                return true;
            }
            return SaveData.ClearCount >= node.NeedClear;
        }

        /// <summary>
        /// 이 노드를 «한 단계» 올릴 수 있나. 값은 보지 않는다 — 돈이 모자란 것과
        /// 애초에 못 사는 것은 화면에서 다르게 보여야 한다.
        /// </summary>
        public static bool CanReach(ConstellationNode node)
        {
            Ensure();
            if (node == null || node.Type == EConstellationNodeType.Core)
            {
                return false;
            }
            if (LevelOf(node.Id) >= node.MaxLevel)
            {
                return false;
            }
            if (IsGateOpen(node) == false)
            {
                return false;
            }
            if (LevelOf(node.Id) > 0)
            {
                // 이미 산 노드를 한 단계 더 올리는 것은 인접을 다시 따질 필요가 없다.
                return true;
            }
            for (int i = 0; i < node.Neighbors.Count; i++)
            {
                if (IsOwned(node.Neighbors[i]))
                {
                    return true;
                }
            }
            return false;
        }

        // ───────────────────────── 경로 ─────────────────────────

        /// <summary>
        /// 보유 노드 «전체»를 출발점으로 하는 너비 우선 탐색. 가중치가 없으니 간선 수 최소 경로다 —
        /// 골드 최소 경로가 아니라는 점을 알고 쓴다. 성좌에서 두 경로의 값 차이는 크지 않고,
        /// 「가장 가까운 길」이 사람이 기대하는 답이다.
        ///
        /// 닫힌 관문은 목적지 자신이 아닌 한 «펼치지 않는다» — 관문이 실제로 길을 막는 자리가 여기다.
        /// </summary>
        public static bool PathTo(int targetId, List<int> outPath)
        {
            Ensure();
            if (outPath == null)
            {
                return false;
            }
            outPath.Clear();

            ConstellationNode target = _graph.Get(targetId);
            if (target == null)
            {
                return false;
            }
            if (IsOwned(targetId) && LevelOf(targetId) < target.MaxLevel)
            {
                // 이미 가진 레벨 노드를 한 단계 더 — 경로가 필요 없다.
                outPath.Add(targetId);
                return true;
            }
            if (IsOwned(targetId))
            {
                return false;
            }

            Dictionary<int, int> from = new Dictionary<int, int>(64);
            List<int> queue = new List<int>(64);
            HashSet<int> seen = new HashSet<int>();

            foreach (KeyValuePair<int, int> kv in _owned)
            {
                queue.Add(kv.Key);
                seen.Add(kv.Key);
            }

            bool found = false;
            for (int head = 0; head < queue.Count; head++)
            {
                int id = queue[head];
                if (id == targetId)
                {
                    found = true;
                    break;
                }

                ConstellationNode node = _graph.Get(id);
                if (node == null)
                {
                    continue;
                }
                for (int i = 0; i < node.Neighbors.Count; i++)
                {
                    int next = node.Neighbors[i];
                    if (seen.Add(next) == false)
                    {
                        continue;
                    }

                    ConstellationNode nextNode = _graph.Get(next);
                    if (nextNode != null && next != targetId && IsGateOpen(nextNode) == false)
                    {
                        continue;
                    }
                    from[next] = id;
                    queue.Add(next);
                }
            }

            if (found == false && seen.Contains(targetId) == false)
            {
                return false;
            }

            // 목표에서 거슬러 올라가되, 이미 가진 노드에 닿으면 멈춘다.
            int cursor = targetId;
            while (IsOwned(cursor) == false)
            {
                outPath.Add(cursor);
                if (from.TryGetValue(cursor, out int prev) == false)
                {
                    outPath.Clear();
                    return false;
                }
                cursor = prev;
            }
            outPath.Reverse();
            return outPath.Count > 0;
        }

        /// <summary>경로 전체를 사는 데 드는 값.</summary>
        public static int PathCost(List<int> path)
        {
            Ensure();
            int sum = 0;
            for (int i = 0; i < path.Count; i++)
            {
                sum += NextCost(_graph.Get(path[i]));
            }
            return sum;
        }

        // ───────────────────────── 구매 ─────────────────────────

        /// <summary>
        /// 목표까지의 경로를 «한 번에» 산다. 한 칸씩만 살 수 있게 하면 잔가지 열 칸을
        /// 열 번 눌러야 하고, 그건 선택이 아니라 노동이다.
        ///
        /// 값이 모자라면 아무것도 사지 않는다 — 절반만 사고 멈추면 원하지 않은 자리에 돈이 묶인다.
        /// </summary>
        public static bool BuyPath(int targetId)
        {
            Ensure();

            List<int> path = new List<int>(16);
            if (PathTo(targetId, path) == false)
            {
                return false;
            }

            int cost = PathCost(path);
            if (SaveData.SuperDna < cost)
            {
                return false;
            }
            if (SaveData.SpendDna(cost) == false)
            {
                return false;
            }

            for (int i = 0; i < path.Count; i++)
            {
                ConstellationNode node = _graph.Get(path[i]);
                if (node == null)
                {
                    continue;
                }
                _owned.TryGetValue(node.Id, out int lv);
                _owned[node.Id] = Mathf.Min(node.MaxLevel, lv + 1);

                // 스킬 노드는 값을 여기서 이미 치렀다. 해금 쪽에서 또 받아 가면 두 번 낸다.
                if (node.Type == EConstellationNodeType.Skill && node.SkillId != 0)
                {
                    SaveData.GrantSkill(node.SkillId);
                }
            }

            SaveOwned();
            Recalculate();
            if (OnChanged != null)
            {
                OnChanged.Invoke();
            }
            return true;
        }

        // ───────────────────────── 효과 ─────────────────────────

        /// <summary>보유한 노드를 0에서 다시 합산한다. 여기가 캐시 무효화의 «유일한» 지점이다.</summary>
        private static void Recalculate()
        {
            _global.Clear();
            _family.Clear();

            foreach (KeyValuePair<int, int> kv in _owned)
            {
                ConstellationNode node = _graph.Get(kv.Key);
                if (node == null || node.Trait == EPartTrait.None)
                {
                    continue;
                }
                float amount = node.Value * kv.Value;
                if (node.FamilyScoped && node.Family != ESkillFamily.None)
                {
                    long key = FamilyKey(node.Family, node.Trait);
                    _family.TryGetValue(key, out float cur);
                    _family[key] = cur + amount;
                }
                else
                {
                    _global.TryGetValue(node.Trait, out float cur);
                    _global[node.Trait] = cur + amount;
                }
            }
        }

        private static long FamilyKey(ESkillFamily family, EPartTrait trait)
        {
            return ((long)family << 32) | (uint)trait;
        }

        /// <summary>모든 스킬에 걸리는 몫.</summary>
        public static float Global(EPartTrait trait)
        {
            Ensure();
            return _global.TryGetValue(trait, out float v) ? v : 0f;
        }

        /// <summary>그 계열 스킬에만 걸리는 몫. 전역분은 포함하지 않는다 — 부르는 쪽이 둘을 더한다.</summary>
        public static float Family(ESkillFamily family, EPartTrait trait)
        {
            Ensure();
            return _family.TryGetValue(FamilyKey(family, trait), out float v) ? v : 0f;
        }

        /// <summary>몸에 걸리는 특성(체력·이동속도·처치회복)은 계열을 안 탄다 — 스킬이 아니라 개에게 붙는다.</summary>
        public static float Body(EPartTrait trait)
        {
            Ensure();
            float sum = Global(trait);
            foreach (KeyValuePair<long, float> kv in _family)
            {
                if ((EPartTrait)(uint)(kv.Key & 0xFFFFFFFF) == trait)
                {
                    sum += kv.Value;
                }
            }
            return sum;
        }
    }
}
