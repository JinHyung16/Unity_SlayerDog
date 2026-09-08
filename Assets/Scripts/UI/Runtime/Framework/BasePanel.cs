using UnityEngine;

namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// Window 안의 큰 덩어리(탭 내용 · 서브 화면). **탭을 처음 누를 때 로드**되고, 안 본 탭은 존재하지 않는다.
    ///
    /// 수명 — Open(window) → Opening() 훅 / 탭 전환은 Show()·Hide() / 창이 닫히면 Close() → OnClose() 훅.
    /// 풀 반납은 <see cref="UILoader.Restore"/> 가 <see cref="Clear"/> 를 부르며 한다.
    /// </summary>
    public abstract class BasePanel : BaseComponent
    {
        protected BaseWindow Window { get; private set; }

        public bool IsOpen { get; private set; }

        public void Open(BaseWindow window)
        {
            Clear();
            Window = window;
            IsOpen = true;
            gameObject.SetActive(true);
            Opening();
        }

        /// <summary>탭으로 다시 들어왔다. 값을 새로 읽는다 — 다른 탭에서 바뀐 것을 들고 있으면 안 된다.</summary>
        public void Show()
        {
            if (IsOpen == false)
            {
                return;
            }
            gameObject.SetActive(true);
            OnReShown();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Close()
        {
            if (IsOpen == false)
            {
                return;
            }
            IsOpen = false;
            OnClose();
            Window = null;
            gameObject.SetActive(false);
        }

        public override void Clear()
        {
            Close();
            base.Clear();
        }

        /// <summary>데이터 읽어 UI 채우기. **여기서만 채운다.**</summary>
        protected virtual void Opening()
        {
        }

        /// <summary>listener 해제 · 구독 해제 · 조각 풀 반납 · 참조 null.</summary>
        protected virtual void OnClose()
        {
        }

        /// <summary>탭 재진입. 기본은 아무것도 안 한다 — 값이 바뀔 수 있는 패널만 덮어쓴다.</summary>
        protected virtual void OnReShown()
        {
        }
    }
}
