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
        private EMode _appliedTo;
        private bool _orderApplied;

        private void ApplyOrder()
        {
            if (_orderApplied && _appliedTo == _mode)
                return;

            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<SpriteRenderer>(true);

            int order = _mode == EMode.Ground ? GroundOrder : OverOrder;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].sortingOrder = order;
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
