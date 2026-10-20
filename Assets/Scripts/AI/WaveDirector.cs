using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_AI
{

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

        private readonly System.Collections.Generic.List<float> _eventNext = new System.Collections.Generic.List<float>(8);

        public event System.Action<string> OnBanner;

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

            float prev = _elapsed;
            _elapsed += Time.deltaTime;

            int prevRound = Jinhyeong_SkillSystem.RoundRegistry.RoundOf(prev);
            int nowRound = Jinhyeong_SkillSystem.RoundRegistry.RoundOf(_elapsed);
            if (nowRound != prevRound)
            {
                Jinhyeong_SkillSystem.RoundData rd = Jinhyeong_SkillSystem.RoundRegistry.Get(nowRound);
                string banner = rd != null && string.IsNullOrEmpty(rd.Banner) == false
                    ? rd.Banner : Loc.F("Wave_Round", "라운드 {0}", nowRound);
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

            int maxAlive = CurrentMaxAlive();
            int alive = spawner.Active.Count;
            if (alive >= maxAlive)
                return;

            int batch = Mathf.Min(CurrentBatch(), maxAlive - alive);
            SpawnRing(spawner, player.transform.position, batch);
        }

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

                // 보스 배너는 맵 보스 이름 (Tables/map boss)
                string banner = ev.Banner;
                if (ev.IsBoss)
                {
                    Jinhyeong_SkillSystem.MapData map = Jinhyeong_SkillSystem.MapRegistry.Current;
                    if (map != null && string.IsNullOrEmpty(map.Boss) == false)
                        banner = Loc.F("Wave_BossAppears", "{0} 출현!", map.Boss);
                }
                if (OnBanner != null && string.IsNullOrEmpty(banner) == false)
                    OnBanner.Invoke(banner);
            }
        }

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

        private int CurrentMaxAlive()
        {
            Jinhyeong_SkillSystem.RoundData r = Jinhyeong_SkillSystem.RoundRegistry.At(_elapsed);
            if (r != null && r.MaxAlive > 0)
                return r.MaxAlive;
            return CommonConfig.Spawner.MaxAlive;
        }

        public int CurrentRound { get { return Jinhyeong_SkillSystem.RoundRegistry.RoundOf(_elapsed); } }

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

                Jinhyeong_SkillSystem.EnemyTypeData type = Jinhyeong_SkillSystem.EnemyTypeRegistry.PickWeighted(_elapsed);
                spawner.Spawn(center + dir * r, type);
            }
        }

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
