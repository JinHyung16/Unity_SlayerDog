using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem.Skill;

namespace Jinhyeong_Managers
{
    public partial class PoolManager
    {
        private readonly Dictionary<string, Queue<GameObject>> _skillPool =
            new Dictionary<string, Queue<GameObject>>(16);

        public GameObject Pool_Skill_Get(string key = KeyEmpty)
        {
            if (string.IsNullOrEmpty(key))
                key = KeyEmpty;

            if (_skillPool.TryGetValue(key, out Queue<GameObject> queue) && queue.Count > 0)
            {
                GameObject pooled = queue.Dequeue();
                if (pooled != null)
                {
                    pooled.SetActive(true);
                    return pooled;
                }
            }

            return CreateSkillRoot(key);
        }

        /// <summary>
        /// 스킬 오브젝트 = Skill_Root 프리팹(로직 컴포넌트 고정) + 아트 자식(스크립트 0개).
        /// 풀이 아트 키별로 나뉘므로 한 번 붙인 아트는 재사용 내내 그대로다.
        /// </summary>
        private GameObject CreateSkillRoot(string artKey)
        {
            GameObject rootPrefab = AddressableManager.Instance != null
                ? AddressableManager.Instance.Get(CommonConfig.Skill.RootPrefabKey)
                : null;

            if (rootPrefab == null)
            {
                Debug.LogError($"[PoolManager] '{CommonConfig.Skill.RootPrefabKey}' 가 로드되지 않았습니다 — 스킬을 스폰할 수 없습니다");
                return null;
            }

            GameObject go = Instantiate(rootPrefab);
            go.name = $"Skill_{artKey}";

            SkillVisual visual = go.GetComponent<SkillVisual>();
            if (visual != null)
                visual.BindArt(artKey);

            return go;
        }

        public void Pool_Skill_Return(string key, GameObject obj)
        {
            if (obj == null)
                return;
            if (string.IsNullOrEmpty(key))
                key = KeyEmpty;

            obj.SetActive(false);
            if (PoolRoot != null)
                obj.transform.SetParent(PoolRoot, false);

            if (_skillPool.TryGetValue(key, out Queue<GameObject> queue) == false)
            {
                queue = new Queue<GameObject>();
                _skillPool[key] = queue;
            }
            queue.Enqueue(obj);
        }

        private void Pool_Skill_Clear()
        {
            foreach (Queue<GameObject> queue in _skillPool.Values)
            {
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj != null)
                        Destroy(obj);
                }
            }
            _skillPool.Clear();
        }
    }
}
