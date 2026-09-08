using Jinhyeong_Constellation;
using Jinhyeong_UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 성좌의 별 하나 — 바탕 · 아이콘 · 테두리 세 겹.
    ///
    /// 세 겹인 이유는 «셋이 서로 다른 것을 말하기» 때문이다.
    ///   바탕   어느 계열인가 (색)
    ///   아이콘 무슨 스킬인가 (그림) — 스킬 노드에만
    ///   테두리 지금 살 수 있는가 (밝기)
    /// 한 겹으로 합치면 색 하나가 세 가지를 동시에 말해야 하고, 그러면 아무것도 안 보인다.
    ///
    /// [사고] 처음엔 바탕 하나뿐이었다. 200개가 전부 「색만 조금 다른 원」이라
    /// 무엇이 스킬이고 무엇을 살 수 있는지 화면에서 알 수 없었다.
    ///
    /// 이름표는 달지 않는다. 200개에 글자를 달면 성좌가 글자밭이 되고, 이름은 누른 뒤
    /// 그 위에 뜨는 설명 패널이 말한다.
    ///
    /// 좌표는 그래프 유닛 그대로 넣는다. Content 가 통째로 확대·이동되므로 줌이 바뀌어도
    /// 노드를 다시 배치하지 않는다.
    /// </summary>
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

        /// <summary>스킬 노드만 그림을 문다. 나머지는 색과 크기로 구분된다.</summary>
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

        /// <summary>
        /// 상태를 색으로 말한다. 산 것 · 지금 살 수 있는 것 · 아직 못 가는 것 · 관문이 막은 것 —
        /// 넷이 구분되지 않으면 트리를 봐도 다음에 뭘 할지 알 수 없다.
        ///
        /// 바탕은 «계열», 테두리는 «상태»다. 못 가는 노드는 바탕을 크게 죽여(Dim) 뒤로 물리고,
        /// 살 수 있는 노드만 테두리가 밝게 서서 눈에 걸린다.
        /// </summary>
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

        /// <summary>고른 별을 도드라지게. 테두리 하나로 말한다 — 크기를 바꾸면 옆 별을 덮는다.</summary>
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
