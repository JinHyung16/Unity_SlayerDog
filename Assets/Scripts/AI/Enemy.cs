using System;
using UnityEngine;
using Jinhyeong_AI.BehaviorTree;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_AI
{

    public class Enemy : BaseBehaviour
    {
        [Header("Bound Components")]
        [SerializeField] private Damageable _damageable;
        [SerializeField] private SkillObject _skills;
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private CharacterFacing _facing;
        [SerializeField] private CharacterAttack _attack;
        [SerializeField] private BehaviorTreeRunner _btRunner;
        [SerializeField] private EnemyAI _ai;
        [SerializeField] private Jinhyeong_View.SpriteSetLibrary _spriteSets;

        public Damageable Damageable { get { return _damageable; } }
        public SkillObject Skills { get { return _skills; } }
        public CharacterMotor Motor { get { return _motor; } }
        public CharacterFacing Facing { get { return _facing; } }
        public CharacterAttack Attack { get { return _attack; } }
        public BehaviorTreeRunner BTRunner { get { return _btRunner; } }
        public EnemyAI AI { get { return _ai; } }

        public event Action<Enemy> OnDespawnRequested;

        public Jinhyeong_SkillSystem.EnemyTypeData Type { get; private set; }

        public float XpValue { get; private set; }

        public bool IsElite { get; private set; }

        private Renderer[] _tintRenderers;
        private MaterialPropertyBlock _tintMpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private Vector3 _baseScale = Vector3.one;
        private bool _baseScaleCaptured;

        private bool _subscribed;
        private bool _bindingValid;

        private void Awake()
        {

            _bindingValid =
                RequireRef(_damageable, nameof(_damageable)) &&
                RequireRef(_skills, nameof(_skills)) &&
                RequireRef(_motor, nameof(_motor)) &&
                RequireRef(_facing, nameof(_facing)) &&
                RequireRef(_attack, nameof(_attack)) &&
                RequireRef(_btRunner, nameof(_btRunner)) &&
                RequireRef(_ai, nameof(_ai));
            if (_bindingValid == false)
                return;

            Init();
        }

        protected override void OnDisabled()
        {
            UnsubscribeDeath();
        }

        public void Init(Vector3 position)
        {
            transform.position = position;
            transform.rotation = Quaternion.identity;
            Init();
        }

        public void Init()
        {
            if (_bindingValid == false)
                return;

            _damageable.MaxHp = CommonConfig.Enemy.Hp;
            _damageable.Hp = CommonConfig.Enemy.Hp;
            _damageable.Shield = 0f;
            _damageable.Stunned = false;
            _damageable.SpeedMultiplier = 1f;
            IsBoss = false;
            ShowHpBar(false);
            _damageable.IncomingDamageMultiplier = 1f;
            _damageable.DestroyOnDeath = false;
            _damageable.NotifyHealthChanged();
            SubscribeDeath();

            _skills.Stunned = false;
            _skills.OutgoingDamageMultiplier = 1f;
            _skills.AttackSpeedMultiplier = 1f;

            _motor.MoveSpeed = CommonConfig.Enemy.MoveSpeed;
            _motor.MoveAxis = Vector2.zero;
            _motor.SpeedMultiplier = 1f;

            _facing.TurnSpeed = CommonConfig.Enemy.TurnSpeed;

            _attack.Damage = CommonConfig.Enemy.AttackDamage;
            _attack.Range = CommonConfig.Enemy.AttackRange;
            _attack.HalfAngleDeg = CommonConfig.Enemy.AttackHalfAngleDeg;
            _attack.Cooldown = CommonConfig.Enemy.AttackCooldown;

            _ai.OnRespawned();
        }

        public void ApplyType(Jinhyeong_SkillSystem.EnemyTypeData type)
        {
            Type = type;

            if (_baseScaleCaptured == false)
            {
                _baseScale = transform.localScale;
                _baseScaleCaptured = true;
            }

            IsElite = false;
            XpValue = CommonConfig.Run.XpPerGem;

            if (type == null)
            {
                if (_damageable != null)
                {
                    _damageable.MaxHp = CommonConfig.Enemy.Hp;
                    _damageable.Hp = _damageable.MaxHp;
                    _damageable.NotifyHealthChanged();
                }
                if (_motor != null)
                    _motor.MoveSpeed = CommonConfig.Enemy.MoveSpeed;
                if (_attack != null)
                    _attack.Damage = CommonConfig.Enemy.AttackDamage;

                transform.localScale = _baseScale;
                ApplyTint(Color.white, false);
                return;
            }

            if (_spriteSets == null)
                _spriteSets = GetComponentInChildren<Jinhyeong_View.SpriteSetLibrary>(true);
            if (_spriteSets != null)
                _spriteSets.Apply(type.Sprite);

            XpValue = type.Xp;

            _damageable.MaxHp = type.Hp;
            _damageable.Hp = type.Hp;
            _damageable.NotifyHealthChanged();

            _motor.MoveSpeed = type.MoveSpeed;
            _attack.Damage = type.AttackDamage;

            transform.localScale = _baseScale * Mathf.Max(0.1f, type.Scale);

            Color tint;
            if (ColorUtility.TryParseHtmlString(type.Color, out tint))
                ApplyTint(tint, true);
            else
                ApplyTint(Color.white, false);
        }

        public void ApplyRoundScaling(float hpMul, float damageMul)
        {
            if (_damageable != null && hpMul > 0f)
            {
                _damageable.MaxHp *= hpMul;
                _damageable.Hp = _damageable.MaxHp;
                _damageable.NotifyHealthChanged();
            }
            if (_attack != null && damageMul > 0f)
                _attack.Damage *= damageMul;
        }

        public bool IsBoss { get; private set; }

        public void MarkAsBoss()
        {
            IsBoss = true;
            ShowHpBar(true);
        }

        private void ShowHpBar(bool on)
        {
            if (_hpBar == null)
                _hpBar = GetComponentInChildren<Jinhyeong_UI.WorldHpBar>(true);
            if (_hpBar != null)
                _hpBar.gameObject.SetActive(on);
        }

        private Jinhyeong_UI.WorldHpBar _hpBar;

        public void ApplyEventMultipliers(float hpMul, float damageMul, float scaleMul, float xpMul, float speedMul = 1f, bool tintAsElite = true)
        {
            IsElite = true;
            _damageable.MaxHp *= Mathf.Max(0.01f, hpMul);
            _damageable.Hp = _damageable.MaxHp;
            _damageable.NotifyHealthChanged();
            _attack.Damage *= Mathf.Max(0.01f, damageMul);
            transform.localScale *= Mathf.Max(0.1f, scaleMul);
            if (_motor != null && speedMul > 0f)
                _motor.MoveSpeed *= speedMul;
            XpValue *= Mathf.Max(0f, xpMul);

            if (tintAsElite)
                ApplyTint(new Color(1f, 0.55f, 0.5f), true);
        }

        private void ApplyTint(Color color, bool apply)
        {
            if (_tintRenderers == null)
                _tintRenderers = GetComponentsInChildren<Renderer>(true);
            if (_tintMpb == null)
                _tintMpb = new MaterialPropertyBlock();

            for (int i = 0; i < _tintRenderers.Length; i++)
            {
                Renderer r = _tintRenderers[i];
                if (r == null)
                    continue;
                if (apply == false)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                _tintMpb.SetColor(BaseColorId, color);
                _tintMpb.SetColor(ColorId, color);
                r.SetPropertyBlock(_tintMpb);
            }
        }

        private void SubscribeDeath()
        {
            if (_subscribed)
                return;
            _damageable.OnDied += HandleDied;
            _subscribed = true;
        }

        private void UnsubscribeDeath()
        {
            if (_subscribed == false)
                return;
            _damageable.OnDied -= HandleDied;
            _subscribed = false;
        }

        private void HandleDied(Damageable d, SkillObject src)
        {
            if (OnDespawnRequested != null)
            {
                OnDespawnRequested.Invoke(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
