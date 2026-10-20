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
        private int _merged;
        private Vector3 _baseScale = Vector3.zero;

        // 쉬는(아직 안 끌려온) 뼈 목록. 이 수를 넘으면 새로 떨어지는 경험치는 반경 안 가장 가까운 뼈에 합친다
        private const int MergeAbove = 50;
        private const float MergeRadius = 3f;
        private const int MaxMergeSteps = 6;
        private static readonly System.Collections.Generic.List<XpGem> _resting = new System.Collections.Generic.List<XpGem>(128);

        public static void Drop(Vector3 position, float xp)
        {
            if (PoolManager.Instance == null)
                return;

            if (_resting.Count >= MergeAbove && TryMerge(position, xp))
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
            gem._merged = 0;
            if (gem._baseScale == Vector3.zero)
                gem._baseScale = gem.transform.localScale;
            gem.transform.localScale = gem._baseScale;
            if (_resting.Contains(gem) == false)
                _resting.Add(gem);
        }

        private static bool TryMerge(Vector3 position, float xp)
        {
            XpGem best = null;
            float bestSq = MergeRadius * MergeRadius;
            for (int i = 0; i < _resting.Count; i++)
            {
                XpGem g = _resting[i];
                if (g == null)
                    continue;
                Vector3 d = g.transform.position - position;
                float sq = d.x * d.x + d.z * d.z;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = g;
                }
            }
            if (best == null)
                return false;
            best._xp += xp;
            best._merged++;
            // 합칠수록 조금씩 크게 (최대 1.6배) — 큰 뼈 = 값진 뼈로 읽히게
            float k = 1f + 0.1f * Mathf.Min(best._merged, MaxMergeSteps);
            best.transform.localScale = best._baseScale * k;
            return true;
        }

        protected override void OnDisabled()
        {
            _resting.Remove(this);
        }

        // 자석 아이템: 쉬는 뼈를 전부 플레이어에게 날린다
        public static void PullAll()
        {
            for (int i = _resting.Count - 1; i >= 0; i--)
            {
                XpGem g = _resting[i];
                if (g == null)
                    continue;
                g._flying = true;
                g._flySpeed = 6f;
            }
            _resting.Clear();
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
                {
                    _flying = true;
                    _resting.Remove(this);
                }
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
