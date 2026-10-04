using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.Skill
{

    public class SkillMethod : BaseBehaviour
    {
        public enum EMotion
        {
            Instant,
            Linear,
            Arc,
            Curve,
            Homing,
        }

        private SkillConfig _config;

        private EMotion _motion;
        private Vector3 _moveDir;
        private float _moveSpeed;
        private float _maxDistance;

        private Vector3 _arcStart;
        private Vector3 _arcEnd;
        private float _arcDuration;
        private float _arcHeight;
        private float _curveAmplitude;

        private Damageable _homingTarget;
        private float _turnRate;

        public EMotion Motion { get { return _motion; } }

        public bool Arrived { get; private set; }

        private void Awake()
        {
            _config = GetComponent<SkillConfig>();
        }

        public void InitInstant()
        {
            _motion = EMotion.Instant;
        }

        public void InitLinear(Vector3 dir, float speed, float maxDist)
        {
            _motion = EMotion.Linear;
            _moveDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            _moveSpeed = speed;
            _maxDistance = maxDist;
        }

        public void InitArc(Vector3 startPos, Vector3 endPos, float speed, float arcHeight)
        {
            _motion = EMotion.Arc;
            _arcStart = startPos;
            _arcEnd = endPos;
            _arcHeight = arcHeight;
            _arcDuration = speed > 0.01f ? Vector3.Distance(startPos, endPos) / speed : 1f;
        }

        public void InitCurve(Vector3 startPos, Vector3 endPos, float speed, float amplitude)
        {
            _motion = EMotion.Curve;
            _arcStart = startPos;
            _arcEnd = endPos;
            _curveAmplitude = amplitude;
            _arcDuration = speed > 0.01f ? Vector3.Distance(startPos, endPos) / speed : 1f;
        }

        public void InitHoming(Damageable target, Vector3 dir, float speed, float maxDist, float turnRate)
        {
            _motion = EMotion.Homing;
            _moveDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            _moveSpeed = speed;
            _maxDistance = maxDist;
            _turnRate = turnRate;
            _homingTarget = target;
        }

        private void Update()
        {
            if (_config == null || _config.Despawning)
                return;

            switch (_motion)
            {
                case EMotion.Instant:
                    break;
                case EMotion.Linear:
                    StepLinear();
                    break;
                case EMotion.Arc:
                    StepArcLike(arc: true);
                    break;
                case EMotion.Curve:
                    StepArcLike(arc: false);
                    break;
                case EMotion.Homing:
                    StepHoming();
                    break;
            }
        }

        private void StepLinear()
        {
            transform.position += _moveDir * _moveSpeed * Time.deltaTime;
            if ((transform.position - _config.StartPos).sqrMagnitude >= _maxDistance * _maxDistance)
                _config.Despawn();
        }

        private void StepHoming()
        {
            if (_homingTarget != null && _homingTarget.IsAlive)
            {
                Vector3 aim = _homingTarget.transform.position + Vector3.up * CommonConfig.Skill.MuzzleHeight;
                Vector3 to = aim - transform.position;
                if (to.sqrMagnitude > 0.0001f)
                {
                    float maxRadians = _turnRate * Mathf.Deg2Rad * Time.deltaTime;
                    _moveDir = Vector3.RotateTowards(_moveDir, to.normalized, maxRadians, 0f).normalized;
                }
            }

            transform.position += _moveDir * _moveSpeed * Time.deltaTime;
            if (_moveDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(_moveDir);

            if ((transform.position - _config.StartPos).sqrMagnitude >= _maxDistance * _maxDistance)
                _config.Despawn();
        }

        private void StepArcLike(bool arc)
        {
            float t = (Time.time - _config.SpawnTime) / Mathf.Max(0.0001f, _arcDuration);
            if (t >= 1f)
            {
                transform.position = _arcEnd;
                _config.Armed = true;
                Arrived = true;
                return;
            }

            Vector3 point = Vector3.Lerp(_arcStart, _arcEnd, t);
            if (arc)
            {
                point.y += _arcHeight * 4f * t * (1f - t);
            }
            else
            {
                Vector3 forward = (_arcEnd - _arcStart).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                if (right.sqrMagnitude < 0.0001f)
                    right = Vector3.Cross(Vector3.forward, forward);
                right.Normalize();
                point += right * (Mathf.Sin(t * Mathf.PI * 2f) * _curveAmplitude);
            }
            transform.position = point;
        }

        protected override void OnDisabled()
        {
            _homingTarget = null;
            _turnRate = 0f;
            _moveSpeed = 0f;
            _maxDistance = 0f;
            Arrived = false;
        }
    }
}
