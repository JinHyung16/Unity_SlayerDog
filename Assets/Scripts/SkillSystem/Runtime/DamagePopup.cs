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

        public static void Spawn(Vector3 worldPos, float amount, bool isPlayerHit)
        {
            if (PoolManager.Instance == null)
                return;

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
            _baseColor = isPlayerHit ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.92f, 0.4f);

            if (_text != null)
            {
                _text.text = Mathf.RoundToInt(amount).ToString();
                _text.color = _baseColor;
            }
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
