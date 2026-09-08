using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_AI.BehaviorTree;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem;
using Jinhyeong_Collision;

namespace Jinhyeong_AI
{

    [RequireComponent(typeof(Damageable))]
    [RequireComponent(typeof(SkillObject))]
    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(CharacterFacing))]
    [RequireComponent(typeof(BehaviorTreeRunner))]
    public class EnemyAI : BaseBehaviour
    {
        private Damageable _self;
        private SkillObject _skills;
        private CharacterMotor _motor;
        private CharacterFacing _facing;
        private BehaviorTreeRunner _runner;
        private OBBCollider _ownBox;

        private Vector3 _spawnPosition;

        private Damageable _target;
        private bool _isAware;
        private float _attackTimer;
        private bool _isFleeing;
        private float _fleeEndTime;
        private float _fleeReengageUntil;
        private Vector3 _patrolTarget;
        private bool _hasPatrolTarget;
        private float _patrolWaitTimer;

        private const int PatrolAttempts = 8;

        private static readonly List<Damageable> _scanBuffer = new List<Damageable>(64);

        private void Awake()
        {
            _self   = GetComponent<Damageable>();
            _skills = GetComponent<SkillObject>();
            _motor  = GetComponent<CharacterMotor>();
            _facing = GetComponent<CharacterFacing>();
            _runner = GetComponent<BehaviorTreeRunner>();
            _ownBox = GetComponentInChildren<OBBCollider>();

            _spawnPosition = transform.position;

            _runner.SetRoot(BuildTree());
        }

        public void OnRespawned()
        {
            _target = null;
            _isAware = false;
            _attackTimer = 0f;
            _isFleeing = false;
            _fleeEndTime = 0f;
            _fleeReengageUntil = 0f;
            _hasPatrolTarget = false;
            _patrolWaitTimer = 0f;
            _spawnPosition = transform.position;
            if (_runner != null)
                _runner.SetRoot(BuildTree());
        }

        private BTNode BuildTree()
        {
            BTSelector root = new BTSelector { Name = "Root" };

            BTSequence flee = new BTSequence { Name = "Flee" };
            flee.Add(new BTCondition(ShouldFlee, "HP<=FleeThreshold"));
            flee.Add(new BTCondition(HasTarget, "TargetExists"));
            flee.Add(new BTAction(TickFlee, "FleeFromTarget"));

            BTSequence attack = new BTSequence { Name = "Attack" };
            attack.Add(new BTCondition(IsAware, "Aware"));
            attack.Add(new BTCondition(InAttackRange, "InAttackRange"));
            attack.Add(new BTAction(TickAttack, "FireAtTarget"));

            BTSequence chase = new BTSequence { Name = "Chase" };
            chase.Add(new BTCondition(IsAware, "Aware"));
            chase.Add(new BTAction(TickChase, "MoveToTarget"));

            BTAction patrol = new BTAction(TickPatrol, "Patrol");

            root.Add(flee);
            root.Add(attack);
            root.Add(chase);
            root.Add(patrol);
            return root;
        }

        private void RefreshAwareness()
        {
            _target = FindNearestFriend();
            if (_target == null)
            {
                _isAware = false;
                return;
            }

            float distSq = (_target.transform.position - transform.position).sqrMagnitude;
            float detSq = CommonConfig.Enemy.DetectionRange * CommonConfig.Enemy.DetectionRange;
            float loseSq = CommonConfig.Enemy.LoseSightRange * CommonConfig.Enemy.LoseSightRange;

            if (_isAware == false)
            {
                if (distSq <= detSq)
                    _isAware = true;
            }
            else
            {
                if (distSq > loseSq)
                    _isAware = false;
            }
        }

        private bool HasTarget()
        {
            RefreshAwareness();
            return _target != null;
        }

        private bool IsAware()
        {
            RefreshAwareness();
            return _isAware && _target != null;
        }

        private bool InAttackRange()
        {
            if (_target == null)
                return false;
            float distSq = (_target.transform.position - transform.position).sqrMagnitude;
            return distSq <= CommonConfig.Enemy.AIAttackRange * CommonConfig.Enemy.AIAttackRange;
        }

        private bool ShouldFlee()
        {
            bool lowHp = (_self.Hp / Mathf.Max(1f, _self.MaxHp)) <= CommonConfig.Enemy.FleeHpPercent;
            if (lowHp == false)
            {
                _isFleeing = false;
                return false;
            }

            if (_isFleeing)
                return true;

            if (Time.time < _fleeReengageUntil)
                return false;

            _isFleeing = true;
            _fleeEndTime = Time.time + CommonConfig.Enemy.FleeDuration;
            return true;
        }

        private EBTStatus TickFlee(float dt)
        {
            if (_target == null)
            {
                _isFleeing = false;
                return EBTStatus.Failure;
            }

            Vector3 toMe = transform.position - _target.transform.position;
            toMe.y = 0f;
            float dist = toMe.magnitude;

            bool timeUp = Time.time >= _fleeEndTime;
            bool safe = dist >= CommonConfig.Enemy.FleeSafeDistance;
            if (timeUp || safe)
            {
                _isFleeing = false;
                _fleeReengageUntil = Time.time + CommonConfig.Enemy.FleeReengageTime;
                _motor.MoveAxis = Vector2.zero;
                return EBTStatus.Failure;
            }

            Vector3 dir = dist < 0.0001f ? Random.insideUnitSphere : toMe / dist;
            dir.y = 0f;
            _motor.SpeedMultiplier = CommonConfig.Enemy.FleeSpeedMultiplier;
            _motor.MoveAxis = new Vector2(dir.x, dir.z);
            FaceDir(dir);
            return EBTStatus.Success;
        }

        private EBTStatus TickAttack(float dt)
        {
            if (_target == null)
                return EBTStatus.Failure;

            _motor.SpeedMultiplier = CommonConfig.Enemy.ChaseSpeedMultiplier;

            Vector3 toTarget = _target.transform.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist > 0.0001f && dist < CommonConfig.Enemy.StandoffDistance)
            {
                Vector3 away = -toTarget / dist;
                _motor.MoveAxis = new Vector2(away.x, away.z);
            }
            else
            {
                _motor.MoveAxis = Vector2.zero;
            }
            FaceDir(toTarget);

            _attackTimer -= dt;
            if (_attackTimer <= 0f)
            {
                _attackTimer = CommonConfig.Enemy.AIAttackInterval;
                Vector3 dir = AttackDirection();
                FireWeapon(dir);
            }
            return EBTStatus.Success;
        }

        private Vector3 AttackDirection()
        {
            if (_target == null)
                return _facing.ForwardPlanar;
            Vector3 to = _target.transform.position - transform.position;
            to.y = 0f;
            return to.sqrMagnitude > 0.0001f ? to.normalized : _facing.ForwardPlanar;
        }

        private void FireWeapon(Vector3 dir)
        {
            if (CommonConfig.Enemy.AttackSkillKey != KeyCode.None)
            {
                bool fired = _skills.TryFireSlot(CommonConfig.Enemy.AttackSkillKey);
                if (fired == false)
                {
                    TryFireMelee(dir);
                }
                return;
            }
            TryFireMelee(dir);
        }

        private void TryFireMelee(Vector3 dir)
        {
            CharacterAttack melee = GetComponent<CharacterAttack>();
            if (melee != null)
                melee.TryFire(dir);
        }

        private EBTStatus TickChase(float dt)
        {
            if (_target == null)
                return EBTStatus.Failure;

            Vector3 to = _target.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f)
            {
                _motor.MoveAxis = Vector2.zero;
                return EBTStatus.Success;
            }
            Vector3 dir = to.normalized;

            _motor.SpeedMultiplier = CommonConfig.Enemy.ChaseSpeedMultiplier;
            _motor.MoveAxis = new Vector2(dir.x, dir.z);
            FaceDir(_target.transform.position - transform.position);
            return EBTStatus.Success;
        }

        private EBTStatus TickPatrol(float dt)
        {
            if (_patrolWaitTimer > 0f)
            {
                _patrolWaitTimer -= dt;
                _motor.MoveAxis = Vector2.zero;
                return EBTStatus.Success;
            }

            if (_hasPatrolTarget == false)
            {
                if (PickNewPatrolTarget() == false)
                {
                    _patrolWaitTimer = CommonConfig.Enemy.PatrolWaitTime;
                    _motor.MoveAxis = Vector2.zero;
                    return EBTStatus.Success;
                }
            }

            Vector3 to = _patrolTarget - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude <= CommonConfig.Enemy.PatrolArrivalDistance * CommonConfig.Enemy.PatrolArrivalDistance)
            {
                _hasPatrolTarget = false;
                _patrolWaitTimer = CommonConfig.Enemy.PatrolWaitTime;
                _motor.MoveAxis = Vector2.zero;
                return EBTStatus.Success;
            }

            if ((_motor.LastCollisionFlags & CollisionFlags.Sides) != 0)
            {
                _hasPatrolTarget = false;
                _motor.MoveAxis = Vector2.zero;
                return EBTStatus.Success;
            }

            Vector3 dir = to.normalized;
            _motor.SpeedMultiplier = CommonConfig.Enemy.ChaseSpeedMultiplier;
            _motor.MoveAxis = new Vector2(dir.x, dir.z);
            FaceDir(dir);
            return EBTStatus.Success;
        }

        private void FaceDir(Vector3 worldDir)
        {
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude < 0.0001f)
                return;
            _facing.ForwardWorld = worldDir;
        }

        private bool PickNewPatrolTarget()
        {
            for (int attempt = 0; attempt < PatrolAttempts; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * CommonConfig.Enemy.PatrolRadius;
                Vector3 candidate = _spawnPosition + new Vector3(offset.x, 0f, offset.y);

                Vector3 to = candidate - transform.position;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist < 0.2f)
                    continue;

                if (OBBPhysics.SegmentBlockedXZ(transform.position, candidate, 0.5f, _ownBox))
                    continue;

                _patrolTarget = candidate;
                _hasPatrolTarget = true;
                return true;
            }
            _hasPatrolTarget = false;
            return false;
        }

        private Damageable FindNearestFriend()
        {
            ESkillTeam enemyTeam = SkillTeamUtil.Opposite(_self.Team);
            Damageable.GetAllOfTeam(enemyTeam, _scanBuffer);

            Damageable best = null;
            float bestSq = float.MaxValue;
            for (int i = 0; i < _scanBuffer.Count; i++)
            {
                Damageable d = _scanBuffer[i];
                if (d == null || d == _self)
                    continue;
                float sq = (d.transform.position - transform.position).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, CommonConfig.Enemy.DetectionRange);
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, CommonConfig.Enemy.LoseSightRange);
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, CommonConfig.Enemy.AIAttackRange);

            if (Application.isPlaying)
            {
                Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.6f);
                Gizmos.DrawWireSphere(_spawnPosition, CommonConfig.Enemy.PatrolRadius);
                if (_hasPatrolTarget)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(_patrolTarget, 0.2f);
                }
            }
        }
    }
}
