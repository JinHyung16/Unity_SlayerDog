using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Character
{
    [RequireComponent(typeof(CharacterFacing))]
    public class AutoAim : BaseBehaviour
    {
        [SerializeField] private float _range = 14f;

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
