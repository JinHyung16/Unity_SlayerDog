using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Character;

namespace Jinhyeong_View
{
    public enum EViewDir
    {
        Down,
        Up,
        Side
    }

    public class CharacterView : BaseBehaviour
    {
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private CharacterFacing _facing;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private SpriteAnimator _animator;

        [Header("Frames")]
        [SerializeField] private Sprite[] _idle;
        [SerializeField] private Sprite[] _walk;

        [Header("Frames — 위/옆 (비우면 정면 그림을 그대로 쓴다)")]
        [SerializeField] private Sprite[] _idleUp;
        [SerializeField] private Sprite[] _walkUp;
        [SerializeField] private Sprite[] _idleSide;
        [SerializeField] private Sprite[] _walkSide;

        [SerializeField] private float _idleFps = 4f;
        [SerializeField] private float _walkFps = 10f;

        [Tooltip("이 이상 움직이면 걷기로 본다. 0 으로 두면 미세 흔들림에도 걷기가 켜진다.")]
        [SerializeField] private float _moveThreshold = 0.05f;

        [Tooltip("피격 시 잠깐 흰색으로 물들이는 시간.")]
        [SerializeField] private float _flashTime = 0.08f;

        private bool _walking;
        private float _flashUntil;
        private Color _baseColor = Color.white;
        private EViewDir _dir = EViewDir.Down;
        private bool _dirDirty = true;

        private const float DirBias = 1.15f;

        public SpriteRenderer Renderer { get { return _renderer; } }

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_animator == null && _renderer != null)
                _animator = _renderer.GetComponent<SpriteAnimator>();
            if (_renderer != null)
                _baseColor = _renderer.color;
        }

        protected override void OnEnabled()
        {
            _walking = false;
            _dir = EViewDir.Down;
            _dirDirty = true;
            _flashUntil = 0f;
            if (_renderer != null)
                _renderer.color = _baseColor;
            PlayIdle();
        }

        public void SetTint(Color c)
        {
            _baseColor = c;
            if (_renderer != null)
                _renderer.color = c;
        }

        public void SetFrameSet(Sprite[] idle, Sprite[] walk)
        {
            SetFrameSet(idle, walk, null, null);
        }

        public void SetFrameSet(Sprite[] idle, Sprite[] walk, Sprite[] idleUp, Sprite[] walkUp, Sprite[] idleSide, Sprite[] walkSide)
        {
            _idle = idle;
            _walk = walk;
            _idleUp = idleUp;
            _walkUp = walkUp;
            _idleSide = idleSide;
            _walkSide = walkSide;
            _walking = false;
            _dirDirty = true;
            PlayIdle();
        }

        public void SetFrameSet(Sprite[] idle, Sprite[] walk, Sprite[] up, Sprite[] side)
        {
            _idle = idle;
            _walk = walk;
            _idleUp = up;
            _walkUp = up;
            _idleSide = side;
            _walkSide = side;
            _walking = false;
            _dirDirty = true;
            PlayIdle();
        }

        public void Flash()
        {
            _flashUntil = Time.time + _flashTime;
            if (_renderer != null)
                _renderer.color = Color.white;
        }

        private void Update()
        {
            if (_flashUntil > 0f && Time.time >= _flashUntil)
            {
                _flashUntil = 0f;
                if (_renderer != null)
                    _renderer.color = _baseColor;
            }

            if (_motor == null)
                return;

            Vector2 axis = _motor.MoveAxis;
            bool moving = axis.sqrMagnitude > _moveThreshold * _moveThreshold;

            if (moving)
            {
                EViewDir want = DirOf(axis);
                if (want != _dir)
                {
                    _dir = want;
                    _dirDirty = true;
                }
            }

            if (moving != _walking || _dirDirty)
            {
                _walking = moving;
                _dirDirty = false;
                if (moving)
                    PlayWalk();
                else
                    PlayIdle();
            }

            if (_renderer == null)
                return;

            if (_dir == EViewDir.Side)
            {
                if (Mathf.Abs(axis.x) > 0.01f)
                    _renderer.flipX = axis.x < 0f;
            }
            else
            {
                _renderer.flipX = false;
            }
        }

        private static EViewDir DirOf(Vector2 axis)
        {
            if (Mathf.Abs(axis.y) > Mathf.Abs(axis.x) * DirBias)
                return axis.y > 0f ? EViewDir.Up : EViewDir.Down;
            return EViewDir.Side;
        }

        private Sprite[] Pick(Sprite[] up, Sprite[] side, Sprite[] down)
        {
            Sprite[] want = _dir == EViewDir.Up ? up : (_dir == EViewDir.Side ? side : down);
            return (want != null && want.Length > 0) ? want : down;
        }

        private void PlayIdle()
        {
            if (_animator == null)
                return;
            Sprite[] f = Pick(_idleUp, _idleSide, _idle);
            if (f == null || f.Length == 0)
                f = Pick(_walkUp, _walkSide, _walk);
            if (f != null && f.Length > 0)
                _animator.SetFrames(f, _idleFps, true);
        }

        private void PlayWalk()
        {
            if (_animator == null)
                return;
            Sprite[] f = Pick(_walkUp, _walkSide, _walk);
            if (f == null || f.Length == 0)
                f = Pick(_idleUp, _idleSide, _idle);
            if (f != null && f.Length > 0)
                _animator.SetFrames(f, _walkFps, true);
        }
    }
}
