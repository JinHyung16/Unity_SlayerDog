using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem
{

    public class XpGem : BaseBehaviour
    {
        private const string PoolKey = CommonConfig.Skill.XpGemKey;

        private float _xp;
        private bool _flying;
        private float _flySpeed;
        private float _spawnTime;

        public static void Drop(Vector3 position, float xp)
        {
            if (PoolManager.Instance == null)
                return;

            GameObject go = PoolManager.Instance.Pool_Fx_Get(PoolKey);
            if (go == null)
                return;

            go.transform.position = new Vector3(position.x, 0.4f, position.z);

            XpGem gem = go.GetComponent<XpGem>();
            if (gem == null)
            {
                Debug.LogWarning($"[XpGem] '{PoolKey}' 프리팹에 XpGem 컴포넌트가 없습니다");
                return;
            }
            gem._xp = xp;
            gem._flying = false;
            gem._flySpeed = 0f;
            gem._spawnTime = Time.time;
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
                pos.y = 0.4f + Mathf.Sin((Time.time - _spawnTime) * 4f) * 0.08f;
                transform.position = pos;

                float magnet = CommonConfig.Run.MagnetRadius;
                if (distSq <= magnet * magnet)
                    _flying = true;
                return;
            }

            _flySpeed += CommonConfig.Run.MagnetAccel * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, _flySpeed * Time.deltaTime);

            if ((target - transform.position).sqrMagnitude <= CommonConfig.Run.PickupDistance * CommonConfig.Run.PickupDistance)
            {
                if (RunProgress.Instance != null)
                    RunProgress.Instance.AddXp(_xp);
                    Jinhyeong_Managers.SfxManager.Play(Jinhyeong_Managers.ESfx.Pickup, 0.35f);

                if (PoolManager.Instance != null)
                    PoolManager.Instance.Pool_Fx_Return(PoolKey, gameObject);
                else
                    Destroy(gameObject);
            }
        }
    }
}
