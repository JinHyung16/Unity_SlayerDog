using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Input;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Character
{

    public class Player : BaseBehaviour
    {
        [Header("Bound Components")]
        [SerializeField] private Damageable _damageable;
        [SerializeField] private SkillObject _skills;
        [SerializeField] private Playable _playable;
        [SerializeField] private KeyboardInputProvider _input;
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private CharacterFacing _facing;
        [SerializeField] private CharacterAttack _attack;
        [SerializeField] private PlayerController _controller;

        public Damageable Damageable { get { return _damageable; } }
        public SkillObject Skills { get { return _skills; } }

        private bool _spawnedNotified;

        private void Awake()
        {

            if (RequireRef(_damageable, nameof(_damageable)) == false)
                return;
            if (RequireRef(_skills, nameof(_skills)) == false)
                return;
            if (RequireRef(_playable, nameof(_playable)) == false)
                return;
            if (RequireRef(_input, nameof(_input)) == false)
                return;
            if (RequireRef(_motor, nameof(_motor)) == false)
                return;
            if (RequireRef(_facing, nameof(_facing)) == false)
                return;
            if (RequireRef(_attack, nameof(_attack)) == false)
                return;
            if (RequireRef(_controller, nameof(_controller)) == false)
                return;

            Init();
        }

        protected override void OnEnabled()
        {
            if (_spawnedNotified)
                return;
            _spawnedNotified = true;
            GameEvents.RaisePlayerSpawned(this);
        }

        protected override void OnDisabled()
        {
            if (_spawnedNotified == false)
                return;
            _spawnedNotified = false;
            GameEvents.RaisePlayerDespawned(this);
        }

        public void Init()
        {
            // 피격 무적은 플레이어에게만 준다. 적에게 주면 광역 스킬이 통째로 무력해진다.
            if (_damageable != null)
                _damageable.InvulnerabilitySeconds = CommonConfig.Player.HitInvulnerability;

            // 상점에서 산 영구 강화. [주의] 스폰 경로가 둘(WorldSpawner · 하네스)이라
            // 스폰하는 쪽에 얹으면 한쪽만 적용된다. 기본값을 세우는 여기가 유일한 길목이다.
            float metaHp = CommonConfig.Player.Hp * SaveData.StatMultiplier(SaveData.EStat.Hp);
            _damageable.MaxHp = metaHp;
            _damageable.Hp = metaHp;
            _damageable.Shield = 0f;
            _damageable.Stunned = false;
            _damageable.SpeedMultiplier = 1f;
            _damageable.IncomingDamageMultiplier = 1f;
            _damageable.NotifyHealthChanged();

            _skills.Stunned = false;
            _skills.OutgoingDamageMultiplier = 1f;
            _skills.AttackSpeedMultiplier = 1f;
            _skills.AttackPower = CommonConfig.Player.AttackPower * SaveData.StatMultiplier(SaveData.EStat.Attack);

            _motor.MoveSpeed = CommonConfig.Player.MoveSpeed * SaveData.StatMultiplier(SaveData.EStat.Speed);
            _motor.SpeedMultiplier = 1f;
            _motor.MoveAxis = Vector2.zero;

            _facing.TurnSpeed = CommonConfig.Player.TurnSpeed;

            _attack.Damage = CommonConfig.Player.AttackDamage;
            _attack.Range = CommonConfig.Player.AttackRange;
            _attack.HalfAngleDeg = CommonConfig.Player.AttackHalfAngleDeg;
            _attack.Cooldown = CommonConfig.Player.AttackCooldown;
        }
    }
}
