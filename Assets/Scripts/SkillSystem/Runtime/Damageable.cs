using System;
using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem.Status;

namespace Jinhyeong_SkillSystem
{

    public class Damageable : BaseBehaviour
    {
        public ESkillTeam Team = ESkillTeam.Enemy;
        public float Hp = 100f;
        public float MaxHp = 100f;
        public bool DestroyOnDeath = true;

        public float IncomingDamageMultiplier = 1f;

        /// <summary>
        /// 피격 후 무적 시간(초). 뱀서류의 필수 장치다 — 없으면 붙은 적 N마리가 각자 때려
        /// 초당 피격이 N회가 되고, 어떤 수치를 넣어도 밸런스가 성립하지 않는다.
        /// 0 이면 무적 없음(적에게는 0 을 준다 — 적이 무적이면 광역이 전부 죽는다).
        /// </summary>
        public float InvulnerabilitySeconds = 0f;

        private float _lastHitTime = -999f;
        public float SpeedMultiplier = 1f;
        public float Shield = 0f;
        public bool Stunned = false;

        public event Action<Damageable, SkillObject> OnDied;
        public event Action<Damageable> OnHealthChanged;

        private static readonly List<Damageable> _all = new List<Damageable>(64);
        public static IReadOnlyList<Damageable> All { get { return _all; } }

        public bool IsAlive { get { return Hp > 0f; } }

        private const float FlashDuration = 0.09f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private StatusList _status;

        /// <summary>
        /// 이 대상에게 걸린 버프/디버프. 컴포넌트가 아니라 여기서 소유하고 Update 에서 돌린다 —
        /// 예전처럼 적용마다 AddComponent 하면 빔 같은 다틱 스킬이 컴포넌트를 수십 개 붙인다.
        /// </summary>
        public StatusList Status
        {
            get
            {
                if (_status == null)
                    _status = new StatusList(this, GetComponent<SkillObject>());
                return _status;
            }
        }

        private readonly List<Renderer> _bodyRenderers = new List<Renderer>(4);
        private MaterialPropertyBlock _flashMpb;
        private bool _renderersCached;
        private float _flashUntil;
        private bool _flashing;

        protected override void OnEnabled()
        {
            if (_all.Contains(this) == false)
            {
                _all.Add(this);
            }
            CacheRenderers();
        }

        private void CacheRenderers()
        {
            if (_renderersCached)
                return;
            _renderersCached = true;
            Renderer[] found = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < found.Length; i++)
            {
                Renderer r = found[i];
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    _bodyRenderers.Add(r);
            }
        }

        private void Update()
        {
            if (_status != null)
                _status.Tick();

            if (_flashing == false)
                return;
            if (Time.time < _flashUntil)
                return;

            _flashing = false;
            for (int i = 0; i < _bodyRenderers.Count; i++)
            {
                if (_bodyRenderers[i] != null)
                    _bodyRenderers[i].SetPropertyBlock(null);
            }
        }

        private void StartHitFlash()
        {
            if (_bodyRenderers.Count == 0)
                return;
            if (_flashMpb == null)
                _flashMpb = new MaterialPropertyBlock();
            _flashMpb.SetColor(BaseColorId, Color.white);
            _flashMpb.SetColor(ColorId, Color.white);
            for (int i = 0; i < _bodyRenderers.Count; i++)
            {
                if (_bodyRenderers[i] != null)
                    _bodyRenderers[i].SetPropertyBlock(_flashMpb);
            }
            _flashing = true;
            _flashUntil = Time.time + FlashDuration;
        }

        protected override void OnDisabled()
        {
            // 풀에 들어갔다 재사용될 때 이전 버프/디버프가 남아 있으면 배수·기절이 그대로 딸려온다
            if (_status != null)
                _status.Clear();

            _all.Remove(this);

            if (_flashing)
            {
                _flashing = false;
                for (int i = 0; i < _bodyRenderers.Count; i++)
                {
                    if (_bodyRenderers[i] != null)
                        _bodyRenderers[i].SetPropertyBlock(null);
                }
            }
        }

        public static List<Damageable> GetAllOfTeam(ESkillTeam team, List<Damageable> buffer = null)
        {
            List<Damageable> list = buffer != null ? buffer : new List<Damageable>(_all.Count);
            list.Clear();
            for (int i = 0; i < _all.Count; i++)
            {
                Damageable d = _all[i];
                if (d == null)
                    continue;
                if (d.IsAlive == false)
                    continue;
                if (d.Team != team)
                    continue;
                list.Add(d);
            }
            return list;
        }

        private float _bonusMaxHpMul = 1f;

        /// <summary>
        /// 파츠가 올려주는 최대 체력 배수. 구성이 바뀔 때마다 통째로 다시 들어온다.
        ///
        /// 더하기가 아니라 **배수 교체**여야 한다 — 시너지 재계산은 구성이 바뀔 때마다 도는데,
        /// 그때마다 더하면 가만히 있어도 체력이 불어난다.
        /// 레벨업이 올린 기본 최대 체력과는 곱해진다. 두 성장축이 서로를 지우면 안 된다.
        /// </summary>
        public void SetBonusMaxHpMultiplier(float mul)
        {
            mul = Mathf.Max(1f, mul);
            if (Mathf.Approximately(mul, _bonusMaxHpMul))
                return;

            float baseMax = MaxHp / _bonusMaxHpMul;
            float newMax = Mathf.Max(1f, baseMax * mul);
            float added = newMax - MaxHp;

            MaxHp = newMax;
            _bonusMaxHpMul = mul;
            if (added > 0f)
                Hp = Mathf.Min(MaxHp, Hp + added);

            if (OnHealthChanged != null)
                OnHealthChanged.Invoke(this);
        }

        /// <summary>회복. 죽은 대상은 되살리지 않는다.</summary>
        public void Heal(float amount)
        {
            if (amount <= 0f || IsAlive == false)
                return;
            float before = Hp;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            if (Hp > before && OnHealthChanged != null)
                OnHealthChanged.Invoke(this);
        }

        /// <summary>최대 체력을 올린다. healSame 이면 늘어난 만큼 현재 체력도 채운다.</summary>
        public void AddMaxHp(float amount, bool healSame)
        {
            if (amount <= 0f)
                return;
            MaxHp += amount;
            if (healSame)
                Hp = Mathf.Min(MaxHp, Hp + amount);
            if (OnHealthChanged != null)
                OnHealthChanged.Invoke(this);
        }

        /// <summary>
        /// 아군 오사 감시.
        ///
        /// 이 버그는 두 번 났다. 피해 경로가 다섯 갈래인데 팀 검사를 호출처마다 흩어 둔 탓이고,
        /// 증상이 「가끔 순간사」라 재현이 안 되면 1회 편차로 넘어가 버린다.
        /// 사람이 오버킬 수치를 보고 눈치채는 대신, **들어오는 순간 세어** 리포트로 올린다.
        ///
        /// 무적 판정보다 **앞에서** 본다 — 무적에 막혀 피해가 0 이어도 「같은 팀을 때리려 했다」는
        /// 사실 자체가 버그다.
        /// </summary>
        public static int FriendlyFireHits { get; private set; }
        public static float FriendlyFireWorst { get; private set; }
        public static string FriendlyFireNote { get; private set; } = string.Empty;

        public static void ResetFriendlyFireWatch()
        {
            FriendlyFireHits = 0;
            FriendlyFireWorst = 0f;
            FriendlyFireNote = string.Empty;
        }

        public bool TakeDamage(float damage, SkillObject source)
        {
            // 같은 팀이 때렸다. 회복(음수 피해)은 정상 경로라 세지 않는다.
            if (damage > 0f && source != null && source.Team == Team)
            {
                FriendlyFireHits++;
                if (damage > FriendlyFireWorst)
                {
                    FriendlyFireWorst = damage;
                    FriendlyFireNote = $"{source.name} -> {name} ({Team})";
                }
            }

            // 이미 죽은 대상은 더 맞지 않는다 — OnDied 가 또 터진다
            if (IsAlive == false)
                return false;

            // 무적 시간 중이면 무시한다. 회복(음수 피해)은 막지 않는다.
            if (InvulnerabilitySeconds > 0f && damage > 0f
                && Time.time - _lastHitTime < InvulnerabilitySeconds)
                return false;

            float incoming = damage * IncomingDamageMultiplier;
            if (Shield > 0f)
            {
                float absorbed = Mathf.Min(Shield, incoming);
                Shield -= absorbed;
                incoming -= absorbed;
            }
            Hp -= incoming;

            if (incoming > 0f)
            {
                // 소리는 **피해가 실제로 들어간 자리**에서 낸다. 발사 시점에 내면
                // 빗나가도 소리가 나서 「맞았다」가 거짓말이 된다.
                // 맞는 쪽이 누구냐로 소리가 갈린다 — 내가 때리는 소리와 내가 맞는 소리는 달라야 한다.
                Jinhyeong_Managers.SfxManager.Play(
                    Team == ESkillTeam.Friend
                        ? Jinhyeong_Managers.ESfx.Hurt
                        : Jinhyeong_Managers.ESfx.Hit,
                    Team == ESkillTeam.Friend ? 1f : 0.5f);

                _lastHitTime = Time.time;
                HitEffect.Spawn(transform.position + Vector3.up * 1f, new Color(1f, 0.35f, 0.2f));
                StartHitFlash();
                DamagePopup.Spawn(transform.position + Vector3.up * 1.6f, incoming, Team == ESkillTeam.Friend);
                ApplyKnockback(source);
            }

            if (OnHealthChanged != null)
                OnHealthChanged.Invoke(this);

            bool died = IsAlive == false;

            if (died)
            {
                if (OnDied != null)
                    OnDied.Invoke(this, source);
                if (DestroyOnDeath)
                    Destroy(gameObject);
            }
            return died;
        }

        private Jinhyeong_Character.CharacterMotor _motor;
        private bool _motorResolved;

        /// <summary>가해자 반대 방향으로 민다. 모터가 없는 대상(고정물)은 그냥 넘어간다.</summary>
        private void ApplyKnockback(SkillObject source)
        {
            if (_motorResolved == false)
            {
                _motor = GetComponent<Jinhyeong_Character.CharacterMotor>();
                _motorResolved = true;
            }
            if (_motor == null || source == null)
                return;

            Vector3 dir = transform.position - source.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            _motor.AddImpulse(dir.normalized * CommonConfig.Combat.KnockbackSpeed);
        }

        public void NotifyHealthChanged()
        {
            if (OnHealthChanged != null)
                OnHealthChanged.Invoke(this);
        }
    }
}
