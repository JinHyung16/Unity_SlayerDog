using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Managers;
using Jinhyeong_View;

namespace Jinhyeong_SkillSystem
{
    public class HitEffect : BaseBehaviour
    {
        // 번짐(Glow) · 심지(Core) 두 층. 색은 KeepOwnColor 가중치대로, 프레임 속도는 층 전부 같이
        [SerializeField] private SpriteRenderer[] _renderers;
        [SerializeField] private SpriteAnimator[] _animators;

        [Tooltip("프레임을 몇 초에 걸쳐 다 재생할지. Spawn 의 life 로 덮어쓴다.")]
        [SerializeField] private float _defaultLife = 0.3f;

        private float _life;
        private float _age;
        private Vector3 _baseScale;

        public static void Spawn(Vector3 pos, Color color, float life = 0.3f, float size = 1.5f)
        {
            SpawnKey(CommonConfig.Skill.HitFxKey, pos, color, life, size);
        }

        // 스킬 적중 버스트. 광역 스킬이 한 프레임에 수십 마리를 때리면 그만큼 생기므로 프레임당 상한을 둔다
        public static bool SpawnBurst(Vector3 pos, Color color)
        {
            if (_burstFrame != Time.frameCount)
            {
                _burstFrame = Time.frameCount;
                _burstsThisFrame = 0;
            }
            if (_burstsThisFrame >= CommonConfig.Skill.MaxBurstsPerFrame)
                return false;
            _burstsThisFrame++;
            return SpawnKey(CommonConfig.Skill.HitBurstKey, pos, color, 0.32f, 1.4f);
        }

        private static int _burstFrame = -1;
        private static int _burstsThisFrame;

        public static bool SpawnKey(string key, Vector3 pos, Color color, float life, float size)
        {
            if (PoolManager.Instance == null)
                return false;

            GameObject go = PoolManager.Instance.Pool_Fx_Get(key);
            if (go == null)
                return false;

            go.transform.SetPositionAndRotation(pos, CameraFollowRotation());
            go.transform.localScale = Vector3.one * size;

            HitEffect fx = go.GetComponent<HitEffect>();
            if (fx == null)
            {
                Debug.LogWarning($"[HitEffect] '{key}' 프리팹에 HitEffect 가 없습니다");
                return false;
            }
            fx._poolKey = key;
            fx.Init(color, life, go.transform.localScale);
            return true;
        }

        private string _poolKey = CommonConfig.Skill.HitFxKey;

        private static Quaternion CameraFollowRotation()
        {
            return Jinhyeong_Character.CameraFollow.ViewRotation;
        }

        private void Awake()
        {
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (_animators == null || _animators.Length == 0)
                _animators = GetComponentsInChildren<SpriteAnimator>(true);
        }

        private void Init(Color color, float life, Vector3 baseScale)
        {
            _life = life > 0f ? life : _defaultLife;
            _age = 0f;
            _baseScale = baseScale;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].color = KeepOwnColor.Resolve(_renderers[i], color);
            }
            for (int i = 0; i < _animators.Length; i++)
            {
                SpriteAnimator anim = _animators[i];
                if (anim == null)
                    continue;
                int n = anim.FrameCount;
                float fps = n > 1 ? n / Mathf.Max(0.01f, _life) : 1f;
                anim.SetFps(fps, false);
                anim.Rewind();
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _life;
            if (t >= 1f)
            {
                if (PoolManager.Instance != null)
                    PoolManager.Instance.Pool_Fx_Return(_poolKey, gameObject);
                else
                    Destroy(gameObject);
                return;
            }

            transform.localScale = _baseScale * (1f + t * 0.25f);
        }
    }
}
