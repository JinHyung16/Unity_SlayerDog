using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_UI
{

    public class SafeArea : BaseBehaviour
    {
        [SerializeField] private RectTransform _rectTF;

        [Tooltip("안전 영역을 적용할 변. 세로 게임은 보통 Vertical 이면 충분하다.")]
        [SerializeField] private SafeAreaUtil.ApplySideType _applySide = SafeAreaUtil.ApplySideType.Vertical;

        private Rect _lastSafeArea;
        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            if (_rectTF == null)
                _rectTF = transform as RectTransform;
        }

        protected override void OnEnabled()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea == _lastSafeArea
                && Screen.width == _lastWidth
                && Screen.height == _lastHeight)
            {
                return;
            }
            Apply();
        }

        private void Apply()
        {
            if (_rectTF == null)
                return;

            _lastSafeArea = Screen.safeArea;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            SafeAreaUtil.ApplySafeArea(_rectTF, _applySide);
        }
    }
}
