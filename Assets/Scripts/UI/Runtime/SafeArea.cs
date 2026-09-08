using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_UI
{

    /// <summary>
    /// 붙은 RectTransform 을 안전 영역에 맞춘다.
    ///
    /// OnEnable 한 번만으로는 부족하다. 기기 회전·해상도 변경·에디터 게임뷰 크기 변경 때
    /// safeArea 가 바뀌므로 값이 달라졌는지 지켜보다 다시 적용한다.
    /// </summary>
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
            // 매 프레임 도는 비용이 아깝지만, 회전 이벤트를 신뢰할 수 없는 기기가 있어
            // 값 비교로 감시하는 쪽이 안전하다. 비교는 실제 적용보다 훨씬 싸다.
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
