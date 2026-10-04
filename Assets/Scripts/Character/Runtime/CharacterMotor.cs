using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Collision;

namespace Jinhyeong_Character
{

    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : BaseBehaviour
    {
        [HideInInspector] public float MoveSpeed;

        [HideInInspector] public float StatusSpeedMultiplier = 1f;

        private Vector3 _impulse;

        protected override void OnDisabled()
        {
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
