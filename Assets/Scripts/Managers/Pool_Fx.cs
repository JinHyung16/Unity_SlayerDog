using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Managers
{

    public partial class PoolManager
    {
        private readonly Dictionary<string, Queue<GameObject>> _fxPool =
            new Dictionary<string, Queue<GameObject>>(8);

        public GameObject Pool_Fx_Get(string key)
        {
            if (string.IsNullOrEmpty(key) || key == KeyEmpty)
                return null;

            if (_fxPool.TryGetValue(key, out Queue<GameObject> queue) && queue.Count > 0)
            {
                GameObject pooled = queue.Dequeue();
                if (pooled != null)
                {
                    pooled.SetActive(true);
                    return pooled;
                }
            }

            GameObject prefab = AddressableManager.Instance != null
                ? AddressableManager.Instance.Get(key)
                : null;

            if (prefab == null)
            {
                Debug.LogWarning($"[PoolManager] FX 프리팹 '{key}' 가 로드되지 않았습니다");
                return null;
            }
            GameObject made = Instantiate(prefab);
            if (PoolRoot != null)
                made.transform.SetParent(PoolRoot, true);
            return made;
        }

        public void Pool_Fx_Prewarm(string key, int count)
        {
            if (string.IsNullOrEmpty(key) || key == KeyEmpty || count <= 0)
                return;

            GameObject prefab = AddressableManager.Instance != null
                ? AddressableManager.Instance.Get(key)
                : null;
            if (prefab == null)
            {
                Debug.LogWarning($"[PoolManager] FX 프리팹 '{key}' 가 없어 미리 만들지 못했습니다");
                return;
            }

            if (_fxPool.TryGetValue(key, out Queue<GameObject> queue) == false)
            {
                queue = new Queue<GameObject>(count);
                _fxPool[key] = queue;
            }

            for (int i = queue.Count; i < count; i++)
            {
                GameObject go = Instantiate(prefab);
                go.SetActive(false);
                if (PoolRoot != null)
                    go.transform.SetParent(PoolRoot, false);
                queue.Enqueue(go);
            }
        }

        public void Pool_Fx_Return(string key, GameObject obj)
        {
            if (obj == null)
                return;
            if (string.IsNullOrEmpty(key))
                key = KeyEmpty;

            obj.SetActive(false);
            if (PoolRoot != null)
                obj.transform.SetParent(PoolRoot, false);

            if (_fxPool.TryGetValue(key, out Queue<GameObject> queue) == false)
            {
                queue = new Queue<GameObject>();
                _fxPool[key] = queue;
            }
            queue.Enqueue(obj);
        }

        private void Pool_Fx_Clear()
        {
            foreach (Queue<GameObject> queue in _fxPool.Values)
            {
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj != null)
                        Destroy(obj);
                }
            }
            _fxPool.Clear();
        }
    }
}
