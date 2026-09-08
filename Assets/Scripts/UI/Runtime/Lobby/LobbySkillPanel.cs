using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Jinhyeong_Common;
using Jinhyeong_Constellation;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 성좌 스킬 트리. 계열 여섯 갈래가 코어에서 바깥으로 뻗고, 슈퍼 DNA 로 «경로»를 사서 넓혀 간다.
    ///
    /// 별에는 이름을 달지 않는다 — 스킬은 아이콘으로 알아보고, 이름과 값은 «누른 별 위»에 뜨는
    /// 설명 패널이 말한다. 200개에 글자를 달면 성좌가 글자밭이 되고, 화면 아래 고정 줄에 적으면
    /// 「지금 보고 있는 별」과 「글자」가 화면 양 끝으로 갈라져 눈이 왕복한다.
    ///
    /// 판정은 전부 <see cref="ConstellationManager"/>(시스템)가 하고 여기는 그리기와 입력만 한다.
    /// 노드가 200개라 「무엇이 바뀌었나」를 따지지 않고 상태가 바뀌면 색만 통째로 다시 칠한다 —
    /// 오브젝트를 다시 만드는 게 아니라 색만 도는 것이라 싸다.
    /// </summary>
    public class LobbySkillPanel : BasePanel, ConstellationNodeComponent.IListener
    {
        public static readonly PrefabAuto<LobbySkillPanel> PrefabAuto = new PrefabAuto<LobbySkillPanel>(PrefabPath);
        public const string PrefabPath = "UI/Lobby/SkillPanel";

        /// <summary>설명 패널과 별 사이 간격(px). 붙여 놓으면 어느 별을 가리키는지 애매하다.</summary>
        private const float TipGap = 10f;

        [SerializeField] private TMP_Text _hint;
        [SerializeField] private RectTransform _content;
        [SerializeField] private ConstellationEdgeGraphic _edges;
        [SerializeField] private ConstellationView _view;
        [SerializeField] private RectTransform _tooltip;
        [SerializeField] private TMP_Text _tipName;
        [SerializeField] private TMP_Text _tipDesc;
        [SerializeField] private TMP_Text _tipCost;

        private readonly List<ConstellationNodeComponent> _nodes = new List<ConstellationNodeComponent>(256);
        private readonly Dictionary<int, ConstellationNodeComponent> _byId =
            new Dictionary<int, ConstellationNodeComponent>(256);
        private readonly List<int> _path = new List<int>(24);
        private Dictionary<int, Color> _familyColor;

        /// <summary>지금 고른 별. 되돌릴 수 없는 지출이라 «두 번 눌러야» 산다 (설정의 초기화와 같은 규칙).</summary>
        private int _selected;

        protected override void Opening()
        {
            ConstellationManager.Ensure();
            _familyColor = ConstellationPalette.BuildFamilyColors();

            ConstellationManager.OnChanged -= HandleStarChanged;
            ConstellationManager.OnChanged += HandleStarChanged;

            if (_view != null)
            {
                _view.OnZoomBandChanged -= HandleZoomChanged;
                _view.OnZoomBandChanged += HandleZoomChanged;
            }

            BuildNodes();

            if (_edges != null)
            {
                _edges.Setup(ConstellationManager.Graph, _familyColor);
            }

            // 별을 다 세운 «뒤에» 맞춘다 — 크기는 별에서 나온다.
            if (_view != null)
            {
                _view.FitTo(ConstellationManager.Graph.Extent);
            }

            Select(0);
            RefreshHint();
        }

        /// <summary>다른 탭에서 초기화가 돌면 성좌가 통째로 비어 있을 수 있다. 다시 들어오면 새로 세운다.</summary>
        protected override void OnReShown()
        {
            ConstellationManager.Ensure();
            if (_nodes.Count == 0)
            {
                BuildNodes();
                if (_edges != null)
                {
                    _edges.Setup(ConstellationManager.Graph, _familyColor);
                }
            }
            if (_view != null)
            {
                _view.FitTo(ConstellationManager.Graph.Extent);
            }
            Select(0);
            RepaintAll();
            RefreshHint();
        }

        protected override void OnClose()
        {
            ConstellationManager.OnChanged -= HandleStarChanged;
            if (_view != null)
            {
                _view.OnZoomBandChanged -= HandleZoomChanged;
            }
            Release();
            _selected = 0;
        }

        // ───────────────────────── 세우기 ─────────────────────────

        private void BuildNodes()
        {
            Release();

            ConstellationGraph graph = ConstellationManager.Graph;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                ConstellationNode node = graph.Nodes[i];
                ConstellationNodeComponent comp = ConstellationNodeComponent.PrefabAuto.CreateForUI(_content);
                if (comp == null)
                {
                    continue;
                }
                comp.Open(this, node, ColorOf(node));
                _nodes.Add(comp);
                _byId[node.Id] = comp;
            }
        }

        private void Release()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                UILoader.Restore(_nodes[i]);
            }
            _nodes.Clear();
            _byId.Clear();
        }

        private Color ColorOf(ConstellationNode node)
        {
            if (_familyColor != null && _familyColor.TryGetValue((int)node.Family, out Color c))
            {
                return c;
            }
            return ConstellationPalette.CoreColor;
        }

        // ───────────────────────── 그리기 ─────────────────────────

        private void RepaintAll()
        {
            ConstellationGraph graph = ConstellationManager.Graph;
            for (int i = 0; i < _nodes.Count; i++)
            {
                ConstellationNode node = graph.Get(_nodes[i].NodeId);
                if (node == null)
                {
                    continue;
                }
                _nodes[i].Paint(node, ColorOf(node));
            }

            // 고른 별은 «마지막에» 덧칠한다. 먼저 칠하면 위 반복문이 도로 지운다.
            if (_selected != 0 && _byId.TryGetValue(_selected, out ConstellationNodeComponent picked))
            {
                picked.SetSelected(true);
            }
            if (_edges != null)
            {
                _edges.Refresh();
            }
        }

        private void RefreshHint()
        {
            if (_hint == null)
            {
                return;
            }
            _hint.text = $"슈퍼 DNA {SaveData.SuperDna} · 별 {ConstellationManager.OwnedCount}/{ConstellationManager.Graph.Nodes.Count - 1}"
                         + " · 끌어서 이동 · 휠로 확대";
        }

        /// <summary>배율이 바뀌면 설명 패널이 별에서 떨어진다. 자리만 다시 잡는다.</summary>
        private void HandleZoomChanged()
        {
            PlaceTooltip();
        }

        private void HandleStarChanged()
        {
            RepaintAll();
            RefreshHint();
        }

        // ───────────────────────── 고르기 · 사기 ─────────────────────────

        /// <summary>
        /// 고른 별까지의 «경로»를 미리 보여 준다. 성좌에서 한 칸만 보여 주면
        /// 「저기까지 얼마나 드나」를 사람이 머릿속으로 더해야 한다.
        /// </summary>
        private void Select(int nodeId)
        {
            _selected = nodeId;

            if (nodeId == 0)
            {
                _path.Clear();
                if (_edges != null)
                {
                    _edges.SetPathPreview(null);
                }
                ShowTooltip(false);
                RepaintAll();
                return;
            }

            ConstellationNode node = ConstellationManager.Graph.Get(nodeId);
            if (node == null)
            {
                return;
            }

            bool hasPath = ConstellationManager.PathTo(nodeId, _path);
            if (_edges != null)
            {
                _edges.SetPathPreview(hasPath ? _path : null);
            }

            if (hasPath == false)
            {
                string why = ConstellationManager.IsOwned(nodeId) ? "이미 열었다" : "아직 길이 닿지 않는다";
                SetTip(node.DisplayName, node.Description, why, false);
            }
            else
            {
                int cost = ConstellationManager.PathCost(_path);
                bool afford = SaveData.SuperDna >= cost;
                string tail = afford
                    ? $"별 {_path.Count}개 · DNA {cost} — 한 번 더 누르면 산다"
                    : $"별 {_path.Count}개 · DNA {cost} — {cost - SaveData.SuperDna} 모자란다";
                SetTip(node.DisplayName, node.Description, tail, afford == false);
            }

            ShowTooltip(true);
            PlaceTooltip();
            RepaintAll();
        }

        private void SetTip(string title, string desc, string cost, bool warn)
        {
            if (_tipName != null)
            {
                _tipName.text = title;
            }
            if (_tipDesc != null)
            {
                _tipDesc.text = desc;
            }
            if (_tipCost != null)
            {
                _tipCost.text = cost;
                _tipCost.color = warn ? UIBuild.ColorWarn : UIBuild.ColorGold;
            }
        }

        private void ShowTooltip(bool on)
        {
            if (_tooltip != null)
            {
                _tooltip.gameObject.SetActive(on);
            }
        }

        /// <summary>
        /// 설명 패널을 고른 별 «바로 위»에 둔다.
        ///
        /// 패널은 창(Viewport) 밖, 화면 위에 얹혀 있다. 창 «안»에 두면 잘라내기(RectMask2D)에 걸려
        /// 가장자리 별을 눌렀을 때 설명이 반쯤 잘린다.
        ///
        /// 위로 못 올릴 만큼 별이 꼭대기에 있으면 «아래»로 뒤집는다 —
        /// 화면 밖으로 나가면 아예 안 보이는 것과 같다.
        /// </summary>
        private void PlaceTooltip()
        {
            if (_tooltip == null || _view == null || _tooltip.gameObject.activeSelf == false)
            {
                return;
            }

            ConstellationNode node = ConstellationManager.Graph.Get(_selected);
            RectTransform area = _tooltip.parent as RectTransform;
            RectTransform viewRt = _view.transform as RectTransform;
            if (node == null || area == null || viewRt == null)
            {
                return;
            }

            // 창 가운데 기준 좌표 → 패널(부모) 기준 좌표. 창이 부모 안 어디에 있든 따라간다.
            Vector2 inView = _view.GraphToLocal(new Vector2(node.X, node.Y));
            Vector3 world = viewRt.TransformPoint(inView);
            Vector2 local = area.InverseTransformPoint(world);

            float half = ConstellationPalette.SizeOf(node.Type) * 0.5f * _view.Zoom;
            Vector2 tipSize = _tooltip.rect.size;

            float y = local.y + half + TipGap;
            _tooltip.pivot = new Vector2(0.5f, 0f);
            if (y + tipSize.y > area.rect.height * 0.5f)
            {
                y = local.y - half - TipGap;
                _tooltip.pivot = new Vector2(0.5f, 1f);
            }

            // 좌우로도 화면 안에 가둔다. 밖으로 나가면 못 읽는다.
            float limit = Mathf.Max(0f, area.rect.width * 0.5f - tipSize.x * 0.5f - 8f);
            _tooltip.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -limit, limit), y);
        }

        void ConstellationNodeComponent.IListener.OnClickStarNode(int nodeId)
        {
            // 첫 클릭은 «보여 주기»다. 경로 하나가 수십 DNA라 눌리자마자 나가면 사고가 된다.
            if (_selected != nodeId)
            {
                Select(nodeId);
                return;
            }

            if (ConstellationManager.BuyPath(nodeId) == false)
            {
                // 못 사는 이유는 이미 설명 패널에 적혀 있다. 여기서 또 말하지 않는다.
                return;
            }

            // 산 뒤엔 그 자리에서 «다음 한 걸음»이 보여야 한다. 선택은 유지하고 경로만 다시 푼다.
            Select(nodeId);
        }

        /// <summary>하네스가 성좌를 실제로 사 보게 하는 문. 사람 손 없이 구매 경로를 재려면 필요하다.</summary>
        public bool DebugBuyFirstAffordable()
        {
            ConstellationGraph graph = ConstellationManager.Graph;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                ConstellationNode node = graph.Nodes[i];
                if (ConstellationManager.CanReach(node) == false)
                {
                    continue;
                }
                if (ConstellationManager.BuyPath(node.Id))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 하네스가 화면을 찍어 보게 하는 문. 사람 손 없이 「눌린 상태」를 만든다.
        ///
        /// 가장 «먼» 별을 고른다 — 경로가 길어야 굵은 금색 선이 성좌를 가로질러
        /// 「선 굵기로 구분이 되는가」를 눈으로 잴 수 있다. 가까운 별을 고르면 선 두어 개뿐이라
        /// 굵기 차이를 판정할 수 없다. 사지는 않으므로 저장은 건드리지 않는다.
        /// </summary>
        public void DebugSelectShowcase()
        {
            ConstellationGraph graph = ConstellationManager.Graph;
            List<int> probe = new List<int>(32);
            ConstellationNode far = null;
            float best = -1f;

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                ConstellationNode n = graph.Nodes[i];
                if (n.Type == EConstellationNodeType.Core)
                {
                    continue;
                }
                // 길이 «실제로 닿는» 것 중에서 고른다. 관문 너머는 경로가 안 나므로
                // 그걸 고르면 굵은 선이 하나도 안 그려져 아무것도 재지 못한다.
                if (ConstellationManager.PathTo(n.Id, probe) == false)
                {
                    continue;
                }
                float d = n.X * n.X + n.Y * n.Y;
                if (d > best)
                {
                    best = d;
                    far = n;
                }
            }
            if (far != null)
            {
                Select(far.Id);
            }
        }
    }
}
