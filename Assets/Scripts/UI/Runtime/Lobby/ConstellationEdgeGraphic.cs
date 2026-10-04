using System.Collections.Generic;
using Jinhyeong_Constellation;
using UnityEngine;
using UnityEngine.UI;

namespace Jinhyeong_UI.Lobby
{

    public class ConstellationEdgeGraphic : MaskableGraphic
    {
        private ConstellationGraph _graph;
        private readonly HashSet<int> _pathPreview = new HashSet<int>();

        private Dictionary<int, Color> _familyColor;

        public void Setup(ConstellationGraph graph, Dictionary<int, Color> familyColor)
        {
            _graph = graph;
            _familyColor = familyColor;
            SetVerticesDirty();
        }

        public void SetPathPreview(List<int> path)
        {
            _pathPreview.Clear();
            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    _pathPreview.Add(path[i]);
                }
            }
            SetVerticesDirty();
        }

        public void Refresh()
        {
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_graph == null)
            {
                return;
            }

            for (int i = 0; i < _graph.EdgeCount; i++)
            {
                ConstellationNode a = _graph.Get(_graph.EdgeA[i]);
                ConstellationNode b = _graph.Get(_graph.EdgeB[i]);
                if (a == null || b == null)
                {
                    continue;
                }
                AddLine(vh, a, b);
            }
        }

        private void AddLine(VertexHelper vh, ConstellationNode a, ConstellationNode b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f)
            {
                return;
            }

            Color color;
            float width;
            Resolve(a, b, out color, out width);

            float nx = -dy / len * width * 0.5f;
            float ny = dx / len * width * 0.5f;

            UIVertex v = UIVertex.simpleVert;
            v.color = color;

            int start = vh.currentVertCount;

            v.position = new Vector3(a.X + nx, a.Y + ny, 0f);
            vh.AddVert(v);
            v.position = new Vector3(b.X + nx, b.Y + ny, 0f);
            vh.AddVert(v);
            v.position = new Vector3(b.X - nx, b.Y - ny, 0f);
            vh.AddVert(v);
            v.position = new Vector3(a.X - nx, a.Y - ny, 0f);
            vh.AddVert(v);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }

        private void Resolve(ConstellationNode a, ConstellationNode b, out Color color, out float width)
        {
            bool previewA = _pathPreview.Contains(a.Id);
            bool previewB = _pathPreview.Contains(b.Id);
            bool ownedA = ConstellationManager.IsOwned(a.Id);
            bool ownedB = ConstellationManager.IsOwned(b.Id);

            if ((previewA && (previewB || ownedB)) || (previewB && ownedA))
            {
                color = ConstellationPalette.EdgePath;
                width = ConstellationPalette.EdgeWidthPath;
                return;
            }
            if (ownedA && ownedB)
            {
                color = FamilyColor(a, b);
                width = ConstellationPalette.EdgeWidthOwned;
                return;
            }

            color = Color.Lerp(ConstellationPalette.EdgeIdle, FamilyColor(a, b), ConstellationPalette.EdgeIdleTint);
            width = ConstellationPalette.EdgeWidthIdle;
        }

        private Color FamilyColor(ConstellationNode a, ConstellationNode b)
        {
            if (_familyColor == null)
            {
                return ConstellationPalette.EdgeOwned;
            }
            if (_familyColor.TryGetValue((int)a.Family, out Color ca))
            {
                return ca;
            }
            if (_familyColor.TryGetValue((int)b.Family, out Color cb))
            {
                return cb;
            }
            return ConstellationPalette.EdgeOwned;
        }
    }
}
