using System;
using System.Collections.Generic;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using UnityEngine;

namespace Jinhyeong_Constellation
{

    public static class ConstellationManager
    {
        private static readonly ConstellationGraph _graph = new ConstellationGraph();
        private static readonly Dictionary<int, int> _owned = new Dictionary<int, int>(64);

        private static readonly Dictionary<EPartTrait, float> _global = new Dictionary<EPartTrait, float>(12);
        private static readonly Dictionary<long, float> _family = new Dictionary<long, float>(24);

        private static bool _built;
        private static bool _hooked;

        public static event Action OnChanged;

        public static ConstellationGraph Graph
        {
            get
            {
                Ensure();
                return _graph;
            }
        }

        public static int OwnedCount
        {
            get
            {
                Ensure();
                return _owned.Count - 1;
            }
        }

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

            if (ConstellationGraphIO.TryLoadBaked(_graph) == false)
            {
                ConstellationGenerator.Build(_graph);
            }

            LoadOwned();
            Recalculate();
        }

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

        public static int LevelOf(int nodeId)
        {
            Ensure();
            return _owned.TryGetValue(nodeId, out int lv) ? lv : 0;
        }

        public static bool IsOwned(int nodeId)
        {
            return LevelOf(nodeId) > 0;
        }

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

        public static bool IsGateOpen(ConstellationNode node)
        {
            if (node == null || node.Type != EConstellationNodeType.Gate)
            {
                return true;
            }
            return SaveData.ClearCount >= node.NeedClear;
        }

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

        public static float Global(EPartTrait trait)
        {
            Ensure();
            return _global.TryGetValue(trait, out float v) ? v : 0f;
        }

        public static float Family(ESkillFamily family, EPartTrait trait)
        {
            Ensure();
            return _family.TryGetValue(FamilyKey(family, trait), out float v) ? v : 0f;
        }

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
