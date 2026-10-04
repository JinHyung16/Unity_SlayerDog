using System;
using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem.Status;

namespace Jinhyeong_SkillSystem
{

    public class Damageable : BaseBehaviour
    {
        private static readonly Color HurtSparkColor = new Color(1f, 0.32f, 0.22f, 1f);

        public ESkillTeam Team = ESkillTeam.Enemy;
        public float Hp = 100f;
        public float MaxHp = 100f;
        public bool DestroyOnDeath = true;

        public float IncomingDamageMultiplier = 1f;

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

        public void Heal(float amount)
        {
            if (amount <= 0f || IsAlive == false)
                return;
            float before = Hp;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            if (Hp > before && OnHealthChanged != null)
                OnHealthChanged.Invoke(this);
        }

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
            if (damage > 0f && source != null && source.Team == Team)
            {
                FriendlyFireHits++;
                if (damage > FriendlyFireWorst)
                {
                    FriendlyFireWorst = damage;
                    FriendlyFireNote = $"{source.name} -> {name} ({Team})";
                }
            }

            if (IsAlive == false)
                return false;

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
                Jinhyeong_Managers.SfxManager.Play(
                    Team == ESkillTeam.Friend
                        ? Jinhyeong_Managers.ESfx.Hurt
                        : Jinhyeong_Managers.ESfx.Hit,
                    Team == ESkillTeam.Friend ? 1f : 0.5f);

                _lastHitTime = Time.time;
                // 플레이어가 맞으면 위험 색(레드오렌지) 스파크. 적이 맞을 때는 SkillHit 가 스킬 색 버스트를 낸다
                if (Team == ESkillTeam.Friend)
                    HitEffect.Spawn(transform.position + Vector3.up * 1f, HurtSparkColor);
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
