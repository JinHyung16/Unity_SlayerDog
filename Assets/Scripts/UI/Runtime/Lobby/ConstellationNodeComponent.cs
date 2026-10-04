using Jinhyeong_Constellation;
using Jinhyeong_UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Jinhyeong_UI.Lobby
{

    public class ConstellationNodeComponent : BaseComponent
    {
        public static readonly PrefabAuto<ConstellationNodeComponent> PrefabAuto =
            new PrefabAuto<ConstellationNodeComponent>(PrefabPath);
        public const string PrefabPath = "UI/Lobby/ConstellationNode";

        public interface IListener
        {
            void OnClickStarNode(int nodeId);
        }

        [SerializeField] private Image _bg;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _frame;
        [SerializeField] private Button _button;

        private IListener _listener;
        private int _nodeId;

        public int NodeId
        {
            get { return _nodeId; }
        }

        public void Open(IListener listener, ConstellationNode node, Color familyColor)
        {
            Clear();
            if (node == null)
            {
                return;
            }

            _listener = listener;
            _nodeId = node.Id;

            float size = ConstellationPalette.SizeOf(node.Type);
            RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            RectTransform.pivot = new Vector2(0.5f, 0.5f);
            RectTransform.sizeDelta = new Vector2(size, size);
            RectTransform.anchoredPosition = new Vector2(node.X, node.Y);

            SetIcon(node);
            Paint(node, familyColor);

            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }
        }

        private void SetIcon(ConstellationNode node)
        {
            if (_icon == null)
            {
                return;
            }

            Sprite sprite = node.Type == EConstellationNodeType.Skill && node.SkillId != 0
                ? UISkin.SkillIcon(node.SkillId)
                : null;

            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
        }

        public void Paint(ConstellationNode node, Color familyColor)
        {
            if (node == null)
            {
                return;
            }

            if (node.Type == EConstellationNodeType.Core)
            {
                Apply(ConstellationPalette.CoreColor, ConstellationPalette.FrameOwned, Color.white);
                return;
            }

            bool owned = ConstellationManager.IsOwned(node.Id);
            if (owned)
            {
                Apply(familyColor, ConstellationPalette.FrameOwned, ConstellationPalette.IconOwned);
                return;
            }

            if (ConstellationManager.IsGateOpen(node) == false)
            {
                Apply(Color.Lerp(ConstellationPalette.NodeLocked, ConstellationPalette.NodeGateShut, 0.8f),
                    ConstellationPalette.FrameGateShut, ConstellationPalette.IconLocked);
                return;
            }

            if (ConstellationManager.CanReach(node))
            {
                Apply(Color.Lerp(ConstellationPalette.NodeLocked, familyColor, ConstellationPalette.ReachableTint),
                    ConstellationPalette.FrameReachable, ConstellationPalette.IconReachable);
                return;
            }

            Apply(Color.Lerp(ConstellationPalette.NodeLocked, familyColor, ConstellationPalette.LockedTint),
                ConstellationPalette.FrameLocked, ConstellationPalette.IconLocked);
        }

        private void Apply(Color bg, Color frame, Color icon)
        {
            if (_bg != null)
            {
                _bg.color = bg;
            }
            if (_frame != null)
            {
                _frame.color = frame;
            }
            if (_icon != null)
            {
                _icon.color = icon;
            }
        }

        public void SetSelected(bool on)
        {
            if (_frame != null && on)
            {
                _frame.color = ConstellationPalette.FrameSelected;
            }
        }

        public override void Clear()
        {
            _listener = null;
            _nodeId = 0;
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
            }
            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.enabled = false;
            }
            base.Clear();
        }

        private void OnClick()
        {
            if (_listener != null)
            {
                _listener.OnClickStarNode(_nodeId);
            }
        }
    }
}
