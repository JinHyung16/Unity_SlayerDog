using System.Collections.Generic;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using UnityEngine;

namespace Jinhyeong_Constellation
{

    /// <summary>
    /// 계열 표 + 상수 + 시드 하나로 성좌를 통째로 만든다.
    ///
    /// 「좌표를 손으로 찍는 트리」와 「절차 생성 성좌」의 갈림길에서 후자를 골랐다.
    /// 손으로 찍으면 노드 200개의 좌표가 데이터가 되고, 「갈래를 조금 벌려 보자」가
    /// 200줄 수정이 된다. 여기서는 상수 한 줄이다.
    ///
    /// 대가는 **결정성에 대한 의존**이다. 시드·순서·난수 소비 지점이 바뀌면 모양이 통째로 바뀌고,
    /// 세이브는 노드 id 로 걸려 있어 산 노드가 옮겨간다. 그래서 난수는 아래 세 곳에서만 쓴다 —
    ///   ① 잔가지 특성 A/B 선택  ② 레벨 노드 승격  ③ 같은 링 이웃 연결
    /// 이 순서를 바꾸지 않는다.
    /// </summary>
    public static class ConstellationGenerator
    {
        /// <summary>한 성단이 만들어 내는 것들 — 길을 깔 때 접속구로 쓴다.</summary>
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

        /// <summary>
        /// 다 만든 «뒤에» 세로만 누른다.
        ///
        /// 만드는 도중에 누르지 않는 이유는, 성단 회전·길 쪼개기·키스톤 가지가 전부
        /// «원형 좌표»를 전제로 각도와 거리를 재기 때문이다. 중간에 누르면 그 계산들이
        /// 두 번 눌린 값을 다시 눌러 모양이 무너진다. 눌린 좌표는 그리기에만 쓴다.
        /// </summary>
        private static void Flatten(ConstellationGraph graph)
        {
            float k = CommonConfig.Constellation.AspectY;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                graph.Nodes[i].Y *= k;
            }
        }

        // ───────────────────────── 갈래 ─────────────────────────

        private static void BuildRegion(ConstellationGraph graph, ConstellationRegionData region, int coreId)
        {
            List<Cluster> clusters = PlaceClusters(region);
            List<PartData> skills = FamilySkills(region.Family);

            for (int i = 0; i < clusters.Count; i++)
            {
                // 성단의 중심은 그 계열 스킬을 하나씩 문다. 스킬보다 성단이 많으면 남는 자리는 계열 보너스가 된다.
                PartData skill = i < skills.Count ? skills[i] : null;
                BuildCluster(graph, region, clusters[i], i, skill);
            }

            BuildRoads(graph, region, clusters, coreId);
            BuildKeystone(graph, region, clusters);
        }

        /// <summary>
        /// 성단을 링에 배분하고 부채꼴 안에 균등하게 편다.
        ///
        /// [사고] 원본 공식은 링별 «비율»로 나눈 뒤 남는 개수를 고정 순서로 메웠다. 성단이 40개일 땐
        /// 잘 돌지만 우리처럼 5~6개면 내림 때문에 **중간 링이 통째로 비어** 갈래가 끊겨 보였다.
        /// 개수가 적을 땐 비율이 아니라 「고르게 훑기」가 맞다.
        /// </summary>
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

                // 바깥 링일수록 넓게 편다. 같은 폭이면 바깥이 성기게 보여 갈래가 가늘어진다.
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

        // ───────────────────────── 성단 ─────────────────────────

        private static void BuildCluster(ConstellationGraph graph, ConstellationRegionData region,
                                         Cluster cluster, int clusterIndex, PartData skill)
        {
            int leaf = cluster.Ring < 2
                ? CommonConfig.Constellation.InnerLeafCount
                : CommonConfig.Constellation.OuterLeafCount;

            // [사고] 성단 «중심»만 자리 검사를 안 해서, 옆 계열이 먼저 깔아 둔 잔가지 위에
            // 스킬 노드가 25유닛 거리로 앉았다. 중심은 스킬을 물고 있어 합칠 수 없으므로 비켜 세운다.
            // 잎·포트·길이 전부 중심을 기준으로 잡히므로 «중심을 먼저» 옮기고 나머지를 만든다.
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

            // 성단마다 모양을 돌린다. 전부 같은 모양이면 성좌가 아니라 격자로 보인다.
            int pattern = cluster.Ring == 0 ? 0 : (clusterIndex + cluster.Ring * 2) % 5;

            float orb = CommonConfig.Constellation.OrbitRadius;
            List<int> leafIds = new List<int>(leaf);
            List<Vector2> local = new List<Vector2>(leaf);

            for (int i = 0; i < leaf; i++)
            {
                Vector2 uv = PatternPoint(pattern, i, leaf, orb);
                local.Add(uv);

                // 로컬 → 월드. 성단의 «바깥 방향»으로 돌린다. 성좌가 방사형으로 뻗어 보이는 이유가 이것이다.
                float cu = Mathf.Cos(cluster.Angle);
                float su = Mathf.Sin(cluster.Angle);
                float wx = cluster.X + cu * uv.x - su * uv.y;
                float wy = cluster.Y + su * uv.x + cu * uv.y;

                // 이미 노드가 있는 자리면 새로 만들지 않고 그 노드를 쓴다 — 겹쳐 찍히면 무엇을 누르는지 알 수 없다.
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

        /// <summary>
        /// 성단 안의 로컬 좌표. 원본의 고정 좌표표 대신 «개수를 받는 식»으로 바꿨다 —
        /// 잔가지 수를 링마다 다르게 두려면 고정표는 매번 다시 적어야 한다.
        /// </summary>
        private static Vector2 PatternPoint(int pattern, int i, int count, float orb)
        {
            switch (pattern)
            {
                case 0:
                {
                    // 원환
                    float a = Mathf.PI * 2f * i / count;
                    return new Vector2(Mathf.Cos(a) * orb, Mathf.Sin(a) * orb);
                }

                case 1:
                {
                    // 마름모 + 바깥 가시
                    if (i < 4)
                    {
                        float a = Mathf.PI * 0.5f * i;
                        return new Vector2(Mathf.Cos(a) * orb, Mathf.Sin(a) * orb);
                    }
                    return new Vector2(orb * (1.75f + 0.6f * (i - 4)), 0f);
                }

                case 2:
                {
                    // 갈퀴 — 두 줄이 바깥으로 나란히
                    int step = i / 2;
                    float side = (i % 2 == 0) ? -1f : 1f;
                    return new Vector2(orb * (0.70f + 0.62f * step), side * orb * 0.60f);
                }

                case 3:
                {
                    // 호
                    float t = count == 1 ? 0.5f : i / (float)(count - 1);
                    float a = -1.5f + t * 3.0f;
                    return new Vector2(Mathf.Cos(a) * orb, Mathf.Sin(a) * orb);
                }

                default:
                {
                    // 십자
                    if (i < 4)
                    {
                        float a = Mathf.PI * 0.5f * i;
                        return new Vector2(Mathf.Cos(a) * orb, Mathf.Sin(a) * orb);
                    }
                    return new Vector2(orb * 1.85f, 0f);
                }
            }
        }

        /// <summary>패턴마다 결선이 다르다. 전부 「중심에서 방사」로 이으면 성단이 죄다 별표가 된다.</summary>
        private static void LinkPattern(ConstellationGraph graph, int pattern, int centerId, List<int> leafIds)
        {
            if (leafIds.Count == 0)
            {
                return;
            }

            switch (pattern)
            {
                case 0:
                    // 고리로 닫고 중심과는 한 곳만 — 순환이 생겨 「돌아가는 길」이 된다
                    for (int i = 0; i < leafIds.Count; i++)
                    {
                        graph.Link(leafIds[i], leafIds[(i + 1) % leafIds.Count]);
                    }
                    graph.Link(centerId, leafIds[0]);
                    break;

                case 2:
                    // 두 줄 사슬. 짝수 인덱스가 아래줄, 홀수가 윗줄
                    for (int i = 0; i < leafIds.Count; i++)
                    {
                        int prev = i - 2;
                        graph.Link(prev >= 0 ? leafIds[prev] : centerId, leafIds[i]);
                    }
                    break;

                case 3:
                    // 호 — 사슬로 잇고 가운데를 중심에 붙인다
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

        /// <summary>
        /// 접속구. 성단끼리는 아무 노드나 잇지 않는다 — 안쪽 끝과 바깥 끝을 정해 두면
        /// 길이 성단을 «관통»하지 않고 가장자리에서 갈아탄다.
        /// </summary>
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

        // ───────────────────────── 길 ─────────────────────────

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
                    // 안쪽 링에 성단이 없으면 코어로 직접 붙인다. 갈래가 끊긴 채 떠 있는 것보다 낫다.
                    Road(graph, region, coreId, c.PortInId, c.Ring, false);
                    continue;
                }

                // 관문은 갈래마다 하나. 가장 바깥으로 나가는 길목에 세워야 실제로 벽이 된다.
                bool gate = gatePlaced == false && c.Ring >= outerRing;
                Road(graph, region, parent.PortOutId, c.PortInId, c.Ring, gate);
                if (gate)
                {
                    gatePlaced = true;
                }
            }

            // 같은 링 이웃끼리. 이 확률 하나가 성좌에 «순환»을 만든다.
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

        /// <summary>
        /// 두 노드 사이를 <c>SegmentLength</c> 간격으로 쪼개 잔가지를 심는다.
        /// 길이가 짧으면 노드 없이 그냥 잇는다 — 노드가 겹쳐 찍히는 것보다 낫다.
        /// </summary>
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
                        // 길이 다른 길·성단과 만났다. 합치면 그 자리가 교차로가 되어 「돌아가는 길」이 하나 는다.
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

                // 관문은 «합칠 수 없다» — 다른 노드에 흡수되면 벽이 사라진다.
                // 자리가 찼으면 길에 수직으로 비켜 세운다.
                Nudge(graph, ref x, ref y, dx, dy, dist);
                ConstellationNode gateNode = NewGateNode(region, ring, x, y);
                placedGate = true;
                graph.Add(gateNode);
                graph.Link(prev, gateNode.Id);
                prev = gateNode.Id;
            }

            // 길이 너무 짧아 중간 노드가 하나도 안 생겼는데 관문을 세워야 한다면, 관문만 따로 끼운다.
            //
            // [사고] 이 가지가 없어서 관문이 계열마다 하나씩 서야 하는데 **통틀어 하나만** 섰다.
            // 성단 접속구 사이 거리가 링 간격보다 짧아 n = 0 이 되는 갈래가 대부분이었다.
            if (gate && placedGate == false)
            {
                // [사고] 그냥 중점에 놓았더니 이미 있던 잔가지와 «거리 0» 으로 겹쳤다.
                // 길이 짧아 중간 노드가 안 생기는 자리는, 애초에 양 끝이 가까운 자리다.
                // 자리가 찼으면 길에 수직으로 비켜 세운다 — 관문은 반드시 길 위에 서야 하므로 지우지는 않는다.
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

        // ───────────────────────── 키스톤 ─────────────────────────

        /// <summary>
        /// 갈래 끝. 가장 바깥 성단에서 바깥으로 두 마디 뻗고 그 끝에 계열을 규정하는 한 방을 둔다.
        /// 「여기까지 가면 이 갈래가 무엇인지 알 수 있다」가 있어야 갈래를 타는 이유가 생긴다.
        /// </summary>
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

                // [사고] 이 가지만 자리 검사를 빠뜨려서, 앞서 세운 관문 위에 «거리 0» 으로 겹쳤다.
                // 노드를 만드는 자리가 세 군데(성단·길·이 가지)인데 두 군데만 검사하면
                // 남은 한 군데가 반드시 사고를 낸다.
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

        // ───────────────────────── 노드 만들기 ─────────────────────────

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

        /// <summary>잔가지 — 작은 «전역» 특성. 계열 한정으로 두면 드랍이 어긋났을 때 성좌가 통째로 헛돈다.</summary>
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

        /// <summary>성단 중심 — 계열 한정 큰 특성. 갈래를 타는 보상이 여기 있다.</summary>
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

        // ───────────────────────── 셈 ─────────────────────────

        /// <summary>링이 오를수록 비싸진다. 반올림은 half-up 이다 — 은행가 반올림을 쓰면 값이 계단에서 어긋난다.</summary>
        public static int CostOf(int baseCost, int ring)
        {
            float raw = baseCost * Mathf.Pow(CommonConfig.Constellation.CostGrowth, ring);
            return Mathf.Max(1, Mathf.FloorToInt(raw + 0.5f));
        }

        /// <summary>
        /// 자리가 찼으면 길에 «수직으로» 비켜 세운다. 합칠 수 없는 노드(관문)를 위한 것이다.
        /// 한 번 비켜서 또 겹치면 반대쪽으로 한 번 더 본다 — 그래도 안 되면 그대로 둔다.
        /// </summary>
        private static void Nudge(ConstellationGraph graph, ref float x, ref float y,
                                  float dx, float dy, float dist)
        {
            if (FindNear(graph, x, y) == null)
            {
                return;
            }

            float offset = CommonConfig.Constellation.MergeDistance * 1.4f;

            // [사고] 길이가 0 인 길에서는 그냥 빠져나가게 해 뒀더니, 관문이 기존 노드 «위에» 그대로 앉았다.
            // 성단 두 개의 접속구가 이미 하나로 합쳐진 자리라 양 끝이 같은 노드였던 것이다.
            // 방향을 못 구하는 것과 비켜설 필요가 없는 것은 다르다 — 방향이 없으면 아무 쪽으로나 비킨다.
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

        /// <summary>이 자리에 이미 노드가 있나. 200개 남짓이라 선형 탐색으로 충분하다.</summary>
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
            // 파일 나열 순서에 기대지 않는다 — 순서가 흔들리면 스킬이 다른 성단으로 옮겨간다.
            list.Sort(CompareSkillId);
            return list;
        }

        private static int CompareSkillId(PartData a, PartData b)
        {
            return a.SkillId.CompareTo(b.SkillId);
        }
    }
}
