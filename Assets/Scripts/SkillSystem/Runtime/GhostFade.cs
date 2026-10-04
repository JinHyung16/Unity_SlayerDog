using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem
{
    // 이동형 스킬의 잔상 한 장. 지나간 자리에 심지 그림을 스킬 색으로 남기고(가산) 사라진다.
    // SkillVisual 이 움직일 때마다 풀에서 꺼내 쓴다 (CommonConfig.Skill.GhostKey).
    public class GhostFade : BaseBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private float _life;
        private float _age;
        private Color _color;
        private Vector3 _baseScale;

        public static void Spawn(SpriteRenderer source, Color color)
        {
            if (source == null || source.sprite == null || PoolManager.Instance == null)
                return;

            GameObject go = PoolManager.Instance.Pool_Fx_Get(CommonConfig.Skill.GhostKey);
            if (go == null)
                return;

            Transform src = source.transform;
            go.transform.SetPositionAndRotation(src.position, src.rotation);
            go.transform.localScale = src.lossyScale;

            GhostFade ghost = go.GetComponent<GhostFade>();
            if (ghost == null)
            {
                Debug.LogWarning($"[GhostFade] '{CommonConfig.Skill.GhostKey}' 프리팹에 GhostFade 가 없습니다");
                return;
            }
            ghost.Init(source.sprite, source.flipX, color);
        }

        private void Init(Sprite sprite, bool flipX, Color color)
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.flipX = flipX;
            _color = color;
            _color.a = CommonConfig.Skill.GhostAlpha;
            _renderer.color = _color;
            _life = CommonConfig.Skill.GhostLife;
            _age = 0f;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _life;
            if (t >= 1f)
            {
                PoolManager.Instance.Pool_Fx_Return(CommonConfig.Skill.GhostKey, gameObject);
                return;
            }

            Color c = _color;
            c.a = _color.a * (1f - t);
            _renderer.color = c;
            transform.localScale = _baseScale * (1f - 0.25f * t);
        }
    }
}
