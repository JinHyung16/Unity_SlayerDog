using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Managers;

namespace Jinhyeong_UI.Framework
{

    public static class UILoader
    {
        private static readonly Dictionary<string, Stack<BaseComponent>> _pools =
            new Dictionary<string, Stack<BaseComponent>>(16);

        private static readonly HashSet<string> _missingWarned = new HashSet<string>();

        public static T Get<T>(string path) where T : BaseComponent
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (_pools.TryGetValue(path, out Stack<BaseComponent> pool))
            {
                while (pool.Count > 0)
                {
                    BaseComponent pooled = pool.Pop();
                    if (pooled == null)
                    {
                        continue;
                    }
                    T typed = pooled as T;
                    if (typed == null)
                    {
                        Debug.LogError($"[UILoader] '{path}' 풀에 {pooled.GetType().Name} 이 들어 있다 — {typeof(T).Name} 을 기대했다");
                        Object.Destroy(pooled.gameObject);
                        continue;
                    }
                    return typed;
                }
            }

            return Make<T>(path);
        }

        private static T Make<T>(string path) where T : BaseComponent
        {
            GameObject prefab = AddressableManager.Instance != null ? AddressableManager.Instance.Get(path) : null;
            if (prefab == null)
            {
                if (_missingWarned.Add(path))
                {
                    Debug.LogError($"[UILoader] '{path}' 프리팹이 로드되어 있지 않다 — 창의 PreloadPaths 에 넣었는가 (이 경로는 한 번만 알린다)");
                }
                return null;
            }

            GameObject go = Object.Instantiate(prefab);
            T comp = go.GetComponent<T>();
            if (comp == null)
            {
                Debug.LogError($"[UILoader] '{path}' 프리팹 루트에 {typeof(T).Name} 이 없다");
                Object.Destroy(go);
                return null;
            }
            comp.SetPath(path);
            return comp;
        }

        public static void Restore(BaseComponent comp)
        {
            if (comp == null)
            {
                return;
            }

            comp.Clear();
            comp.gameObject.SetActive(false);

            if (string.IsNullOrEmpty(comp.Path))
            {
                Object.Destroy(comp.gameObject);
                return;
            }

            Transform root = UIManagement.Instance != null ? UIManagement.Instance.PoolRoot : null;
            comp.RectTransform.SetParent(root, false);

            if (_pools.TryGetValue(comp.Path, out Stack<BaseComponent> pool) == false)
            {
                pool = new Stack<BaseComponent>(8);
                _pools[comp.Path] = pool;
            }
            pool.Push(comp);
        }

        public static void RestoreSafe<T>(ref T comp) where T : BaseComponent
        {
            T local = comp;
            comp = null;
            Restore(local);
        }

        public static void Preload<T>(string path, int count) where T : BaseComponent
        {
            if (count <= 0)
            {
                return;
            }

            if (_pools.TryGetValue(path, out Stack<BaseComponent> pool) == false)
            {
                pool = new Stack<BaseComponent>(count);
                _pools[path] = pool;
            }

            Transform root = UIManagement.Instance != null ? UIManagement.Instance.PoolRoot : null;

            for (int i = pool.Count; i < count; i++)
            {
                T comp = Make<T>(path);
                if (comp == null)
                {
                    return;
                }
                comp.gameObject.SetActive(false);
                comp.RectTransform.SetParent(root, false);
                pool.Push(comp);
            }
        }

        public static void ClearAll()
        {
            foreach (Stack<BaseComponent> pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    BaseComponent comp = pool.Pop();
                    if (comp != null)
                    {
                        Object.Destroy(comp.gameObject);
                    }
                }
            }
            _pools.Clear();
        }
    }
}
