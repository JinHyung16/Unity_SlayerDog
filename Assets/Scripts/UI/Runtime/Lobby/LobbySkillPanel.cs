using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Jinhyeong_Common;
using Jinhyeong_Constellation;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    public class LobbySkillPanel : BasePanel, ConstellationNodeComponent.IListener
    {
        public static readonly PrefabAuto<LobbySkillPanel> PrefabAuto = new PrefabAuto<LobbySkillPanel>(PrefabPath);
        public const string PrefabPath = "UI/Lobby/SkillPanel";

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

            if (_view != null)
            {
                _view.FitTo(OpeningExtent());
            }

            Select(0);
            RefreshHint();
        }

        // 처음 열 때는 전체가 아니라 코어 주변(링 0~2)을 화면에 채운다 — 전체를 맞추면 노드가 점이 되고 이름이 안 뜬다 (10-04 사용자 UX 지적).
        // 휠로 줄이면 전체가 보인다
        private static Vector2 OpeningExtent()
        {
            float[] ring = Jinhyeong_Common.CommonConfig.Constellation.RingRadius;
            float r = ring[Mathf.Min(2, ring.Length - 1)];
            Vector2 full = ConstellationManager.Graph.Extent;
            return new Vector2(Mathf.Min(full.x, r * 2f), Mathf.Min(full.y, r * 2f * Jinhyeong_Common.CommonConfig.Constellation.AspectY));
        }

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
                _view.FitTo(OpeningExtent());
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
            _hint.text = Loc.F("Skill_Hint", "슈퍼 DNA {0} · 별 {1}/{2} · 끌어서 이동 · 휠로 확대",
                SaveData.SuperDna, ConstellationManager.OwnedCount, ConstellationManager.Graph.Nodes.Count - 1);
        }

        private void HandleZoomChanged()
        {
            PlaceTooltip();
        }

        private void HandleStarChanged()
        {
            RepaintAll();
            RefreshHint();
        }

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
                string why = ConstellationManager.IsOwned(nodeId) ? Loc.T("Skill_AlreadyOwned", "이미 열었다") : Loc.T("Skill_Unreachable", "아직 길이 닿지 않습니다");
                SetTip(node.DisplayName, node.Description, why, false);
            }
            else
            {
                int cost = ConstellationManager.PathCost(_path);
                bool afford = SaveData.SuperDna >= cost;
                string tail = afford
                    ? Loc.F("Skill_CostAfford", "별 {0}개 · DNA {1} — 한 번 더 누르면 산다", _path.Count, cost)
                    : Loc.F("Skill_CostShort", "별 {0}개 · DNA {1} — {2} 부족합니다", _path.Count, cost, cost - SaveData.SuperDna);
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

            float limit = Mathf.Max(0f, area.rect.width * 0.5f - tipSize.x * 0.5f - 8f);
            _tooltip.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -limit, limit), y);
        }

        void ConstellationNodeComponent.IListener.OnClickStarNode(int nodeId)
        {
            if (_selected != nodeId)
            {
                Select(nodeId);
                return;
            }

            if (ConstellationManager.BuyPath(nodeId) == false)
            {
                return;
            }

            Select(nodeId);
        }

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
