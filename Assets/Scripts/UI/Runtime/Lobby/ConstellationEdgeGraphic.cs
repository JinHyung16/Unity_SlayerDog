using System.Collections.Generic;
using Jinhyeong_Constellation;
using UnityEngine;
using UnityEngine.UI;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 성좌의 «선» 전부를 한 장의 메시로 굽는다.
    ///
    /// [사고] 처음엔 간선 하나를 <see cref="Image"/> 하나로 만들려 했다. 간선이 250개면
    /// 오브젝트 250개 · 드로우콜 250개가 되고, 그 250개가 노드 220개와 섞여 배칭이 끊긴다.
    /// 선은 «클릭할 필요가 없는 것»이라 오브젝트일 이유가 없다 — 정점만 있으면 된다.
    ///
    /// 좌표는 그래프 유닛 그대로 쓴다. 이 그래픽이 Content 의 자식이고 Content 가 통째로
    /// 확대·이동되므로, 줌이 바뀔 때마다 정점을 다시 계산할 이유가 없다.
    /// </summary>
    public class ConstellationEdgeGraphic : MaskableGraphic
    {
        private ConstellationGraph _graph;
        private readonly HashSet<int> _pathPreview = new HashSet<int>();

        /// <summary>계열별 색. 갈래가 색으로 갈려야 성좌가 「여섯 갈래」로 읽힌다.</summary>
        private Dictionary<int, Color> _familyColor;

        public void Setup(ConstellationGraph graph, Dictionary<int, Color> familyColor)
        {
            _graph = graph;
            _familyColor = familyColor;
            SetVerticesDirty();
        }

        /// <summary>미리보기 경로를 갈아끼운다. 살 경로가 어디로 나는지 선으로 보여야 값이 납득된다.</summary>
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

            // 선의 법선 방향으로 굵기의 절반씩 벌려 사각형 하나를 만든다.
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

        /// <summary>
        /// 선의 «색과 굵기»를 함께 정한다.
        ///
        /// [사고] 굵기를 하나로 두고 색만 바꿨더니, 선 240개가 얽힌 화면에서
        /// 「어디까지 이어 놨는지」가 안 보였다. 색만으로 구분하기엔 선이 너무 가늘다.
        /// 산 길은 굵게, 안 산 길은 가늘게 — 굵기가 먼저 눈에 들어온다.
        /// </summary>
        private void Resolve(ConstellationNode a, ConstellationNode b, out Color color, out float width)
        {
            bool previewA = _pathPreview.Contains(a.Id);
            bool previewB = _pathPreview.Contains(b.Id);
            bool ownedA = ConstellationManager.IsOwned(a.Id);
            bool ownedB = ConstellationManager.IsOwned(b.Id);

            // 살 경로는 금색으로 «지나간다». 양끝 중 하나만 미리보기여도 그 선을 밟는다.
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

            // 안 산 선도 계열 색을 «아주 옅게» 띤다. 전부 같은 회색이면 갈래가 구분되지 않는다.
            color = Color.Lerp(ConstellationPalette.EdgeIdle, FamilyColor(a, b), ConstellationPalette.EdgeIdleTint);
            width = ConstellationPalette.EdgeWidthIdle;
        }

        private Color FamilyColor(ConstellationNode a, ConstellationNode b)
        {
            if (_familyColor == null)
            {
                return ConstellationPalette.EdgeOwned;
            }
            // 코어 쪽 간선은 계열이 없다. 계열이 붙은 끝의 색을 따라간다.
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
