using TMPro;
using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem
{

    public class DamagePopup : BaseBehaviour
    {
        [SerializeField] private TextMeshPro _text;

        private float _age;
        private Vector3 _start;
        private Color _baseColor;
        private Vector3 _baseScale = Vector3.zero;
        private bool _counted;

        // 후반 광역 판에서 숫자가 화면을 덮었다 — 1 미만(표시 0)은 안 띄우고, 적 피해 숫자는 프레임당 · 동시 상한을 둔다
        // 플레이어가 맞은 숫자는 상한 없이 늘 띄운다 (위험 정보)
        private const int MaxPerFrame = 4;
        private const int MaxAlive = 28;
        private const float EnemyHitScale = 0.72f;
        private static int _frame = -1;
        private static int _thisFrame;
        private static int _alive;

        public static void Spawn(Vector3 worldPos, float amount, bool isPlayerHit)
        {
            if (PoolManager.Instance == null || amount < 1f)
                return;
            if (isPlayerHit == false)
            {
                if (_frame != Time.frameCount)
                {
                    _frame = Time.frameCount;
                    _thisFrame = 0;
                }
                if (_thisFrame >= MaxPerFrame || _alive >= MaxAlive)
                    return;
                _thisFrame++;
            }

            GameObject go = PoolManager.Instance.Pool_Fx_Get(CommonConfig.Skill.DamageTextKey);
            if (go == null)
                return;

            DamagePopup popup = go.GetComponent<DamagePopup>();
            if (popup == null)
                return;

            Vector3 jitter = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.2f, 0.2f));
            go.transform.position = worldPos + jitter;
            popup.Init(amount, isPlayerHit);
        }

        private void Init(float amount, bool isPlayerHit)
        {
            if (_text == null)
                _text = GetComponent<TextMeshPro>();

            _age = 0f;
            _start = transform.position;
            if (_baseScale == Vector3.zero)
                _baseScale = transform.localScale;
            transform.localScale = isPlayerHit ? _baseScale : _baseScale * EnemyHitScale;
            if (_counted == false)
            {
                _counted = true;
                _alive++;
            }
            _baseColor = isPlayerHit ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.92f, 0.4f);

            if (_text != null)
            {
                _text.text = amount >= 10000f ? (amount / 1000f).ToString("0.#") + "k" : Mathf.RoundToInt(amount).ToString();
                _text.color = _baseColor;
            }
        }

        // 풀 반환 · 장면 정리 어느 쪽으로 꺼져도 동시 개수를 돌려놓는다
        protected override void OnDisabled()
        {
            if (_counted == false)
                return;
            _counted = false;
            _alive = Mathf.Max(0, _alive - 1);
        }

        private void Update()
        {
            float life = CommonConfig.Combat.DamageTextLife;
            _age += Time.deltaTime;
            float t = _age / life;

            if (t >= 1f)
            {
                if (PoolManager.Instance != null)
                    PoolManager.Instance.Pool_Fx_Return(CommonConfig.Skill.DamageTextKey, gameObject);
                else
                    Destroy(gameObject);
                return;
            }

            float rise = CommonConfig.Combat.DamageTextRise * (1f - (1f - t) * (1f - t));
            transform.position = _start + Vector3.up * rise;

            if (_text != null)
            {
                Color c = _baseColor;
                c.a = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
                _text.color = c;
            }

            Camera cam = CameraFollow.GameCamera;
            if (cam != null)
                transform.rotation = cam.transform.rotation;
        }
    }
}
