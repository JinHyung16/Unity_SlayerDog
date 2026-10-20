using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem
{
    // 필드 아이템 — 회복 고기 · 자석. 적이 죽을 때 드물게 떨어진다 (EnemySpawner)
    // 상자처럼 찾아가서 줍는다 (가까이 오면 그때 끌려온다)
    public class FieldPickup : BaseBehaviour
    {
        public enum EKind
        {
            Meat,
            Magnet
        }

        [SerializeField] private EKind _kind;

        private string _poolKey;
        private bool _flying;
        private float _flySpeed;
        private float _spawnTime;

        public static void Drop(string key, Vector3 position)
        {
            if (PoolManager.Instance == null)
                return;
            GameObject go = PoolManager.Instance.Pool_Fx_Get(key);
            if (go == null)
                return;
            go.transform.position = new Vector3(position.x, 0.4f, position.z);
            FieldPickup p = go.GetComponent<FieldPickup>();
            if (p == null)
            {
                Debug.LogWarning($"[FieldPickup] '{key}' 프리팹에 FieldPickup 이 없습니다");
                return;
            }
            p._poolKey = key;
            p._flying = false;
            p._flySpeed = 0f;
            p._spawnTime = Time.time;
        }

        private void Update()
        {
            Jinhyeong_Character.Player player = GameEvents.CurrentPlayer;
            if (player == null)
                return;

            Vector3 target = player.transform.position + Vector3.up * 0.6f;
            Vector3 to = target - transform.position;
            float distSq = to.x * to.x + to.z * to.z;

            if (_flying == false)
            {
                Vector3 pos = transform.position;
                pos.y = 0.4f + Mathf.Sin((Time.time - _spawnTime) * 3f) * 0.1f;
                transform.position = pos;
                float r = CommonConfig.Run.FieldPickupRadius;
                if (distSq <= r * r)
                    _flying = true;
                return;
            }

            _flySpeed += CommonConfig.Run.MagnetAccel * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, _flySpeed * Time.deltaTime);
            if ((target - transform.position).sqrMagnitude > CommonConfig.Run.PickupDistance * CommonConfig.Run.PickupDistance)
                return;

            Apply(player);
            SfxManager.Play(ESfx.Pickup, 0.8f);
            if (PoolManager.Instance != null)
                PoolManager.Instance.Pool_Fx_Return(_poolKey, gameObject);
            else
                Destroy(gameObject);
        }

        private void Apply(Jinhyeong_Character.Player player)
        {
            switch (_kind)
            {
                case EKind.Meat:
                    Damageable hp = player.GetComponent<Damageable>();
                    if (hp != null)
                        hp.Heal(hp.MaxHp * CommonConfig.Run.MeatHealRatio);
                    HitEffect.Spawn(player.transform.position + Vector3.up, new Color(0.5f, 1f, 0.55f, 1f), 0.4f, 2f);
                    break;
                case EKind.Magnet:
                    XpGem.PullAll();
                    HitEffect.Spawn(player.transform.position + Vector3.up, new Color(1f, 0.85f, 0.35f, 1f), 0.5f, 3f);
                    break;
            }
        }
    }
}
