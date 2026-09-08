using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Managers;
using Jinhyeong_View;

namespace Jinhyeong_SkillSystem
{
    /// <summary>
    /// 피격 순간 재생되는 2D 스프라이트 연출.
    ///
    /// 프리팹(Fx_Hit)에 프레임이 박혀 있고 풀에서 꺼내 쓴다.
    /// 원본 이펙트 시트는 흰색 변형만 반입했다 — 색은 SpriteRenderer.color 로 입힌다.
    /// 덕분에 스킬 속성 색(DB 의 SkillVisual.color)을 그대로 반영하면서 스프라이트는 한 벌만 쓴다.
    /// </summary>
    public class HitEffect : BaseBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private SpriteAnimator _animator;

        [Tooltip("프레임을 몇 초에 걸쳐 다 재생할지. Spawn 의 life 로 덮어쓴다.")]
        [SerializeField] private float _defaultLife = 0.3f;

        private float _life;
        private float _age;
        private Vector3 _baseScale;

        public static void Spawn(Vector3 pos, Color color, float life = 0.3f, float size = 1.5f)
        {
            if (PoolManager.Instance == null)
                return;

            GameObject go = PoolManager.Instance.Pool_Fx_Get(CommonConfig.Skill.HitFxKey);
            if (go == null)
                return;

            // 스프라이트는 카메라를 향해 세운다. 눕히면 픽셀이 찌그러진다.
            go.transform.SetPositionAndRotation(pos, CameraFollowRotation());
            go.transform.localScale = Vector3.one * size;

            HitEffect fx = go.GetComponent<HitEffect>();
            if (fx == null)
            {
                Debug.LogWarning($"[HitEffect] '{CommonConfig.Skill.HitFxKey}' 프리팹에 HitEffect 가 없습니다");
                return;
            }
            fx.Init(color, life, go.transform.localScale);
        }

        private static Quaternion CameraFollowRotation()
        {
            return Jinhyeong_Character.CameraFollow.ViewRotation;
        }

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_animator == null && _renderer != null)
                _animator = _renderer.GetComponent<SpriteAnimator>();
        }

        private void Init(Color color, float life, Vector3 baseScale)
        {
            _life = life > 0f ? life : _defaultLife;
            _age = 0f;
            _baseScale = baseScale;

            if (_renderer != null)
                _renderer.color = color;

            if (_animator != null)
            {
                int n = _animator.FrameCount;
                // 프레임 전체가 life 안에 딱 맞게 떨어지도록 fps 를 역산한다.
                float fps = n > 1 ? n / Mathf.Max(0.01f, _life) : 1f;
                _animator.SetFps(fps, false);
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _life;
            if (t >= 1f)
            {
                if (PoolManager.Instance != null)
                    PoolManager.Instance.Pool_Fx_Return(CommonConfig.Skill.HitFxKey, gameObject);
                else
                    Destroy(gameObject);
                return;
            }

            // 살짝 커지며 사라진다. 스프라이트 프레임 자체가 확산하므로 과하게 키우지 않는다.
            transform.localScale = _baseScale * (1f + t * 0.25f);
        }
    }
}
