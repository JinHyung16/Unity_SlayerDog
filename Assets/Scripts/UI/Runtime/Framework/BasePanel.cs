using UnityEngine;

namespace Jinhyeong_UI.Framework
{

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

        protected virtual void Opening()
        {
        }

        protected virtual void OnClose()
        {
        }

        protected virtual void OnReShown()
        {
        }
    }
}
