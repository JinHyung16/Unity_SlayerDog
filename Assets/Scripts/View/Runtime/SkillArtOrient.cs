using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;

namespace Jinhyeong_View
{
    public class SkillArtOrient : BaseBehaviour
    {
        public enum EMode
        {
            Ground,
            Billboard
        }

        [SerializeField] private EMode _mode = EMode.Ground;

        public EMode Mode { get { return _mode; } set { _mode = value; } }

        private const float ArtForwardYaw = -90f;

        private const int GroundOrder = 7;

        private const int OverOrder = 20;

        private SpriteRenderer[] _renderers;
        private int[] _baseOrders;
        private int _minBaseOrder;
        private EMode _appliedTo;
        private bool _orderApplied;

        private void ApplyOrder()
        {
            if (_orderApplied && _appliedTo == _mode)
                return;

            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<SpriteRenderer>(true);
                // 프리팹이 정한 층 순서(번짐 아래 · 심지 위)를 기억해 두고 기준 순서 위에 그대로 얹는다
                // 전부 같은 순서로 덮으면 가산 번짐이 물건 심지 위에 그려져 제 색이 계열 색에 먹힌다
                _baseOrders = new int[_renderers.Length];
                _minBaseOrder = int.MaxValue;
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _baseOrders[i] = _renderers[i] != null ? _renderers[i].sortingOrder : 0;
                    if (_baseOrders[i] < _minBaseOrder)
                        _minBaseOrder = _baseOrders[i];
                }
            }

            int order = _mode == EMode.Ground ? GroundOrder : OverOrder;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].sortingOrder = order + (_baseOrders[i] - _minBaseOrder);
            }

            _appliedTo = _mode;
            _orderApplied = true;
        }

        private void LateUpdate()
        {
            ApplyOrder();

            if (_mode == EMode.Billboard)
            {
                Camera cam = CameraFollow.GameCamera;
                if (cam != null)
                    transform.rotation = cam.transform.rotation;
                return;
            }

            Vector3 fwd = transform.parent != null ? transform.parent.forward : Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f)
                fwd = Vector3.forward;

            float yaw = Quaternion.LookRotation(fwd, Vector3.up).eulerAngles.y;

            transform.rotation = Quaternion.Euler(90f, yaw + ArtForwardYaw, 0f);
        }
    }
}
