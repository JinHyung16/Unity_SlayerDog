using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Managers
{

    public partial class PoolManager : BaseBehaviour
    {
        public const string KeyEmpty = "Empty";

        private static readonly string[] ResourcesPathPrefixes =
        {
            "Prefabs/Skills/",
            "Prefabs/",
            "",
        };

        private static readonly Dictionary<string, GameObject> _resourcesCache =
            new Dictionary<string, GameObject>(16);

        private static readonly HashSet<string> _missingWarned = new HashSet<string>();

        public static PoolManager Instance { get; private set; }

        [Tooltip("풀에서 꺼낸 인스턴스의 부모. null이면 풀 미스 시 root에 스폰됨.")]
        public Transform PoolRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (PoolRoot == null)
                PoolRoot = transform;
        }

        public static PoolManager Ensure()
        {
            if (Instance != null)
                return Instance;
            GameObject go = new GameObject("_PoolManager");
            return go.AddComponent<PoolManager>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Clear();
                Instance = null;
            }
        }

        public void Clear()
        {
            Pool_Skill_Clear();
            Pool_Fx_Clear();
        }

        private GameObject InstantiateFromAddressable(string key)
        {
            GameObject prefab = ResolvePrefab(key);
            if (prefab == null)
                return null;
            return Instantiate(prefab, PoolRoot);
        }

        private GameObject ResolvePrefab(string key)
        {

            AddressableManager am = AddressableManager.Instance;
            GameObject prefab = am != null ? am.Get(key) : null;
            if (prefab != null)
                return prefab;

            if (_resourcesCache.TryGetValue(key, out prefab) && prefab != null)
                return prefab;

            for (int i = 0; i < ResourcesPathPrefixes.Length; i++)
            {
                prefab = Resources.Load<GameObject>(ResourcesPathPrefixes[i] + key);
                if (prefab != null)
                {
                    _resourcesCache[key] = prefab;
                    return prefab;
                }
            }

            if (_missingWarned.Add(key))
            {
                Debug.LogWarning($"[PoolManager] '{key}' prefab을 Addressables / Resources에서 찾을 수 없음 (이 키는 한 번만 경고)");
            }
            return null;
        }
    }
}
