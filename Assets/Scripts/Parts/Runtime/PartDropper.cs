using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Parts
{
    public class PartDropper : BaseBehaviour
    {
        public const string PickupKey = "Fx_PartBox";

        public static PartDropper Instance { get; private set; }

        [Tooltip("런 시작 후 첫 상자까지의 시간(초). 알몸 구간을 짧게 유지한다.")]
        [SerializeField] private float _firstDropDelay = 4f;

        // 간격은 고정이 아니다 — 슬롯이 찰수록 길어진다 (CommonConfig.Run.DropInterval*). 6칸이 1분에 차면 성장이 멈춘다 (10-04 실측)

        [Tooltip("플레이어 기준 드랍 반경. 화면 안에 보여야 주우러 갈 마음이 생긴다.")]
        [SerializeField] private float _dropRadiusMin = 3.5f;
        [SerializeField] private float _dropRadiusMax = 7f;

        private float _nextDropTime;
        private bool _running;

        protected override void OnEnabled()
        {
            Instance = this;
            GameEvents.OnPlayerSpawned += HandlePlayerSpawned;

            HandlePlayerSpawned(GameEvents.CurrentPlayer);
        }

        protected override void OnDisabled()
        {
            if (Instance == this)
                Instance = null;
            GameEvents.OnPlayerSpawned -= HandlePlayerSpawned;
        }

        private void HandlePlayerSpawned(Jinhyeong_Character.Player p)
        {
            _running = p != null;
            _nextDropTime = Time.time + _firstDropDelay;
        }

        private void Update()
        {
            if (_running == false || Time.time < _nextDropTime)
                return;

            Jinhyeong_Character.Player p = GameEvents.CurrentPlayer;
            if (p == null)
            {
                _nextDropTime = Time.time + CommonConfig.Run.DropIntervalBase;
                return;
            }
            PartLoadout owned = p.GetComponent<PartLoadout>();
            int equipped = owned != null ? owned.EquippedCount : 0;
            bool full = equipped >= PartLoadout.SlotCount;
            _nextDropTime = Time.time + (full
                ? CommonConfig.Run.DropIntervalFull
                : CommonConfig.Run.DropIntervalBase + CommonConfig.Run.DropIntervalPerPart * equipped);

            float ang = Random.value * Mathf.PI * 2f;
            float r = Random.Range(_dropRadiusMin, _dropRadiusMax);
            Vector3 pos = p.transform.position + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            DropAt(pos, p);
        }

        // 스피커가 «내 기본 공격 앰프»를 줄 확률. 주우면 같은 스킬이라 옵션이 기본 공격에 합쳐진다 (PartLoadout.TryEquip 병합 경로)
        private static PartData RollAmp(PartLoadout loadout)
        {
            if (loadout == null)
                return null;
            PartInstance basic = loadout.UniquePart;
            if (basic == null || basic.Options.Count >= PartInstance.MaxOptions)
                return null;
            float chance = loadout.EquippedCount >= PartLoadout.SlotCount ? CommonConfig.Run.AmpChanceFull : CommonConfig.Run.AmpChance;
            if (Random.value >= chance)
                return null;
            return basic.Data;
        }

        public void DropAt(Vector3 pos, Jinhyeong_Character.Player player = null)
        {
            if (PoolManager.Instance == null)
                return;

            PartLoadout loadout = player != null ? player.GetComponent<PartLoadout>() : null;

            PartData data = RollAmp(loadout);
            for (int i = 0; i < 12 && data == null; i++)
            {
                PartData c = PartRegistry.Roll(loadout != null ? loadout.PreferredFamily : ESkillFamily.None);
                if (c == null)
                    return;
                if (loadout == null || loadout.CanUse(c))
                {
                    data = c;
                    break;
                }
            }
            if (data == null)
                return;

            PartInstance inst = PartInstance.Create(data, PartInstance.RollGrade());
            if (inst == null)
                return;

            GameObject go = PoolManager.Instance.Pool_Fx_Get(PickupKey);
            if (go == null)
            {
                Debug.LogWarning($"[PartDropper] '{PickupKey}' 풀에서 꺼내지 못했습니다");
                return;
            }

            go.transform.SetPositionAndRotation(pos, Jinhyeong_Character.CameraFollow.ViewRotation);
            PartPickup pickup = go.GetComponent<PartPickup>();
            if (pickup == null)
            {
                Debug.LogWarning($"[PartDropper] '{PickupKey}' 프리팹에 PartPickup 이 없습니다");
                return;
            }
            pickup.Setup(inst);
        }
    }
}
