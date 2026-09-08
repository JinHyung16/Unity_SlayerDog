using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Character
{
    /// <summary>
    /// 자동 조준 — 스킬이 나가는 방향을 가장 가까운 적 쪽으로 돌린다.
    ///
    /// [사고] 이게 없을 때 정면은 **걷는 방향**뿐이었다. 탑다운 뱀서류에서는 적에게서
    /// 도망치며 싸우는 시간이 대부분이라, 부채꼴·투사체 스킬이 매번 등 뒤로 나갔다.
    /// 스킬이 조준되지 않으면 어떤 수치를 넣어도 화력이 안 나온다.
    ///
    /// 루트 회전만 돌린다. 스프라이트는 빌보드라 이 회전에 영향받지 않으므로,
    /// 「보는 방향(그림)」과 「쏘는 방향(판정)」을 따로 굴릴 수 있다.
    /// </summary>
    [RequireComponent(typeof(CharacterFacing))]
    public class AutoAim : BaseBehaviour
    {
        /// <summary>이 거리 밖의 적은 조준하지 않는다. 화면 밖을 겨누면 방향이 계속 튄다.</summary>
        [SerializeField] private float _range = 14f;

        /// <summary>조준 대상을 다시 고르는 주기(초). 매 프레임 고르면 적이 겹칠 때 방향이 떤다.</summary>
        [SerializeField] private float _retargetInterval = 0.2f;

        [SerializeField] private CharacterFacing _facing;
        [SerializeField] private CharacterMotor _motor;

        private Damageable _target;
        private float _nextRetarget;

        private void Awake()
        {
            if (_facing == null)
                _facing = GetComponent<CharacterFacing>();
            if (_motor == null)
                _motor = GetComponent<CharacterMotor>();
        }

        protected override void OnEnabled()
        {
            _target = null;
            _nextRetarget = 0f;
        }

        private void Update()
        {
            if (_facing == null)
                return;

            if (Time.time >= _nextRetarget || IsValid(_target) == false)
            {
                _nextRetarget = Time.time + Mathf.Max(0.05f, _retargetInterval);
                _target = FindNearest();
            }

            if (IsValid(_target))
            {
                Vector3 to = _target.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
                {
                    _facing.ForwardWorld = to;
                    return;
                }
            }

            // 적이 없으면 걷는 방향을 본다. 가만히 서 있으면 마지막 방향을 유지한다.
            if (_motor == null)
                return;
            Vector2 axis = _motor.MoveAxis;
            if (axis.sqrMagnitude > 0.01f)
                _facing.ForwardWorld = new Vector3(axis.x, 0f, axis.y);
        }

        private bool IsValid(Damageable d)
        {
            if (d == null || d.IsAlive == false || d.isActiveAndEnabled == false)
                return false;
            return (d.transform.position - transform.position).sqrMagnitude <= _range * _range;
        }

        private Damageable FindNearest()
        {
            System.Collections.Generic.IReadOnlyList<Damageable> all = Damageable.All;
            Damageable best = null;
            float bestSq = _range * _range;
            Vector3 me = transform.position;

            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (d == null || d.IsAlive == false || d.isActiveAndEnabled == false)
                    continue;
                if (d.Team != ESkillTeam.Enemy)
                    continue;

                float sq = (d.transform.position - me).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }
    }
}
