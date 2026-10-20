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

        public int BossSpawnCount { get; private set; }
        public int BossKillCount { get; private set; }

        public void NotifyBossSpawned()
        {
            BossSpawnCount++;
        }

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

            if (e.Damageable != null)
            {
                e.Damageable.OnDied -= HandleEnemyDied;
                e.Damageable.OnDied += HandleEnemyDied;
            }

            e.Init(position);
            e.ApplyType(type);

            Jinhyeong_SkillSystem.RoundData round =
                Jinhyeong_SkillSystem.RoundRegistry.At(Jinhyeong_AI.WaveDirector.Instance != null
                    ? Jinhyeong_AI.WaveDirector.Instance.Elapsed : 0f);
            // 맵 난이도는 라운드 배율 위에 곱한다 (Tables/map)
            Jinhyeong_SkillSystem.MapData map = Jinhyeong_SkillSystem.MapRegistry.Current;
            float mapHp = map != null && map.HpMul > 0f ? map.HpMul : 1f;
            float mapDmg = map != null && map.DamageMul > 0f ? map.DamageMul : 1f;
            if (round != null)
                e.ApplyRoundScaling(round.HpMul * mapHp, round.DamageMul * mapDmg);

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

            if (enemy != null && enemy.IsBoss)
            {
                BossKillCount++;
                Jinhyeong_UI.GameFlow.ClearGame();
            }

            float xp = enemy != null ? enemy.XpValue : CommonConfig.Run.XpPerGem;
            Jinhyeong_SkillSystem.XpGem.Drop(victim.transform.position, xp);

            float roll = Random.value;
            if (roll < CommonConfig.Run.MagnetDropChance)
                Jinhyeong_SkillSystem.FieldPickup.Drop(CommonConfig.Skill.MagnetKey, victim.transform.position);
            else if (roll < CommonConfig.Run.MagnetDropChance + CommonConfig.Run.MeatDropChance)
                Jinhyeong_SkillSystem.FieldPickup.Drop(CommonConfig.Skill.MeatKey, victim.transform.position);

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
