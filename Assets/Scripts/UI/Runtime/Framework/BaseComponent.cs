using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_UI.Framework
{

    public abstract class BaseComponent : BaseBehaviour
    {
        public string Path { get; private set; }

        private RectTransform _rectTransform;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = transform as RectTransform;
                }
                return _rectTransform;
            }
        }

        internal void SetPath(string path)
        {
            Path = path;
        }

        public virtual void Clear()
        {
        }
    }
}
