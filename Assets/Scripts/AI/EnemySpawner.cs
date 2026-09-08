using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_AI
{

    public class EnemySpawner : BaseBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        public static EnemySpawner Ensure()
        {
            if (Instance != null)
                return Instance;
            GameObject go = new GameObject("_EnemySpawner");
            return go.AddComponent<EnemySpawner>();
        }

        [Header("Pool")]
        [Tooltip("Enemy 프리팹의 Addressables 주소.")]
        [SerializeField] private string _addressableKey = "obj_enemy";
        [SerializeField] private Transform _poolRoot;
        [SerializeField] private Transform _spawnAroundTarget;

        private Enemy _prefab;

        private readonly Queue<Enemy> _pool = new Queue<Enemy>(16);
        private readonly List<Enemy> _active = new List<Enemy>(16);

        private bool _initialSpawnDone;
        private bool _prewarmDone;

        public IReadOnlyList<Enemy> Active { get { return _active; } }

        /// <summary>
        /// 이번 런에서 등장/처치한 보스 수.
        /// [사고] 리포트가 「끝난 시점에 살아 있는 보스」만 찾다 보니, 잡고 나면 「미등장」으로 찍혔다.
        /// 지나간 사건은 살아 있는 오브젝트가 아니라 **카운터**로 남겨야 한다.
        /// </summary>
        public int BossSpawnCount { get; private set; }
        public int BossKillCount { get; private set; }

        public void NotifyBossSpawned()
        {
            BossSpawnCount++;
        }

        /// <summary>이번 런에서 죽인 적 수. HUD 가 그대로 읽는다. Clear 에서 리셋된다.</summary>
        public int KillCount { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (_poolRoot == null)
                _poolRoot = transform;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        protected override void OnEnabled()
        {
            if (Instance != this)
                return;
            GameEvents.OnPlayerSpawned += HandlePlayerSpawned;
            GameEvents.OnPlayerDespawned += HandlePlayerDespawned;
            if (GameEvents.CurrentPlayer != null)
                HandlePlayerSpawned(GameEvents.CurrentPlayer);
        }

        protected override void OnDisabled()
        {
            GameEvents.OnPlayerSpawned -= HandlePlayerSpawned;
            GameEvents.OnPlayerDespawned -= HandlePlayerDespawned;
        }

        private void Start()
        {
            if (Instance != this)
                return;
            TryPrewarm();
            TryInitialSpawn();
        }

        private void HandlePlayerSpawned(Player p)
        {
            if (p == null)
                return;
            if (_spawnAroundTarget == null)
                _spawnAroundTarget = p.transform;
            TryPrewarm();
            TryInitialSpawn();
        }

        /// <summary>
        /// 풀 예열은 프리팹이 캐시에 오른 **뒤에만** 한다.
        /// [사고] Start 에서 바로 예열했더니 GameInitializer 가 이 컴포넌트를 세우는 시점(씬 로드 직후)에는
        /// 아직 아무도 obj_enemy 를 로드하지 않아 매 부팅마다 「캐시에 없음」 에러가 한 번씩 찍혔다.
        /// 플레이어 스폰(WorldSpawner 가 사전로드를 끝낸 뒤)에서 다시 시도하면 그 자리에서 채워진다.
        /// </summary>
        private void TryPrewarm()
        {
            if (_prewarmDone)
                return;
            if (IsPrefabReady() == false)
                return;
            _prewarmDone = true;
            Prewarm();
        }

        private bool IsPrefabReady()
        {
            if (_prefab != null)
                return true;
            AddressableManager am = AddressableManager.Instance;
            return am != null && am.IsLoaded(_addressableKey);
        }

        private void HandlePlayerDespawned(Player p)
        {
            if (p != null && _spawnAroundTarget == p.transform)
            {
                _spawnAroundTarget = null;
            }
        }

        private void Prewarm()
        {
            for (int i = 0; i < CommonConfig.Spawner.PrewarmCount; i++)
            {
                Enemy e = CreateInstance();
                if (e == null)
                    return;
                e.gameObject.SetActive(false);
                _pool.Enqueue(e);
            }
        }

        private void TryInitialSpawn()
        {
            if (_initialSpawnDone)
                return;
            if (_spawnAroundTarget == null)
                return;
            // 프리팹이 아직 없으면 6번 실패하고 「끝났다」고 표시하던 자리다. 조용히 다음 기회를 기다린다.
            if (IsPrefabReady() == false)
                return;

            Vector3 center = _spawnAroundTarget.position;
            int count = CommonConfig.Spawner.InitialEnemyCount;
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / Mathf.Max(1, count)) * i * Mathf.Deg2Rad;
                float radius = CommonConfig.Spawner.InitialSpawnRadius + Random.value * 2f;
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Spawn(pos);
            }
            _initialSpawnDone = true;
        }

        public Enemy Spawn(Vector3 position)
        {
            return Spawn(position, null);
        }

        public Enemy Spawn(Vector3 position, Jinhyeong_SkillSystem.EnemyTypeData type)
        {
            Enemy e;
            if (_pool.Count > 0)
            {
                e = _pool.Dequeue();
                if (e == null)
                    return Spawn(position);
                e.gameObject.SetActive(true);
            }
            else
            {
                e = CreateInstance();
                if (e == null)
                    return null;
            }

            e.OnDespawnRequested -= HandleEnemyDespawnRequested;
            e.OnDespawnRequested += HandleEnemyDespawnRequested;

            // 킬 집계. 풀 재사용 시 중복 구독을 막으려고 빼고 다시 건다 (OnDespawnRequested 와 같은 패턴)
            if (e.Damageable != null)
            {
                e.Damageable.OnDied -= HandleEnemyDied;
                e.Damageable.OnDied += HandleEnemyDied;
            }

            e.Init(position);
            e.ApplyType(type);

            // 라운드 배수. ApplyType 이 기본값을 넣은 **뒤에** 곱해야 한다 —
            // 순서를 바꾸면 ApplyType 이 배수를 덮어써서 라운드가 아무 효과도 없다.
            Jinhyeong_SkillSystem.RoundData round =
                Jinhyeong_SkillSystem.RoundRegistry.At(Jinhyeong_AI.WaveDirector.Instance != null
                    ? Jinhyeong_AI.WaveDirector.Instance.Elapsed : 0f);
            if (round != null)
                e.ApplyRoundScaling(round.HpMul, round.DamageMul);

            _active.Add(e);
            return e;
        }

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Enemy e = _active[i];
                if (e == null)
                    continue;
                e.OnDespawnRequested -= HandleEnemyDespawnRequested;
                if (e.gameObject != null)
                    Destroy(e.gameObject);
            }
            _active.Clear();

            while (_pool.Count > 0)
            {
                Enemy e = _pool.Dequeue();
                if (e != null && e.gameObject != null)
                    Destroy(e.gameObject);
            }

            if (EnemyCullingSystem.Instance != null)
                EnemyCullingSystem.Instance.Clear();

            _initialSpawnDone = false;
            _spawnAroundTarget = null;
            _prefab = null;
            KillCount = 0;
            BossSpawnCount = 0;
            BossKillCount = 0;
        }

        public void Despawn(Enemy enemy)
        {
            if (enemy == null)
                return;
            enemy.OnDespawnRequested -= HandleEnemyDespawnRequested;

            if (EnemyCullingSystem.Instance != null)
                EnemyCullingSystem.Instance.Forget(enemy);

            _active.Remove(enemy);
            enemy.gameObject.SetActive(false);
            enemy.transform.SetParent(_poolRoot, false);
            _pool.Enqueue(enemy);
        }

        private void HandleEnemyDespawnRequested(Enemy e)
        {
            Despawn(e);
        }

        private void HandleEnemyDied(Jinhyeong_SkillSystem.Damageable victim, Jinhyeong_SkillSystem.SkillObject source)
        {
            KillCount++;
            Jinhyeong_Managers.SfxManager.Play(Jinhyeong_Managers.ESfx.Kill, 0.5f);
            if (victim == null)
                return;

            Enemy enemy = victim.GetComponent<Enemy>();

            // 보스 처치 = 클리어. 12라운드까지 버티는 것 자체가 목표라, 여기가 런의 결승선이다.
            if (enemy != null && enemy.IsBoss)
            {
                BossKillCount++;
                Jinhyeong_UI.GameFlow.ClearGame();
            }

            float xp = enemy != null ? enemy.XpValue : CommonConfig.Run.XpPerGem;
            Jinhyeong_SkillSystem.XpGem.Drop(victim.transform.position, xp);

            // 처치 회복. 「많이 죽일수록 산다」 — 공격 성장이 생존으로 이어지는 유일한 통로다.
            Jinhyeong_Character.Player player = Jinhyeong_Managers.GameEvents.CurrentPlayer;
            if (player == null)
                return;
            Jinhyeong_Parts.PartLoadout parts = player.GetComponent<Jinhyeong_Parts.PartLoadout>();
            if (parts != null)
                parts.GrantLifesteal();
        }

        private Enemy CreateInstance()
        {
            Enemy prefab = ResolvePrefab();
            if (prefab == null)
                return null;
            return Instantiate(prefab, _poolRoot, false);
        }

        private Enemy ResolvePrefab()
        {
            if (_prefab != null)
                return _prefab;

            AddressableManager am = AddressableManager.Instance;
            GameObject go = am != null ? am.Get(_addressableKey) : null;
            if (go == null)
            {
                Debug.LogError($"[EnemySpawner] '{_addressableKey}' addressable이 캐시에 없음 — 사전로드(WorldSpawner) 누락 또는 그룹 미등록", this);
                return null;
            }

            _prefab = go.GetComponent<Enemy>();
            if (_prefab == null)
            {
                Debug.LogError($"[EnemySpawner] '{_addressableKey}' prefab에 Enemy 컴포넌트 없음", this);
            }
            return _prefab;
        }
    }
}
