using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Parts
{
    /// <summary>
    /// 파츠 상자 드랍 관리자.
    ///
    /// 뱀서류라 필드를 넓게 파밍하지 않는다. 대신 두 경로로 파츠가 나온다.
    ///   1) 시간마다 플레이어 근처에 상자가 떨어진다 (초반 알몸 구간을 빨리 벗어나게)
    ///   2) 정예/보스가 죽으면 그 자리에 떨어진다 (위험 보상)
    ///
    /// 0~1분이 가장 위험한 구간이라(알몸 + 기본 물기뿐) 첫 상자는 시작 직후에 준다.
    /// </summary>
    public class PartDropper : BaseBehaviour
    {
        public const string PickupKey = "Fx_PartBox";

        public static PartDropper Instance { get; private set; }

        [Tooltip("런 시작 후 첫 상자까지의 시간(초). 알몸 구간을 짧게 유지한다.")]
        [SerializeField] private float _firstDropDelay = 4f;

        /// <summary>
        /// 이후 상자 간격(초). [실측] 22초면 1분에 2~3개뿐이라 6부위가 차기 전에 라운드가 앞서 나가고,
        /// 생존이 43~106초로 2.4배나 흔들렸다 — 「언제 줍느냐」가 판을 정해버린다.
        /// 성장 속도가 난이도 상승을 따라가야 곡선을 판단할 수 있다.
        /// </summary>
        [SerializeField] private float _dropInterval = 12f;

        [Tooltip("플레이어 기준 드랍 반경. 화면 안에 보여야 주우러 갈 마음이 생긴다.")]
        [SerializeField] private float _dropRadiusMin = 3.5f;
        [SerializeField] private float _dropRadiusMax = 7f;

        private float _nextDropTime;
        private bool _running;

        protected override void OnEnabled()
        {
            Instance = this;
            GameEvents.OnPlayerSpawned += HandlePlayerSpawned;

            // [사고] 구독만 걸고 끝냈더니, 플레이어가 **먼저** 생성된 경로에서는 이벤트가 이미
            // 지나가 버려 드랍이 한 번도 안 돌았다. 상자가 안 떨어지니 「파츠가 안 늘어난다」로 보인다.
            // 이 코드베이스의 다른 추적자들처럼 현재 값을 직접 확인해 시작한다.
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

            _nextDropTime = Time.time + _dropInterval;

            Jinhyeong_Character.Player p = GameEvents.CurrentPlayer;
            if (p == null)
                return;

            float ang = Random.value * Mathf.PI * 2f;
            float r = Random.Range(_dropRadiusMin, _dropRadiusMax);
            Vector3 pos = p.transform.position + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            DropAt(pos, p);
        }

        /// <summary>지정 위치에 파츠 상자를 떨어뜨린다. 적 사망 시에도 호출한다.</summary>
        public void DropAt(Vector3 pos, Jinhyeong_Character.Player player = null)
        {
            if (PoolManager.Instance == null)
                return;

            PartLoadout loadout = player != null ? player.GetComponent<PartLoadout>() : null;

            // 고유 파츠가 잠근 부위는 뽑아봐야 못 쓰므로 다시 굴린다.
            PartData data = null;
            for (int i = 0; i < 12; i++)
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
