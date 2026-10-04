using System;
using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_SkillSystem;
using Jinhyeong_Common;

namespace Jinhyeong_Character
{

    [RequireComponent(typeof(SkillObject))]
    public class CharacterAttack : BaseBehaviour
    {
        [HideInInspector] public float Damage;
        [HideInInspector] public float Range;
        [HideInInspector] public float HalfAngleDeg;
        [HideInInspector] public float Cooldown;

        public event Action OnFired;

        private SkillObject _caster;
        private float _nextReadyTime;

        private void Awake()
        {
            _caster = GetComponent<SkillObject>();
        }

        public bool TryFire(Vector3 forwardPlanar)
        {
            if (_caster != null && _caster.Stunned)
                return false;
            if (Time.time < _nextReadyTime)
                return false;
            _nextReadyTime = Time.time + Cooldown;

            if (OnFired != null)
                OnFired.Invoke();

            if (_caster != null)
                _caster.NotifyAttack();

            Damageable target = FindFrontTarget(forwardPlanar);
            if (target == null)
                return true;

            float scaled = Damage * (_caster != null ? _caster.OutgoingDamageMultiplier : 1f);
            target.TakeDamage(scaled, _caster);
            return true;
        }

        private Damageable FindFrontTarget(Vector3 forwardPlanar)
        {
            ESkillTeam enemyTeam = SkillTeamUtil.Opposite(_caster != null ? _caster.Team : ESkillTeam.Friend);
            float rangeSq = Range * Range;
            float cosHalf = Mathf.Cos(HalfAngleDeg * Mathf.Deg2Rad);

            Damageable best = null;
            float bestSq = float.MaxValue;

            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (d == null || d.IsAlive == false)
                    continue;
                if (d.Team != enemyTeam)
                    continue;

                Vector3 to = d.transform.position - transform.position;
                to.y = 0f;
                float sq = to.sqrMagnitude;
                if (sq > rangeSq || sq < 0.0001f)
                    continue;

                Vector3 dir = to / Mathf.Sqrt(sq);
                if (Vector3.Dot(forwardPlanar, dir) < cosHalf)
                    continue;

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
