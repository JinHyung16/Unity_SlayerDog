using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Character;

namespace Jinhyeong_View
{
    /// <summary>보는 방향. 왼쪽은 옆모습을 좌우 반전해 쓰므로 그림이 3벌이면 된다.</summary>
    public enum EViewDir
    {
        Down,
        Up,
        Side
    }

    /// <summary>
    /// 캐릭터 스프라이트 비주얼. 이동 여부로 대기/걷기를 고르고, 이동 방향으로 4방향을 고른다.
    ///
    /// [사고] 처음엔 정면 그림 한 벌에 flipX 만 썼다. 그런데 그 그림이 좌우 대칭이라
    /// 뒤집어도 눈에 띄는 변화가 없었고, 위아래로 걸어도 계속 정면을 봐서 방향이 안 읽혔다.
    /// 탑다운에서 방향이 안 읽히면 어디로 가는지·무엇을 겨누는지가 전부 흐려진다.
    ///
    /// 스프라이트는 카메라를 향해 세워 두므로(Billboard) 좌우 반전은 flipX 로 처리한다.
    /// 월드는 XZ 평면이라 화면의 "위/아래"는 z, "좌/우"는 x 다.
    /// </summary>
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

        /// <summary>이 값보다 z 성분이 크게 우세해야 위/아래로 본다. 없으면 대각선에서 방향이 떤다.</summary>
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

        /// <summary>적 타입별 색을 입힐 때 쓴다. 피격 플래시가 원래 색으로 되돌아오도록 기준색도 바꾼다.</summary>
        public void SetTint(Color c)
        {
            _baseColor = c;
            if (_renderer != null)
                _renderer.color = c;
        }

        /// <summary>런타임에 비주얼을 갈아 끼운다 (적 타입 교체용).</summary>
        public void SetFrameSet(Sprite[] idle, Sprite[] walk)
        {
            SetFrameSet(idle, walk, null, null);
        }

        /// <summary>
        /// 방향 그림까지 갈아 끼운다.
        ///
        /// up/side 를 비우면 Pick 이 정면으로 떨어진다 — 납치선처럼 **방향 개념이 없는 개체**는
        /// 그림을 만들 필요 없이 그냥 안 주면 된다.
        /// </summary>
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

            // 방향은 **움직일 때만** 갱신한다. 멈춘 순간의 미세한 축 값으로 방향이 바뀌면
            // 제자리에서 앞뒤로 홱홱 돌아간다.
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

            // 왼쪽은 옆모습을 뒤집어 쓴다. 정면/뒷모습은 대칭이라 뒤집을 이유가 없다.
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
            // z 가 x 보다 확실히 클 때만 위/아래. 같으면 옆모습이 낫다 —
            // 옆이 실루엣이 가장 또렷해 대각선에서도 잘 읽힌다.
            if (Mathf.Abs(axis.y) > Mathf.Abs(axis.x) * DirBias)
                return axis.y > 0f ? EViewDir.Up : EViewDir.Down;
            return EViewDir.Side;
        }

        /// <summary>방향 그림이 비어 있으면 정면 그림으로 떨어진다 — 적처럼 한 벌만 있는 경우.</summary>
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
