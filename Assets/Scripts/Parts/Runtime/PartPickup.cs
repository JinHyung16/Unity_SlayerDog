using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Parts
{
    /// <summary>
    /// 필드에 떨어진 파츠 상자. 밟으면 그 자리에서 착용한다.
    ///
    /// 뱀서류 템포에서 「교체할까요?」 팝업으로 멈춰 세우면 흐름이 끊긴다.
    /// 그래서 빈 부위면 무조건 착용, 찬 부위면 옵션이 더 많을 때만 자동 교체한다.
    /// 판단 기준은 PartLoadout.IsUpgrade 한 곳에만 있다.
    /// </summary>
    public class PartPickup : BaseBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("이 반경에 들어오면 획득. 상자는 자석으로 끌려오지 않는다 — 찾아가는 맛이 있어야 한다.")]
        [SerializeField] private float _pickupRadius = 0.9f;

        [Tooltip("이 시간이 지나면 사라진다. 0 이면 영구.")]
        [SerializeField] private float _lifetime = 0f;

        private PartInstance _payload;
        private float _spawnTime;

        /// <summary>등급별 상자 색. 굳이 아이콘을 나눌 필요 없이 색만으로 읽힌다.</summary>
        private static readonly Color[] GradeColors =
        {
            new Color(1f, 1f, 1f, 1f),               // Common
            new Color(0.55f, 0.85f, 1f, 1f),         // Rare
            new Color(1f, 0.82f, 0.35f, 1f)          // Legendary
        };

        public PartInstance Payload { get { return _payload; } }

        public void Setup(PartInstance inst)
        {
            _payload = inst;
            _spawnTime = Time.time;

            if (_renderer == null)
                _renderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_renderer != null && inst != null)
            {
                int gi = Mathf.Clamp((int)inst.Grade - 1, 0, GradeColors.Length - 1);
                _renderer.color = GradeColors[gi];
            }
        }

        private void Update()
        {
            if (_payload == null)
                return;

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
