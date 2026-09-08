using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Managers;

namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// UI 조각의 풀. Get 으로 꺼내고 Restore 로 돌려준다 — UI 코드는 <c>Instantiate</c>/<c>Destroy</c> 를 직접 부르지 않는다.
    /// (레퍼런스 ProjectSample 의 Loader.Get / Loader.Restore 와 같은 계약)
    ///
    /// 풀은 **경로(주소) 단위**로 나뉜다. 종류가 다른 것을 한 풀에 담으면 리셋 규칙이 섞인다.
    /// 프리팹은 <see cref="AddressableManager"/> 에 미리 올라와 있어야 한다 — 창이 <c>PreloadPaths</c> 로 선언한다.
    /// </summary>
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
                    // 씬 리로드로 죽은 참조는 버린다. 풀은 static 이라 씬을 넘어 살지만 안의 인스턴스는 아니다.
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

        /// <summary>
        /// 풀로 돌려준다. 반납 시점에 조각이 자기 상태를 전부 리셋한다(<see cref="BaseComponent.Clear"/>) —
        /// listener · 구독 · 참조. 부모는 풀 루트로, 비활성으로.
        /// </summary>
        public static void Restore(BaseComponent comp)
        {
            if (comp == null)
            {
                return;
            }

            comp.Clear();
            comp.gameObject.SetActive(false);

            // 로더가 만든 것이 아니면 돌아갈 풀이 없다. 그냥 지운다.
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

        /// <summary>참조를 먼저 끊고 반납한다. 반납 뒤에 옛 참조를 쓰는 사고를 막는다.</summary>
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

            // 이미 들어 있는 만큼은 빼고 채운다 — 창을 다시 열어도 두 배가 되지 않는다.
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

        /// <summary>씬을 떠날 때 비운다. 정적 풀이 이전 씬의 인스턴스를 물고 있으면 누수다.</summary>
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
