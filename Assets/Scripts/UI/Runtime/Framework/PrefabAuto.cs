using UnityEngine;

namespace Jinhyeong_UI.Framework
{

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

        public void Preload(int count)
        {
            UILoader.Preload<T>(Path, count);
        }
    }
}
