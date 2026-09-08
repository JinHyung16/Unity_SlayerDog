using UnityEngine;

namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// 조각(Component · Panel)의 로더 손잡이. 클래스가 <c>public static readonly PrefabAuto&lt;X&gt; PrefabAuto</c> 로 하나 들고 있는다.
    /// 여는 쪽은 경로를 모른다 — <see cref="UILoader"/> 가 풀에서 꺼내 주고 <see cref="UILoader.Restore"/> 로 돌려받는다.
    /// (레퍼런스 ProjectSample 의 PrefabAuto 와 같은 계약)
    /// </summary>
    public sealed class PrefabAuto<T> where T : BaseComponent
    {
        public readonly string Path;

        public PrefabAuto(string path)
        {
            Path = path;
        }

        public T Create(Transform parent)
        {
            return Create(parent, true);
        }

        public T Create(Transform parent, bool isActive)
        {
            T comp = UILoader.Get<T>(Path);
            if (comp == null)
            {
                return null;
            }

            if (parent != null)
            {
                Transform t = comp.transform;
                t.SetParent(parent, false);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }

            comp.gameObject.SetActive(isActive);
            return comp;
        }

        /// <summary>UI 조각 전용 — RectTransform 을 부모에 붙이고 앵커 위치를 0 으로 되돌린다.</summary>
        public T CreateForUI(Transform parent, bool isActive = true)
        {
            T comp = UILoader.Get<T>(Path);
            if (comp == null)
            {
                return null;
            }

            if (parent != null)
            {
                RectTransform rt = comp.RectTransform;
                rt.SetParent(parent, false);
                rt.anchoredPosition3D = Vector3.zero;
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
            }

            comp.gameObject.SetActive(isActive);
            return comp;
        }

        /// <summary>개수를 아는 목록은 미리 만든다. 첫 열림에서 Instantiate 가 터지면 프레임이 끊긴다.</summary>
        public void Preload(int count)
        {
            UILoader.Preload<T>(Path, count);
        }
    }
}
