using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// UI 조각의 뿌리. Window · Panel · Component 가 전부 여기서 나온다.
    ///
    /// 엔진 훅(Awake/OnEnable)에 로직을 두지 않는다 — 풀에서 재사용되면 두 번째부터 안 돌고,
    /// 순서를 아무도 보장하지 않는다. 필요할 때 <c>Open()</c>, 닫을 때 <c>Close()</c>/<see cref="Clear"/> 로
    /// 값과 참조를 초기화한다.
    /// </summary>
    public abstract class BaseComponent : BaseBehaviour
    {
        /// <summary>풀 반납 키. <see cref="UILoader"/> 가 꺼낼 때 심는다. 비어 있으면 로더가 만든 것이 아니다.</summary>
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

        /// <summary>
        /// 반납 시점의 리셋. listener · 구독 · 데이터 참조를 전부 끊는다.
        /// <c>Open()</c> 의 첫 줄에서도 부른다 — 재사용 조각이 이전 값을 물고 있으면 안 된다.
        /// </summary>
        public virtual void Clear()
        {
        }
    }
}
