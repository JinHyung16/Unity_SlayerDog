using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Managers
{

    /// <summary>
    /// 짧게 터지고 사라지는 연출용 풀 (피격 플래시, 연쇄 번개 라인 등).
    ///
    /// 스킬 본체(Pool_Skill)와 달리 아트+로직이 한 프리팹에 들어 있어서 조립이 없다.
    /// 프리팹에 컴포넌트가 이미 박혀 있으므로 런타임 AddComponent 가 없다.
    /// </summary>
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

        /// <summary>
        /// 미리 만들어 둔다. 적(Prewarm 200)과 달리 FX 풀은 빈 채로 시작해서,
        /// 전투가 붙는 순간 피격 이펙트·데미지 텍스트·경험치 젬이 **동시에** Instantiate 된다.
        /// 물량이 쏟아지는 장르라 그 첫 폭발이 그대로 프레임 끊김으로 보인다.
        ///
        /// 이미 들어 있는 만큼은 빼고 채운다 — 씬을 다시 시작해도 두 배가 되지 않는다.
        /// </summary>
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
