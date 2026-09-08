using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Collision;

namespace Jinhyeong_Character
{

    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : BaseBehaviour
    {
        [HideInInspector] public float MoveSpeed;

        /// <summary>
        /// 버프/디버프가 쓰는 속도 배수. SpeedMultiplier 는 적 AI 가 추격/도주로 매 틱 덮어쓰므로
        /// 상태이상이 그걸 건드리면 다음 틱에 지워진다. 그래서 채널을 분리한다.
        /// </summary>
        [HideInInspector] public float StatusSpeedMultiplier = 1f;

        // 넉백 같은 순간 힘. 입력 이동과 별개 채널로 더해지고 감쇠한다.
        private Vector3 _impulse;

        protected override void OnDisabled()
        {
            // 풀 반납 시 남은 넉백이 재사용 첫 프레임에 튀지 않게
            _impulse = Vector3.zero;
        }

        public void AddImpulse(Vector3 planarVelocity)
        {
            planarVelocity.y = 0f;
            _impulse += planarVelocity;
        }
        [HideInInspector] public float SpeedMultiplier = 1f;

        private CharacterController _cc;
        private OBBCollider _ownBox;
        private float _verticalVelocity;

        public Vector2 MoveAxis { get; set; }
        public CollisionFlags LastCollisionFlags { get; private set; }

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _ownBox = GetComponentInChildren<OBBCollider>();
        }

        private void Update()
        {
            ApplyGravity();
            Move();
        }

        private void ApplyGravity()
        {
            if (_cc.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -1f;
            }
            else
            {
                _verticalVelocity += CommonConfig.Physics.Gravity * Time.deltaTime;
            }
        }

        private void Move()
        {
            Vector2 axis = MoveAxis;
            Vector3 planar = new Vector3(axis.x, 0f, axis.y) * MoveSpeed * SpeedMultiplier * StatusSpeedMultiplier;

            if (_impulse.sqrMagnitude > 0.0001f)
            {
                planar += _impulse;
                _impulse = Vector3.MoveTowards(_impulse, Vector3.zero, CommonConfig.Combat.KnockbackDecay * Time.deltaTime);
            }
            Vector3 planarDelta = planar * Time.deltaTime;

            float radius = _cc.radius;
            Vector3 resolved = OBBPhysics.ResolvePlanarMove(transform.position, planarDelta, radius, _ownBox);
            bool blocked = (resolved - planarDelta).sqrMagnitude > 1e-8f;

            Vector3 delta = new Vector3(resolved.x, _verticalVelocity * Time.deltaTime, resolved.z);
            LastCollisionFlags = _cc.Move(delta);
            if (blocked)
                LastCollisionFlags |= CollisionFlags.Sides;
        }
    }
}
