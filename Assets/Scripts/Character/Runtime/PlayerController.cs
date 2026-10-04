using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Input;
using Jinhyeong_SkillSystem;
using Jinhyeong_Common;

namespace Jinhyeong_Character
{

    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(CharacterFacing))]
    [RequireComponent(typeof(CharacterAttack))]
    [RequireComponent(typeof(SkillObject))]
    public class PlayerController : BaseBehaviour
    {
        public MonoBehaviour InputSource;
        public InputBindings Bindings;

        private IInputProvider _input;
        private CharacterMotor _motor;
        private CharacterFacing _facing;
        private CharacterAttack _attack;
        private SkillObject _skills;

        private static readonly List<Damageable> _enemyScan = new List<Damageable>(64);

        private void Awake()
        {

            _motor = GetComponent<CharacterMotor>();
            _facing = GetComponent<CharacterFacing>();
            _attack = GetComponent<CharacterAttack>();
            _skills = GetComponent<SkillObject>();

            if (RequireRef(InputSource, nameof(InputSource)) == false)
                return;
            if (RequireRef(Bindings, nameof(Bindings)) == false)
                return;

            _input = InputSource as IInputProvider;
            if (_input == null)
            {
                Debug.LogError($"[PlayerController] '{name}'의 InputSource({InputSource.GetType().Name})가 IInputProvider를 구현하지 않음", this);
                enabled = false;
            }
        }

        private AutoAim _autoAim;

        private void Start()
        {
            _autoAim = GetComponent<AutoAim>();
        }

        private void Update()
        {
            Vector2 axis = _input.MoveAxis;
            Vector3 worldMove = ComputeCameraRelativeMove(axis);
            _motor.MoveAxis = new Vector2(worldMove.x, worldMove.z);

            if (worldMove.sqrMagnitude > 0.0001f && _autoAim == null)
            {
                _facing.ForwardWorld = worldMove;
            }

            if (_input.GetAttackDown())
            {
                _attack.TryFire(_facing.ForwardPlanar);
            }
            else
            {
                AutoAttackNearestEnemy();
            }

            for (int i = 0; i < Bindings.SkillSlots.Count; i++)
            {
                KeyCode slotKey = Bindings.SkillSlots[i].Key;
                if (slotKey == KeyCode.None)
                    continue;
                if (_input.GetSkillSlotDown(slotKey))
                {
                    _skills.TryFireSlot(slotKey);
                }
            }
        }

        private void AutoAttackNearestEnemy()
        {
            Damageable target = FindNearestEnemyInRange(CommonConfig.Player.AutoAttackRange);
            if (target == null)
                return;

            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;
            Vector3 dir = to.sqrMagnitude > 0.0001f ? to.normalized : _facing.ForwardPlanar;
            _attack.TryFire(dir);
        }

        private Damageable FindNearestEnemyInRange(float range)
        {
            ESkillTeam enemyTeam = SkillTeamUtil.Opposite(_skills != null ? _skills.Team : ESkillTeam.Friend);
            Damageable.GetAllOfTeam(enemyTeam, _enemyScan);

            float maxSq = range * range;
            Damageable best = null;
            float bestSq = float.MaxValue;
            for (int i = 0; i < _enemyScan.Count; i++)
            {
                Damageable d = _enemyScan[i];
                Vector3 to = d.transform.position - transform.position;
                to.y = 0f;
                float sq = to.sqrMagnitude;
                if (sq <= maxSq && sq < bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        private Vector3 ComputeCameraRelativeMove(Vector2 axis)
        {
            if (axis.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            float yaw = 0f;
            CameraFollow cam = CameraFollow.Active;
            if (cam != null)
                yaw = cam.Yaw;

            Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 local = new Vector3(axis.x, 0f, axis.y);
            return yawRot * local;
        }
    }
}
