using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jinhyeong_UI.Framework
{

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

        public virtual IReadOnlyList<string> PreloadPaths { get { return Array.Empty<string>(); } }

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

        protected virtual void Opening()
        {
        }

        protected virtual void BeforeClosed()
        {
        }

        protected virtual void OnClose()
        {
        }

        protected virtual bool HandleCanClose()
        {
            return true;
        }

        protected virtual void OnOtherWindowOpened()
        {
        }

        protected virtual void OnReOpened()
        {
        }
    }
}
