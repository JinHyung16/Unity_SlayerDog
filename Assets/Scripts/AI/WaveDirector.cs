using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_AI
{

    /// <summary>
    /// 무한 스테이지의 스폰 담당. 생존 시간이 길어질수록 더 자주, 더 많이 쏟아낸다.
    ///
    /// 스폰 위치는 **카메라 시야 밖 링**이다. 화면 안에서 튀어나오면 갑툭튀로 보이고,
    /// 너무 멀면 걸어오는 동안 지루하다. 그 사이가 SpawnRingRadius.
    ///
    /// 실제 곡선은 데이터로 빼야 하지만(추후 _DataExporter),
    /// 지금은 감을 잡기 위한 최소 구현이다.
    /// </summary>
    public class WaveDirector : BaseBehaviour
    {
        public static WaveDirector Instance { get; private set; }

        public static WaveDirector Ensure()
        {
            if (Instance != null)
                return Instance;
            GameObject go = new GameObject("_WaveDirector");
            return go.AddComponent<WaveDirector>();
        }

        // 실측(2026-08-22): 1.4s/2마리 → 0.15s/14마리(300s) 는 5분에 93마리/초라 2분에 상한(300)을 친다.
        // 1/초에서 시작해 3분 ~4/초, 5분 ~7/초, 10분 ~30/초. 초반은 스킬이 따라잡고 후반은 군중이 쌓이게.
        [Header("스폰 주기")]
        [Tooltip("시작 시 스폰 간격(초).")]
        [SerializeField] private float _startInterval = 1.0f;

        [Tooltip("도달 가능한 최소 간격(초).")]
        [SerializeField] private float _minInterval = 0.25f;

        [Tooltip("간격이 최소치에 닿는 데 걸리는 시간(초).")]
        [SerializeField] private float _rampSeconds = 600f;

        [Header("스폰 수")]
        [SerializeField] private int _startBatch = 1;
        [SerializeField] private int _maxBatch = 8;

        private float _elapsed;
        private float _nextSpawnTime;

        // 이벤트별 다음 발동 시각. 인덱스 = WaveEventRegistry.All 순서
        private readonly System.Collections.Generic.List<float> _eventNext = new System.Collections.Generic.List<float>(8);

        /// <summary>경고 배너 요청. HUD 가 구독한다.</summary>
        public event System.Action<string> OnBanner;

        /// <summary>런 시작부터의 생존 시간. HUD 가 그대로 쓴다.</summary>
        public float Elapsed { get { return _elapsed; } }

        public bool Running { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BeginRun()
        {
            _elapsed = 0f;
            _nextSpawnTime = 0f;
            Running = true;

            _eventNext.Clear();
            System.Collections.Generic.IReadOnlyList<Jinhyeong_SkillSystem.WaveEventData> events = Jinhyeong_SkillSystem.WaveEventRegistry.All;
            for (int i = 0; i < events.Count; i++)
                _eventNext.Add(events[i].Time);
        }

        public void StopRun()
        {
            Running = false;
        }

        private void Update()
        {
            if (Running == false)
                return;

            // 생존 시간은 스폰 가능 여부와 무관하게 흐른다. HUD 가 이 값을 그대로 쓰므로
            // 스폰이 막혔다고 시계까지 멈추면 화면에 0:00 이 박힌다.
            float prev = _elapsed;
            _elapsed += Time.deltaTime;

            // 라운드 전환. 1분마다 한 번씩만 알린다.
            int prevRound = Jinhyeong_SkillSystem.RoundRegistry.RoundOf(prev);
            int nowRound = Jinhyeong_SkillSystem.RoundRegistry.RoundOf(_elapsed);
            if (nowRound != prevRound)
            {
                Jinhyeong_SkillSystem.RoundData rd = Jinhyeong_SkillSystem.RoundRegistry.Get(nowRound);
                string banner = rd != null && string.IsNullOrEmpty(rd.Banner) == false
                    ? rd.Banner : $"ROUND {nowRound}";
                if (OnBanner != null)
                    OnBanner.Invoke(banner);
            }

            EnemySpawner spawner = EnemySpawner.Instance;
            Player player = GameEvents.CurrentPlayer;
            if (spawner == null || player == null)
                return;
            if (_elapsed < _nextSpawnTime)
                return;

            TickEvents(spawner, player.transform.position);

            _nextSpawnTime = _elapsed + CurrentInterval();

            // 상한을 넘기면 스폰을 멈춘다. 넘겨봐야 프레임만 죽고 플레이어는 어차피 다 못 잡는다.
            int maxAlive = CurrentMaxAlive();
            int alive = spawner.Active.Count;
            if (alive >= maxAlive)
                return;

            int batch = Mathf.Min(CurrentBatch(), maxAlive - alive);
            SpawnRing(spawner, player.transform.position, batch);
        }

        /// <summary>
        /// 정예/보스 이벤트. 시각이 되면 현재 등장 가능한 타입에 배수를 곱해 스폰하고 배너를 띄운다.
        /// 상한(MaxAlive)을 무시한다 — 보스가 "자리 없어서" 안 나오면 안 된다.
        /// </summary>
        private void TickEvents(EnemySpawner spawner, Vector3 center)
        {
            System.Collections.Generic.IReadOnlyList<Jinhyeong_SkillSystem.WaveEventData> events = Jinhyeong_SkillSystem.WaveEventRegistry.All;
            for (int i = 0; i < events.Count && i < _eventNext.Count; i++)
            {
                if (_eventNext[i] < 0f || _elapsed < _eventNext[i])
                    continue;

                Jinhyeong_SkillSystem.WaveEventData ev = events[i];
                _eventNext[i] = ev.Repeat > 0f ? _eventNext[i] + ev.Repeat : -1f;

                Camera cam = CameraFollow.GameCamera;
                float baseAngle = Random.value * Mathf.PI * 2f;
                for (int n = 0; n < Mathf.Max(1, ev.Count); n++)
                {
                    float angle = baseAngle + (Mathf.PI * 2f / Mathf.Max(1, ev.Count)) * n;
                    Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    float r = cam != null ? EdgeDistance(cam, center, dir) + 2f : CommonConfig.SurvivorView.SpawnRingRadius;

                    // 타입이 지정된 이벤트는 그 적으로만 나온다. 보스가 매판 다른 몸으로 나오면
                    // 체력·속도가 통째로 달라져 보스전을 설계할 수 없다.
                    Jinhyeong_SkillSystem.EnemyTypeData type = ev.EnemyTypeId > 0
                        ? Jinhyeong_SkillSystem.EnemyTypeRegistry.Get(ev.EnemyTypeId)
                        : null;
                    if (type == null)
                        type = Jinhyeong_SkillSystem.EnemyTypeRegistry.PickWeighted(_elapsed);
                    Enemy e = spawner.Spawn(center + dir * r, type);
                    if (e == null)
                        continue;
                    e.ApplyEventMultipliers(ev.HpMul, ev.DamageMul, ev.ScaleMul, ev.XpMul, ev.SpeedMul, ev.IsBoss == false);
                    if (ev.IsBoss)
                    {
                        e.MarkAsBoss();
                        spawner.NotifyBossSpawned();
                    }
                }

                if (OnBanner != null && string.IsNullOrEmpty(ev.Banner) == false)
                    OnBanner.Invoke(ev.Banner);
            }
        }

        /// <summary>
        /// 스폰 간격은 라운드 테이블이 정한다. 여기 수식을 박아 두면 기획이 곡선을 못 만진다.
        /// 테이블이 비어 있을 때만 옛 램프로 떨어진다.
        /// </summary>
        private float CurrentInterval()
        {
            Jinhyeong_SkillSystem.RoundData r = Jinhyeong_SkillSystem.RoundRegistry.At(_elapsed);
            if (r != null && r.SpawnGap > 0f)
                return r.SpawnGap;

            float t = Mathf.Clamp01(_elapsed / Mathf.Max(1f, _rampSeconds));
            return Mathf.Lerp(_startInterval, _minInterval, t);
        }

        private int CurrentBatch()
        {
            Jinhyeong_SkillSystem.RoundData r = Jinhyeong_SkillSystem.RoundRegistry.At(_elapsed);
            if (r != null && r.SpawnBatch > 0)
                return r.SpawnBatch;

            float t = Mathf.Clamp01(_elapsed / Mathf.Max(1f, _rampSeconds));
            return Mathf.RoundToInt(Mathf.Lerp(_startBatch, _maxBatch, t));
        }

        /// <summary>동시 생존 상한도 라운드가 정한다. 상한이 곧 화면 밀도이자 프레임 예산이다.</summary>
        private int CurrentMaxAlive()
        {
            Jinhyeong_SkillSystem.RoundData r = Jinhyeong_SkillSystem.RoundRegistry.At(_elapsed);
            if (r != null && r.MaxAlive > 0)
                return r.MaxAlive;
            return CommonConfig.Spawner.MaxAlive;
        }

        /// <summary>현재 라운드 (1부터). HUD 가 쓴다.</summary>
        public int CurrentRound { get { return Jinhyeong_SkillSystem.RoundRegistry.RoundOf(_elapsed); } }

        /// <summary>
        /// 화면 경계 바로 바깥에 흩뿌린다.
        ///
        /// 고정 반지름 원을 쓰면 안 된다. 쿼터뷰에서 지면에 비치는 가시 영역은 **사다리꼴**이라
        /// 먼 쪽(화면 위)은 넓고 가까운 쪽(화면 아래)은 좁다. 원으로 뿌리면 위쪽에만 적이 몰리고
        /// 옆·아래는 한참 밖에서 생겨 걸어 들어오느라 비어 보인다.
        ///
        /// 그래서 방향마다 "화면을 벗어나는 거리"를 직접 찾아 그 바로 밖에 놓는다.
        /// 카메라 각도·거리·화면비가 바뀌어도 알아서 맞는다.
        /// </summary>
        private void SpawnRing(EnemySpawner spawner, Vector3 center, int count)
        {
            if (count <= 0)
                return;

            Camera cam = CameraFollow.GameCamera;
            float baseAngle = Random.value * Mathf.PI * 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle + (Mathf.PI * 2f / count) * i + Random.Range(-0.25f, 0.25f);
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                float r = cam != null
                    ? EdgeDistance(cam, center, dir) + Random.Range(1f, 3f)
                    : CommonConfig.SurvivorView.SpawnRingRadius;

                // 경과 시간에 따라 등장 가능한 타입을 가중치로 뽑는다. 타입 DB 가 비어 있으면 기본값.
                Jinhyeong_SkillSystem.EnemyTypeData type = Jinhyeong_SkillSystem.EnemyTypeRegistry.PickWeighted(_elapsed);
                spawner.Spawn(center + dir * r, type);
            }
        }

        /// <summary>
        /// center 에서 dir 방향으로 나아갈 때 화면을 벗어나는 거리를 찾는다.
        /// 1유닛씩 훑고 그 구간을 이분해서 좁힌다 — 정밀할 필요는 없고 화면 밖이기만 하면 된다.
        /// </summary>
        private static float EdgeDistance(Camera cam, Vector3 center, Vector3 dir)
        {
            const float Step = 1f;
            const float MaxDistance = 40f;

            float last = 0f;
            for (float d = Step; d <= MaxDistance; d += Step)
            {
                if (IsOffscreen(cam, center + dir * d))
                {
                    float lo = last;
                    float hi = d;
                    for (int it = 0; it < 5; it++)
                    {
                        float mid = (lo + hi) * 0.5f;
                        if (IsOffscreen(cam, center + dir * mid))
                            hi = mid;
                        else
                            lo = mid;
                    }
                    return hi;
                }
                last = d;
            }
            return CommonConfig.SurvivorView.SpawnRingRadius;
        }

        private static bool IsOffscreen(Camera cam, Vector3 worldPos)
        {
            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            return vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f;
        }
    }
}
