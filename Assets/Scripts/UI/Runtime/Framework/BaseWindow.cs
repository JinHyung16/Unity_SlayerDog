using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// 화면 하나. **Canvas 를 가진 유일한 계층**이다 — Panel · Component 에는 Canvas 를 달지 않는다.
    ///
    /// 수명은 정해진 훅으로만 흐른다 (레퍼런스 ProjectSample 의 BaseWindow 와 같은 계약):
    /// <code>
    /// Open(...)  → OpenInternal(onOpenBefore) → 상태 Opening → onOpenBefore → Opening() → 상태 Opened → 활성화
    /// Close()    → HandleCanClose()? → BeforeClosed() → 상태 Closed → OnClose() → 비활성화 → 코루틴 정지
    /// </code>
    /// 깊이(sortingOrder)는 창이 정하지 않는다 — <see cref="UIManagement"/> 가 대역과 여는 순서로 준다.
    /// <c>Awake</c>/<c>Start</c> 에서 데이터를 읽지 않는다. 풀에서 재사용되면 두 번째부터 안 돈다.
    /// </summary>
    public abstract class BaseWindow : BaseComponent
    {
        [Header("Canvas")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasScaler _scaler;

        [Header("Depth")]
        [SerializeField] private EWindowType _windowType = EWindowType.Normal;
        [Tooltip("WindowType 이 ConstantDepth 일 때만 쓴다. 명시적 예외.")]
        [SerializeField] private int _constantDepth;
        [Tooltip("전체 크기 창이면 아래 창의 캔버스를 끈다. 안 끄면 안 보이는 화면이 계속 리빌드된다.")]
        [SerializeField] private bool _isFullSizeWindow = true;

        [Header("Close")]
        [SerializeField] private ECloseType _closeType = ECloseType.Close;

        public Canvas Canvas { get { return _canvas; } }
        public CanvasScaler Scaler { get { return _scaler; } }
        public EWindowType WindowType { get { return _windowType; } }
        public int ConstantDepth { get { return _constantDepth; } }
        public bool IsFullSizeWindow { get { return _isFullSizeWindow; } }
        public EWindowState WindowState { get; private set; } = EWindowState.Closed;

        /// <summary>
        /// 이 창이 쓰는 패널 · 조각 프리팹 주소. <see cref="WindowFactory"/> 가 열기 전에 전부 올린다 —
        /// 그래야 이후 <see cref="UILoader.Get{T}"/> 가 동기로 돈다.
        /// </summary>
        public virtual IReadOnlyList<string> PreloadPaths { get { return Array.Empty<string>(); } }

        /// <summary>상태 전이 알림. <see cref="UIManagement"/> 가 구독해 깊이와 뒤 창 표시를 관리한다.</summary>
        public event Action<BaseWindow> OnStateChanged;

        public bool CanvasEnable
        {
            get { return _canvas != null && _canvas.enabled; }
            set
            {
                if (_canvas != null)
                {
                    _canvas.enabled = value;
                }
            }
        }

        // 부품 배선만 한다. 데이터를 읽지 않는다.
        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }
            if (_scaler == null)
            {
                _scaler = GetComponent<CanvasScaler>();
            }
        }

        /// <summary>파생 창의 <c>Open(...)</c> 이 부른다. listener 저장 같은 「열기 전 준비」를 onOpenBefore 로 넘긴다.</summary>
        protected void OpenInternal(Action onOpenBefore)
        {
            if (IsOpen())
            {
                return;
            }

            SetState(EWindowState.Opening);
            if (onOpenBefore != null)
            {
                onOpenBefore.Invoke();
            }
            Opening();
            SetState(EWindowState.Opened);
            SetEnable(true);
        }

        public void Close()
        {
            if (IsOpen() == false)
            {
                return;
            }
            // 상시 UI 는 Close 로 안 닫힌다. 닫으려면 ForcedClose — 실수로 로비 메뉴가 사라지는 것을 막는다.
            if (_windowType == EWindowType.Passive)
            {
                return;
            }
            if (_closeType == ECloseType.Handle && HandleCanClose() == false)
            {
                return;
            }
            BaseClose();
        }

        /// <summary>조건부로 못 닫는 창을 시스템이 강제로 닫을 때.</summary>
        public void ForcedClose()
        {
            if (IsOpen() == false)
            {
                return;
            }
            BaseClose();
        }

        private void BaseClose()
        {
            BeforeClosed();
            SetState(EWindowState.Closed);
            OnClose();
            SetEnable(false);
            StopAllCoroutines();
        }

        public bool IsOpen()
        {
            return WindowState != EWindowState.Closed;
        }

        public bool IsOpening()
        {
            return WindowState == EWindowState.Opening;
        }

        public void SetDepth(int depth)
        {
            if (_canvas == null)
            {
                return;
            }
            if (_canvas.sortingOrder != depth)
            {
                _canvas.sortingOrder = depth;
            }
        }

        public int GetDepth()
        {
            return _canvas != null ? _canvas.sortingOrder : -1;
        }

        public void SetEnable(bool isEnable)
        {
            gameObject.SetActive(isEnable);
        }

        public void OtherWindowOpened()
        {
            OnOtherWindowOpened();
        }

        public void ReOpened()
        {
            OnReOpened();
        }

        private void SetState(EWindowState state)
        {
            if (WindowState == state)
            {
                return;
            }
            WindowState = state;
            if (OnStateChanged != null)
            {
                OnStateChanged.Invoke(this);
            }
        }

        /// <summary>데이터 읽어 UI 채우기. **여기서만 채운다.**</summary>
        protected virtual void Opening()
        {
        }

        /// <summary>닫히기 전 확인(연출 대기 · 저장).</summary>
        protected virtual void BeforeClosed()
        {
        }

        /// <summary>listener 해제 · 구독 해제 · 풀 반납 · **참조 null**. 안 끊으면 닫힌 창이 옛 데이터를 물고 있다.</summary>
        protected virtual void OnClose()
        {
        }

        /// <summary>조건부로 닫힘을 막을 때만 (입력 중 등). CloseType 이 Handle 일 때 묻는다.</summary>
        protected virtual bool HandleCanClose()
        {
            return true;
        }

        /// <summary>다른 창이 위에 열렸다 (타이머 정지 등).</summary>
        protected virtual void OnOtherWindowOpened()
        {
        }

        /// <summary>위 창이 닫혀 다시 보인다 — **여기서 값을 새로 읽는다.**</summary>
        protected virtual void OnReOpened()
        {
        }
    }
}
