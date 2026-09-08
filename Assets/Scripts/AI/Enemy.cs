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

        /// <summary>이번 스폰에 적용된 타입. null 이면 CommonConfig 기본값.</summary>
        public Jinhyeong_SkillSystem.EnemyTypeData Type { get; private set; }

        /// <summary>처치 시 떨어뜨리는 경험치. 타입 XP × 이벤트 배수.</summary>
        public float XpValue { get; private set; }

        /// <summary>정예/보스 이벤트로 스폰됐는지. 이름표·색 구분에 쓴다.</summary>
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
            // [사고] 풀에서 재사용되는 오브젝트다. 표식을 안 지우면 다음 판의 잡졸이 보스가 되어
            // 아무나 죽여도 클리어가 뜬다.
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

        /// <summary>
        /// 타입 데이터로 수치·스케일·색을 덮어쓴다. Init() 뒤에 부른다 —
        /// Init 이 CommonConfig 기본값으로 리셋하므로 순서가 바뀌면 타입이 지워진다.
        /// </summary>
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
                // [사고] 여기서 그냥 빠져나갔더니 체력·피해가 **이전 개체의 값 그대로** 남았다.
                // 바로 뒤에 라운드 배수가 또 곱해지므로, 풀에서 재사용될 때마다 체력이 불어난다.
                // 타입이 없어도 기본값으로 되돌리는 것이 리셋의 책임이다.
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

            // 2D 비주얼 교체. 프리팹은 하나고 타입만 DB 로 바뀐다.
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

        /// <summary>
        /// 라운드 배수. 정예 배수와 달리 **모든 적**에게 걸리고 틴트를 바꾸지 않는다 —
        /// 라운드가 올랐다고 잡졸 색이 변하면 정예와 구분이 안 된다.
        /// </summary>
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

        /// <summary>
        /// 정예/보스 이벤트 배수. ApplyType 뒤에 부른다. 체력은 최대치와 현재치를 같이 올린다.
        /// </summary>
        /// <summary>보스인가. 처치하면 런이 클리어된다 — 판정은 데이터(WaveEvent.IsBoss)가 정한다.</summary>
        public bool IsBoss { get; private set; }

        public void MarkAsBoss()
        {
            IsBoss = true;
            ShowHpBar(true);
        }

        /// <summary>
        /// 잡몹은 HP 바를 안 보여준다.
        ///
        /// 화면에 100 마리가 깔리는 게임이라 바가 100 개면 **화면이 바로 안 읽힌다.**
        /// 잡졸은 몇 대 때리면 죽으므로 남은 체력을 알 이유도 없다. 보스만 「얼마나 남았나」가 정보다.
        /// </summary>
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

            // 정예 표시. [사고] 3D 프록시 시절엔 흰 모델에 어두운 색을 곱해 구분했는데,
            // 2D 스프라이트는 이미 색을 갖고 있어 같은 값을 곱하면 형체를 알 수 없는 검은 실루엣이 된다.
            // 스프라이트를 살리면서 눈에 띄려면 어둡게가 아니라 **뜨겁게** 물들여야 한다.
            //
            // [사고] 이 틴트를 보스에도 씌웠더니 납치선이 주황 덩어리가 돼 UFO 로 안 읽혔다.
            // 틴트는 「똑같이 생긴 잡졸 중 정예」를 가리키는 장치다. 제 스프라이트가 있는 개체에는
            // 해로울 뿐이다.
            if (tintAsElite)
                ApplyTint(new Color(1f, 0.55f, 0.5f), true);
        }

        /// <summary>프록시 구분용 틴트. 머티리얼 인스턴스를 만들지 않고 PropertyBlock 으로만.</summary>
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
