using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Constellation
{

    /// <summary>
    /// 노드와 간선의 그릇. 간선은 **방향 없는 쌍**이고, 양쪽 노드의 이웃 목록에 서로를 넣어 둔다 —
    /// 구매 판정과 경로 탐색이 인접만 보기 때문에 이 형태가 곧 질의 형태다.
    ///
    /// 트리가 아니라 그래프다. 같은 링의 이웃 성단이 서로 이어지면 순환이 생기고,
    /// 그 순환이 「돌아가는 길」이 되어 선택지가 된다. 부모/자식으로 짜면 이걸 담지 못한다.
    /// </summary>
    public class ConstellationGraph
    {
        public readonly List<ConstellationNode> Nodes = new List<ConstellationNode>(256);

        /// <summary>간선 목록 — (a, b) 쌍을 한 번씩만 담는다. 그리기가 이 목록을 그대로 훑는다.</summary>
        public readonly List<int> EdgeA = new List<int>(320);
        public readonly List<int> EdgeB = new List<int>(320);

        private readonly Dictionary<int, ConstellationNode> _byId = new Dictionary<int, ConstellationNode>(256);
        private readonly HashSet<long> _edgeSet = new HashSet<long>();

        public int EdgeCount { get { return EdgeA.Count; } }

        /// <summary>
        /// 성좌가 실제로 차지하는 «가로 세로». 반경(원) 하나로 맞추면 눌린 성좌에서 가로가 크게 남는다 —
        /// 담아야 하는 것은 원이 아니라 상자다.
        /// </summary>
        public Vector2 Extent
        {
            get
            {
                float minX = 0f;
                float maxX = 0f;
                float minY = 0f;
                float maxY = 0f;
                for (int i = 0; i < Nodes.Count; i++)
                {
                    ConstellationNode n = Nodes[i];
                    if (n.X < minX)
                    {
                        minX = n.X;
                    }
                    if (n.X > maxX)
                    {
                        maxX = n.X;
                    }
                    if (n.Y < minY)
                    {
                        minY = n.Y;
                    }
                    if (n.Y > maxY)
                    {
                        maxY = n.Y;
                    }
                }
                return new Vector2(maxX - minX, maxY - minY);
            }
        }

        /// <summary>코어에서 가장 먼 노드까지의 거리. 화면에 «전체를 담는» 배율을 여기서 뽑는다.</summary>
        public float Radius
        {
            get
            {
                float max = 0f;
                for (int i = 0; i < Nodes.Count; i++)
                {
                    float x = Nodes[i].X;
                    float y = Nodes[i].Y;
                    float r = x * x + y * y;
                    if (r > max)
                    {
                        max = r;
                    }
                }
                return Mathf.Sqrt(max);
            }
        }

        public void Clear()
        {
            Nodes.Clear();
            EdgeA.Clear();
            EdgeB.Clear();
            _byId.Clear();
            _edgeSet.Clear();
        }

        public ConstellationNode Get(int id)
        {
            return _byId.TryGetValue(id, out ConstellationNode v) ? v : null;
        }

        public void Add(ConstellationNode node)
        {
            if (node == null)
            {
                return;
            }
            Nodes.Add(node);
            _byId[node.Id] = node;
        }

        /// <summary>
        /// 간선을 잇는다. 같은 쌍이 두 번 와도 한 번만 들어간다 —
        /// 성단 결선과 길 연결이 같은 자리를 두 번 잇는 경우가 실제로 나온다.
        /// </summary>
        public void Link(int a, int b)
        {
            if (a == b)
            {
                return;
            }
            int lo = a < b ? a : b;
            int hi = a < b ? b : a;
            long key = ((long)lo << 32) | (uint)hi;
            if (_edgeSet.Add(key) == false)
            {
                return;
            }

            ConstellationNode na = Get(a);
            ConstellationNode nb = Get(b);
            if (na == null || nb == null)
            {
                return;
            }
            na.Neighbors.Add(b);
            nb.Neighbors.Add(a);
            EdgeA.Add(a);
            EdgeB.Add(b);
        }
    }
}
