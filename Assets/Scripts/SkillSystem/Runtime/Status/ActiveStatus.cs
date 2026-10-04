using UnityEngine;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.Status
{

    public sealed class ActiveStatus
    {
        public enum Kind { Buff, Debuff }

        public Kind EffectKind;
        public int EffectId;
        public string EffectName;
        public float Duration;
        public float TickInterval;
        public float Value0;
        public float Value1;

        public EBuffType BuffKind;
        public EDebuffType DebuffKind;
        public SkillObject SourceCaster;

        private float _startTime;
        private float _nextTickTime;
        private bool _entered;

        private Damageable _damageable;
        private SkillObject _owner;
        private Jinhyeong_Character.CharacterMotor _motor;

        public bool IsExpired { get { return Time.time - _startTime >= Duration; } }

        public void Setup(SkillBuffData data, SkillObject source, Damageable damageable, SkillObject owner)
        {
            EffectKind = Kind.Buff;
            EffectId = data.Id;
            EffectName = data.Name;
            Duration = data.Duration;
            TickInterval = data.TickInterval;
            Value0 = data.Value0;
            Value1 = data.Value1;
            BuffKind = data.Type;
            SourceCaster = source;
            Begin(damageable, owner);
        }

        public void Setup(SkillDebuffData data, SkillObject source, Damageable damageable, SkillObject owner)
        {
            EffectKind = Kind.Debuff;
            EffectId = data.Id;
            EffectName = data.Name;
            Duration = data.Duration;
            TickInterval = data.TickInterval;
            Value0 = data.Value0;
            Value1 = data.Value1;
            DebuffKind = data.Type;
            SourceCaster = source;
            Begin(damageable, owner);
        }

        private void Begin(Damageable damageable, SkillObject owner)
        {
            _damageable = damageable;
            _owner = owner;
            _motor = damageable != null ? damageable.GetComponent<Jinhyeong_Character.CharacterMotor>() : null;
            _startTime = Time.time;
            _nextTickTime = TickInterval > 0f ? Time.time + TickInterval : float.MaxValue;

            ApplyEnter();
            _entered = true;
        }

        public void RefreshDuration()
        {
            _startTime = Time.time;
            if (TickInterval > 0f)
            {
                _nextTickTime = Time.time + TickInterval;
            }
        }

        public void Tick()
        {
            if (Time.time < _nextTickTime)
                return;

            _nextTickTime += TickInterval;

            if (EffectKind == Kind.Buff)
            {
                if (BuffKind == EBuffType.HealOverTime && _damageable != null)
                    _damageable.Hp = Mathf.Min(_damageable.MaxHp, _damageable.Hp + Value0);
                return;
            }

            if (DebuffKind == EDebuffType.DamageOverTime && _damageable != null)
                _damageable.TakeDamage(Value0, SourceCaster);
        }

        public void End()
        {
            if (_entered == false)
                return;
            _entered = false;
            ApplyExit();
            _damageable = null;
            _owner = null;
            SourceCaster = null;
        }

        private void ApplyEnter()
        {
            if (EffectKind == Kind.Buff)
            {
                switch (BuffKind)
                {
                    case EBuffType.DamageBoost:
                        if (_owner != null)
                            _owner.OutgoingDamageMultiplier += Value0;
                        break;
                    case EBuffType.AttackSpeedBoost:
                        if (_owner != null)
                            _owner.AttackSpeedMultiplier += Value0;
                        break;
                    case EBuffType.Shield:
                        if (_damageable != null)
                            _damageable.Shield += Value0;
                        break;
                    case EBuffType.MoveSpeedBoost:
                        if (_motor != null)
                            _motor.StatusSpeedMultiplier *= (1f + Value0);
                        break;
                    case EBuffType.MaxHpBoost:
                        if (_damageable != null)
                        {
                            _damageable.MaxHp *= (1f + Value0);
                            _damageable.Hp *= (1f + Value0);
                        }
                        break;
                }
                return;
            }

            switch (DebuffKind)
            {
                case EDebuffType.SlowReduce:
                    if (_motor != null)
                        _motor.StatusSpeedMultiplier *= (1f - Value0);
                    break;
                case EDebuffType.DamageReceiveIncrease:
                    if (_damageable != null)
                        _damageable.IncomingDamageMultiplier += Value0;
                    break;
                case EDebuffType.Stun:
                    if (_damageable != null)
                        _damageable.Stunned = true;
                    if (_owner != null)
                        _owner.Stunned = true;
                    break;
            }
        }

        private void ApplyExit()
        {
            if (EffectKind == Kind.Buff)
            {
                switch (BuffKind)
                {
                    case EBuffType.DamageBoost:
                        if (_owner != null)
                            _owner.OutgoingDamageMultiplier -= Value0;
                        break;
                    case EBuffType.AttackSpeedBoost:
                        if (_owner != null)
                            _owner.AttackSpeedMultiplier -= Value0;
                        break;
                    case EBuffType.Shield:
                        break;
                    case EBuffType.MoveSpeedBoost:
                        if (_motor != null && Mathf.Approximately(Value0, -1f) == false)
                            _motor.StatusSpeedMultiplier /= (1f + Value0);
                        break;
                    case EBuffType.MaxHpBoost:
                        if (_damageable != null && Mathf.Approximately(Value0, -1f) == false)
                        {
                            _damageable.MaxHp /= (1f + Value0);
                            _damageable.Hp = Mathf.Min(_damageable.Hp, _damageable.MaxHp);
                        }
                        break;
                }
                return;
            }

            switch (DebuffKind)
            {
                case EDebuffType.SlowReduce:
                    if (_motor != null && Mathf.Approximately(Value0, 1f) == false)
                        _motor.StatusSpeedMultiplier /= (1f - Value0);
                    break;
                case EDebuffType.DamageReceiveIncrease:
                    if (_damageable != null)
                        _damageable.IncomingDamageMultiplier -= Value0;
                    break;
                case EDebuffType.Stun:
                    if (_damageable != null)
                        _damageable.Stunned = false;
                    if (_owner != null)
                        _owner.Stunned = false;
                    break;
            }
        }
    }
}
