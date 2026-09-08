using TMPro;
using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem
{

    /// <summary>
    /// 피해 숫자. Fx_DamageText 프리팹(3D TextMeshPro)에 박혀 Pool_Fx 로 재사용된다.
    /// 위로 떠오르며 사라진다. 카메라를 향해 눕는다(빌보드).
    /// </summary>
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

            // 같은 자리에 연타가 겹치지 않게 살짝 흩뿌린다
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

            // 처음 빠르게 튀어오르고 천천히 멎는다
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
