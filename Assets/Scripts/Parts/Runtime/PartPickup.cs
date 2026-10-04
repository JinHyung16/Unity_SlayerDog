using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Parts
{
    public class PartPickup : BaseBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        // 스피커 테두리 빛 — 등급색으로 틴트하고 맥박처럼 깜빡인다. 몸(_renderer)은 제 색 그대로
        [SerializeField] private SpriteRenderer _glow;

        [Tooltip("이 반경에 들어오면 획득. 상자는 자석으로 끌려오지 않는다 — 찾아가는 맛이 있어야 한다.")]
        [SerializeField] private float _pickupRadius = 0.9f;

        [Tooltip("이 시간이 지나면 사라진다. 0 이면 영구.")]
        [SerializeField] private float _lifetime = 0f;

        private PartInstance _payload;
        private float _spawnTime;

        private static readonly Color AmpColor = new Color(1f, 0.30f, 0.72f, 1f);
        private Color _glowColor = Color.white;

        private static readonly Color[] GradeColors =
        {
            new Color(1f, 1f, 1f, 1f),
            new Color(0.18f, 0.90f, 1f, 1f),
            new Color(1f, 0.93f, 0.32f, 1f)
        };

        public PartInstance Payload { get { return _payload; } }

        public void Setup(PartInstance inst)
        {
            _payload = inst;
            _spawnTime = Time.time;

            if (_glow != null && inst != null)
            {
                int gi = Mathf.Clamp((int)inst.Grade - 1, 0, GradeColors.Length - 1);
                // 기본 공격 앰프(고유 파츠)는 마젠타 — 무기와 한눈에 갈린다
                _glowColor = inst.Data != null && inst.Data.Owner > 0 ? AmpColor : GradeColors[gi];
                _glow.color = _glowColor;
            }
        }

        private void Update()
        {
            if (_payload == null)
                return;

            if (_glow != null)
            {
                Color c = _glowColor;
                c.a = 0.65f + 0.35f * Mathf.Sin((Time.time - _spawnTime) * 6f);
                _glow.color = c;
            }

            if (_lifetime > 0f && Time.time - _spawnTime > _lifetime)
            {
                Despawn();
                return;
            }

            Jinhyeong_Character.Player p = GameEvents.CurrentPlayer;
            if (p == null)
                return;

            Vector3 to = p.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > _pickupRadius * _pickupRadius)
                return;

            Collect(p);
        }

        private void Collect(Jinhyeong_Character.Player p)
        {
            PartLoadout loadout = p.GetComponent<PartLoadout>();
            if (loadout == null)
            {
                Despawn();
                return;
            }

            if (loadout.IsUpgrade(_payload))
                loadout.TryEquip(_payload);

            Despawn();
        }

        private void Despawn()
        {
            _payload = null;
            if (PoolManager.Instance != null)
                PoolManager.Instance.Pool_Fx_Return(PartDropper.PickupKey, gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}
