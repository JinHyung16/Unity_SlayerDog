using System.Collections.Generic;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using UnityEngine;

namespace Jinhyeong_Constellation
{

    public static class ConstellationGenerator
    {
        private class Cluster
        {
            public int Ring;
            public float Angle;
            public float X;
            public float Y;
            public int CenterId;
            public int PortInId;
            public int PortOutId;
            public int PortVMaxId;
            public int PortVMinId;
        }

        private static int _nextId;
        private static int _levelPlaced;
        private static MulberryRandom _rng;

        public static void Build(ConstellationGraph graph)
        {
            if (graph == null)
            {
                return;
            }
            graph.Clear();

            _nextId = 1;
            _levelPlaced = 0;
            _rng = new MulberryRandom(CommonConfig.Constellation.Seed);

            ConstellationNode core = NewNode(EConstellationNodeType.Core, ESkillFamily.None, 0f, 0f, 0);
            core.Cost = 0;
            core.DisplayName = "개";
            core.Description = "여기서 시작한다";
            graph.Add(core);

            IReadOnlyList<ConstellationRegionData> regions = ConstellationRegionRegistry.All;
            for (int r = 0; r < regions.Count; r++)
            {
                BuildRegion(graph, regions[r], core.Id);
            }

            Flatten(graph);
        }

        private static void Flatten(ConstellationGraph graph)
        {
            float k = CommonConfig.Constellation.AspectY;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                graph.Nodes[i].Y *= k;
            }
        }

        private static void BuildRegion(ConstellationGraph graph, ConstellationRegionData region, int coreId)
        {
            List<Cluster> clusters = PlaceClusters(region);
            List<PartData> skills = FamilySkills(region.Family);

            for (int i = 0; i < clusters.Count; i++)
            {
                PartData skill = i < skills.Count ? skills[i] : null;
                BuildCluster(graph, region, clusters[i], i, skill);
            }

            BuildRoads(graph, region, clusters, coreId);
            BuildKeystone(graph, region, clusters);
        }

        private static List<Cluster> PlaceClusters(ConstellationRegionData region)
        {
            float[] radius = CommonConfig.Constellation.RingRadius;
            int ringCount = radius.Length;
            int n = Mathf.Max(1, region.ClusterCount);

            List<Cluster> list = new List<Cluster>(n);
            int[] perRing = new int[ringCount];
            for (int i = 0; i < n; i++)
            {
                int ring = Mathf.Clamp(i * ringCount / n, 0, ringCount - 1);
                Cluster c = new Cluster();
                c.Ring = ring;
                list.Add(c);
                perRing[ring]++;
            }

            int[] seen = new int[ringCount];
            for (int i = 0; i < list.Count; i++)
            {
                Cluster c = list[i];
                int count = perRing[c.Ring];
                int idx = seen[c.Ring]++;

                float width = region.Span * (0.60f + 0.09f * c.Ring);
                float f = count == 1 ? 0.5f : idx / (float)(count - 1);
                float deg = region.CenterAngle + (f - 0.5f) * width;
                float rad = deg * Mathf.Deg2Rad;

                c.Angle = rad;
                c.X = Mathf.Cos(rad) * radius[c.Ring];
                c.Y = Mathf.Sin(rad) * radius[c.Ring];
            }
            return list;
        }

        private static void BuildCluster(ConstellationGraph graph, ConstellationRegionData region,
                                         Cluster cluster, int clusterIndex, PartData skill)
        {
            int leaf = cluster.Ring < 2
                ? CommonConfig.Constellation.InnerLeafCount
                : CommonConfig.Constellation.OuterLeafCount;

            float cx = cluster.X;
            float cy = cluster.Y;
            Nudge(graph, ref cx, ref cy, Mathf.Cos(cluster.Angle), Mathf.Sin(cluster.Angle), 1f);
            cluster.X = cx;
            cluster.Y = cy;

            ConstellationNode center = skill != null
                ? NewSkillNode(region, cluster, skill)
                : NewNotableNode(region, cluster);
            graph.Add(center);
            cluster.CenterId = center.Id;

            int pattern = cluster.Ring == 0 ? 0 : (clusterIndex + cluster.Ring * 2) % 5;

            float orb = CommonConfig.Constellation.OrbitRadius;
            List<int> leafIds = new List<int>(leaf);
            List<Vector2> local = new List<Vector2>(leaf);

            for (int i = 0; i < leaf; i++)
            {
                Vector2 uv = PatternPoint(pattern, i, leaf, orb);
                local.Add(uv);

                float cu = Mathf.Cos(cluster.Angle);
                float su = Mathf.Sin(cluster.Angle);
                float wx = cluster.X + cu * uv.x - su * uv.y;
                float wy = cluster.Y + su * uv.x + cu * uv.y;

                ConstellationNode near = FindNear(graph, wx, wy);
                if (near != null)
                {
                    leafIds.Add(near.Id);
                    continue;
                }

                ConstellationNode leafNode = NewLeafNode(region, i, wx, wy, cluster.Ring);
                graph.Add(leafNode);
                leafIds.Add(leafNode.Id);
            }

            LinkPattern(graph, pattern, center.Id, leafIds);
            ResolvePorts(cluster, local, leafIds);
        }

        private static Vector2 PatternPoint(int pattern, int i, int count, float orb)
        {
            switch (pattern)
            {
                case 0:
                {
                    float a = Mathf.PI * 2f * i / count;
                    return new Vector2(Mathf.Cos(a) * orb, Mathf.Sin(a) * orb);
                }

                case 1:
                {
                    if (i < 4)
                    {
                        float a = Mathf.PI * 0.5f * i;
                        return new Vector2(Mathf.Cos(a) * orb, Mathf.Sin(a) * orb);
                    }
                    return new Vector2(orb * (1.75f + 0.6f * (i - 4)), 0f);
                }

                case 2:
                {
                    int step = i / 2;
                    float side = (i % 2 == 0) ? -1f : 1f;
                    return new Vector2(orb * (0.70f + 0.62f * step), side * orb * 0.60f);
                }

                case 3:
                {
                    float t = count == 1 ? 0.5f : i / (float)(count - 1);
                    float a = -1.5f + t * 3.0f;
                    return new Vector2(Mathf.Cos(a) * orb, Mathf.Sin(a) * orb);
                }

                default:
                {
                    if (i < 4)
                    {
                        float a = Mathf.PI * 0.5f * i;
                        return new Vector2(Mathf.Cos(a) * orb, Mathf.Sin(a) * orb);
                    }
                    return new Vector2(orb * 1.85f, 0f);
                }
            }
        }

        private static void LinkPattern(ConstellationGraph graph, int pattern, int centerId, List<int> leafIds)
        {
            if (leafIds.Count == 0)
            {
                return;
            }

            switch (pattern)
            {
                case 0:
                    for (int i = 0; i < leafIds.Count; i++)
                    {
                        graph.Link(leafIds[i], leafIds[(i + 1) % leafIds.Count]);
                    }
                    graph.Link(centerId, leafIds[0]);
                    break;

                case 2:
                    for (int i = 0; i < leafIds.Count; i++)
                    {
                        int prev = i - 2;
                        graph.Link(prev >= 0 ? leafIds[prev] : centerId, leafIds[i]);
                    }
                    break;

                case 3:
                    for (int i = 1; i < leafIds.Count; i++)
                    {
                        graph.Link(leafIds[i - 1], leafIds[i]);
                    }
                    graph.Link(centerId, leafIds[leafIds.Count / 2]);
                    break;

                default:
                    for (int i = 0; i < leafIds.Count; i++)
                    {
                        graph.Link(centerId, leafIds[i]);
                    }
                    break;
            }
        }

        private static void ResolvePorts(Cluster cluster, List<Vector2> local, List<int> leafIds)
        {
            cluster.PortInId = cluster.CenterId;
            cluster.PortOutId = cluster.CenterId;
            cluster.PortVMaxId = cluster.CenterId;
            cluster.PortVMinId = cluster.CenterId;
            if (local.Count == 0)
            {
                return;
            }

            float minU = float.MaxValue;
            float maxU = float.MinValue;
            float maxV = float.MinValue;
            float minV = float.MaxValue;

            for (int i = 0; i < local.Count; i++)
            {
                Vector2 p = local[i];
                if (p.x < minU)
                {
                    minU = p.x;
                    cluster.PortInId = leafIds[i];
                }
                if (p.x > maxU)
                {
                    maxU = p.x;
                    cluster.PortOutId = leafIds[i];
                }
                if (p.y > maxV)
                {
                    maxV = p.y;
                    cluster.PortVMaxId = leafIds[i];
                }
                if (p.y < minV)
                {
                    minV = p.y;
                    cluster.PortVMinId = leafIds[i];
                }
            }
        }

        private static void BuildRoads(ConstellationGraph graph, ConstellationRegionData region,
                                       List<Cluster> clusters, int coreId)
        {
            bool gatePlaced = false;
            int outerRing = CommonConfig.Constellation.RingRadius.Length - 1;

            for (int i = 0; i < clusters.Count; i++)
            {
                Cluster c = clusters[i];
                if (c.Ring == 0)
                {
                    Road(graph, region, coreId, c.PortInId, c.Ring, false);
                    continue;
                }

                Cluster parent = NearestInner(clusters, c);
                if (parent == null)
                {
                    Road(graph, region, coreId, c.PortInId, c.Ring, false);
                    continue;
                }

                bool gate = gatePlaced == false && c.Ring >= outerRing;
                Road(graph, region, parent.PortOutId, c.PortInId, c.Ring, gate);
                if (gate)
                {
                    gatePlaced = true;
                }
            }

            for (int i = 0; i < clusters.Count; i++)
            {
                for (int k = i + 1; k < clusters.Count; k++)
                {
                    if (clusters[i].Ring != clusters[k].Ring)
                    {
                        continue;
                    }
                    if (_rng.Chance(CommonConfig.Constellation.NeighborLinkChance) == false)
                    {
                        continue;
                    }
                    Road(graph, region, clusters[i].PortVMaxId, clusters[k].PortVMinId, clusters[i].Ring, false);
                }
            }
        }

        private static Cluster NearestInner(List<Cluster> clusters, Cluster self)
        {
            Cluster best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < clusters.Count; i++)
            {
                Cluster c = clusters[i];
                if (c.Ring != self.Ring - 1)
                {
                    continue;
                }
                float dx = c.X - self.X;
                float dy = c.Y - self.Y;
                float d = dx * dx + dy * dy;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
            return best;
        }

        private static void Road(ConstellationGraph graph, ConstellationRegionData region,
                                 int fromId, int toId, int ring, bool gate)
        {
            ConstellationNode a = graph.Get(fromId);
            ConstellationNode b = graph.Get(toId);
            if (a == null || b == null)
            {
                return;
            }

            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            int n = Mathf.Max(0, Mathf.RoundToInt(dist / CommonConfig.Constellation.SegmentLength) - 1);

            int gateSlot = (n + 1) / 2;
            bool placedGate = false;

            int prev = fromId;
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)(n + 1);
                float x = a.X + dx * t;
                float y = a.Y + dy * t;

                bool isGate = gate && i == gateSlot;

                if (isGate == false)
                {
                    ConstellationNode near = FindNear(graph, x, y);
                    if (near != null)
                    {
                        graph.Link(prev, near.Id);
                        prev = near.Id;
                        continue;
                    }

                    ConstellationNode leaf = NewLeafNode(region, i, x, y, ring);
                    graph.Add(leaf);
                    graph.Link(prev, leaf.Id);
                    prev = leaf.Id;
                    continue;
                }

                Nudge(graph, ref x, ref y, dx, dy, dist);
                ConstellationNode gateNode = NewGateNode(region, ring, x, y);
                placedGate = true;
                graph.Add(gateNode);
                graph.Link(prev, gateNode.Id);
                prev = gateNode.Id;
            }

            if (gate && placedGate == false)
            {
                float gx = (a.X + b.X) * 0.5f;
                float gy = (a.Y + b.Y) * 0.5f;
                Nudge(graph, ref gx, ref gy, dx, dy, dist);

                ConstellationNode node = NewGateNode(region, ring, gx, gy);
                graph.Add(node);
                graph.Link(prev, node.Id);
                prev = node.Id;
            }

            graph.Link(prev, toId);
        }

        private static void BuildKeystone(ConstellationGraph graph, ConstellationRegionData region, List<Cluster> clusters)
        {
            Cluster outer = null;
            for (int i = 0; i < clusters.Count; i++)
            {
                if (outer == null || clusters[i].Ring > outer.Ring)
                {
                    outer = clusters[i];
                }
            }
            if (outer == null)
            {
                return;
            }

            ConstellationNode port = graph.Get(outer.PortOutId);
            if (port == null)
            {
                return;
            }

            float cu = Mathf.Cos(outer.Angle);
            float su = Mathf.Sin(outer.Angle);
            float step = CommonConfig.Constellation.SpurStep;

            int prev = outer.PortOutId;
            for (int i = 1; i <= 2; i++)
            {
                float x = port.X + cu * step * i;
                float y = port.Y + su * step * i;

                ConstellationNode near = FindNear(graph, x, y);
                if (near != null)
                {
                    graph.Link(prev, near.Id);
                    prev = near.Id;
                    continue;
                }

                ConstellationNode way = NewLeafNode(region, i, x, y, outer.Ring);
                graph.Add(way);
                graph.Link(prev, way.Id);
                prev = way.Id;
            }

            float kx = port.X + cu * step * 3f;
            float ky = port.Y + su * step * 3f;
            Nudge(graph, ref kx, ref ky, cu, su, 1f);

            ConstellationNode keystone = NewNode(EConstellationNodeType.Keystone, region.Family, kx, ky, outer.Ring);
            keystone.Cost = CostOf(CommonConfig.Constellation.CostKeystone, outer.Ring);
            keystone.Trait = region.KeystoneTrait;
            keystone.Value = region.KeystoneValue;
            keystone.FamilyScoped = true;
            keystone.DisplayName = region.KeystoneName;
            keystone.Description = region.KeystoneDesc;
            graph.Add(keystone);
            graph.Link(prev, keystone.Id);
        }

        private static ConstellationNode NewNode(EConstellationNodeType type, ESkillFamily family,
                                                 float x, float y, int ring)
        {
            ConstellationNode n = new ConstellationNode();
            n.Id = _nextId++;
            n.Type = type;
            n.Family = family;
            n.X = x;
            n.Y = y;
            n.Ring = ring;
            n.MaxLevel = 1;
            return n;
        }

        private static ConstellationNode NewLeafNode(ConstellationRegionData region, int index, float x, float y, int ring)
        {
            bool useA = _rng.Next() < 0.5f;
            EPartTrait trait = useA ? region.TraitA : region.TraitB;
            float baseValue = useA ? region.ValueA : region.ValueB;
            float value = baseValue * (1f + CommonConfig.Constellation.ValueRingGain * ring);

            bool level = index == 2
                         && _levelPlaced < CommonConfig.Constellation.LevelNodeMax
                         && _rng.Chance(CommonConfig.Constellation.LevelChance);

            ConstellationNode n = NewNode(level ? EConstellationNodeType.Level : EConstellationNodeType.Small,
                region.Family, x, y, ring);
            n.Trait = trait;
            n.Value = Round3(level ? value * 1.2f : value);
            n.FamilyScoped = false;

            if (level)
            {
                _levelPlaced++;
                n.MaxLevel = CommonConfig.Constellation.MaxNodeLevel;
                n.Cost = CostOf(CommonConfig.Constellation.CostLevel, ring);
            }
            else
            {
                n.Cost = CostOf(CommonConfig.Constellation.CostSmall, ring);
            }

            n.DisplayName = TraitText.Short(trait, n.Value);
            n.Description = TraitText.Long(trait, n.Value, false, region.DisplayName);
            return n;
        }

        private static ConstellationNode NewNotableNode(ConstellationRegionData region, Cluster cluster)
        {
            float value = Round3(region.ValueA * 4f * (1f + CommonConfig.Constellation.ValueRingGain * cluster.Ring));
            ConstellationNode n = NewNode(EConstellationNodeType.Notable, region.Family, cluster.X, cluster.Y, cluster.Ring);
            n.Cost = CostOf(CommonConfig.Constellation.CostNotable, cluster.Ring);
            n.Trait = region.TraitA;
            n.Value = value;
            n.FamilyScoped = true;
            n.DisplayName = $"{region.DisplayName} {TraitText.Short(region.TraitA, value)}";
            n.Description = TraitText.Long(region.TraitA, value, true, region.DisplayName);
            return n;
        }

        private static ConstellationNode NewSkillNode(ConstellationRegionData region, Cluster cluster, PartData skill)
        {
            ConstellationNode n = NewNode(EConstellationNodeType.Skill, region.Family, cluster.X, cluster.Y, cluster.Ring);
            n.Cost = CostOf(CommonConfig.Constellation.CostSkill, cluster.Ring);
            n.SkillId = skill.SkillId;
            n.DisplayName = skill.DisplayName;
            n.Description = "해금 — 필드에서 이 파츠가 나오기 시작한다";
            return n;
        }

        private static ConstellationNode NewGateNode(ConstellationRegionData region, int ring, float x, float y)
        {
            int need = CommonConfig.Constellation.GateNeedClear;
            ConstellationNode n = NewNode(EConstellationNodeType.Gate, region.Family, x, y, ring);
            n.Cost = CostOf(CommonConfig.Constellation.CostGate, ring);
            n.NeedClear = need;
            n.DisplayName = "관문";
            n.Description = $"클리어 {need}회 이상이어야 지나간다";
            return n;
        }

        public static int CostOf(int baseCost, int ring)
        {
            float raw = baseCost * Mathf.Pow(CommonConfig.Constellation.CostGrowth, ring);
            return Mathf.Max(1, Mathf.FloorToInt(raw + 0.5f));
        }

        private static void Nudge(ConstellationGraph graph, ref float x, ref float y,
                                  float dx, float dy, float dist)
        {
            if (FindNear(graph, x, y) == null)
            {
                return;
            }

            float offset = CommonConfig.Constellation.MergeDistance * 1.4f;

            float px;
            float py;
            if (dist > 0.001f)
            {
                px = -dy / dist * offset;
                py = dx / dist * offset;
            }
            else
            {
                px = offset;
                py = 0f;
            }

            if (FindNear(graph, x + px, y + py) == null)
            {
                x += px;
                y += py;
                return;
            }
            if (FindNear(graph, x - px, y - py) == null)
            {
                x -= px;
                y -= py;
            }
        }

        private static ConstellationNode FindNear(ConstellationGraph graph, float x, float y)
        {
            float sqr = CommonConfig.Constellation.MergeDistance * CommonConfig.Constellation.MergeDistance;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                ConstellationNode n = graph.Nodes[i];
                float dx = n.X - x;
                float dy = n.Y - y;
                if (dx * dx + dy * dy < sqr)
                {
                    return n;
                }
            }
            return null;
        }

        private static float Round3(float v)
        {
            return Mathf.Round(v * 1000f) / 1000f;
        }

        private static List<PartData> FamilySkills(ESkillFamily family)
        {
            List<PartData> list = new List<PartData>(8);
            IReadOnlyList<PartData> all = PartRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Family == family)
                {
                    list.Add(all[i]);
                }
            }
            list.Sort(CompareSkillId);
            return list;
        }

        private static int CompareSkillId(PartData a, PartData b)
        {
            return a.SkillId.CompareTo(b.SkillId);
        }
    }
}
