using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Constellation
{

    public class ConstellationGraph
    {
        public readonly List<ConstellationNode> Nodes = new List<ConstellationNode>(256);

        public readonly List<int> EdgeA = new List<int>(320);
        public readonly List<int> EdgeB = new List<int>(320);

        private readonly Dictionary<int, ConstellationNode> _byId = new Dictionary<int, ConstellationNode>(256);
        private readonly HashSet<long> _edgeSet = new HashSet<long>();

        public int EdgeCount { get { return EdgeA.Count; } }

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
