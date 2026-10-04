using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;

namespace Jinhyeong_AI
{

    public class EnemyCullingSystem : BaseBehaviour
    {
        public static EnemyCullingSystem Instance { get; private set; }

        public static EnemyCullingSystem Ensure()
        {
            if (Instance != null)
                return Instance;
            GameObject go = new GameObject("_EnemyCullingSystem");
            return go.AddComponent<EnemyCullingSystem>();
        }

        [Tooltip("한 프레임에 검사할 적 수. 전부 매 프레임 보지 않고 나눠서 훑는다.")]
        [SerializeField] private int _checkPerFrame = 60;

        [Tooltip("화면 밖 적의 AI 틱 간격(초). 0이면 매 프레임.")]
        [SerializeField] private float _offscreenTickInterval = 0.2f;

        private readonly Dictionary<Enemy, EnemyCullState> _states = new Dictionary<Enemy, EnemyCullState>(256);
        private readonly List<Enemy> _scratch = new List<Enemy>(256);

        private int _cursor;

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

        public void Forget(Enemy enemy)
        {
            if (enemy == null)
                return;
            _states.Remove(enemy);
        }

        public void Clear()
        {
            _states.Clear();
            _cursor = 0;
        }

        private void LateUpdate()
        {
            EnemySpawner spawner = EnemySpawner.Instance;
            if (spawner == null)
                return;

            Camera cam = CameraFollow.GameCamera;
            if (cam == null)
                return;

            Player player = Jinhyeong_Managers.GameEvents.CurrentPlayer;
            if (player == null)
                return;

            Vector3 center = player.transform.position;
            float despawnSq = CommonConfig.SurvivorView.DespawnRadius * CommonConfig.SurvivorView.DespawnRadius;

            _scratch.Clear();
            _scratch.AddRange(spawner.Active);
            if (_scratch.Count == 0)
                return;

            int budget = Mathf.Min(_checkPerFrame, _scratch.Count);
            for (int n = 0; n < budget; n++)
            {
                if (_cursor >= _scratch.Count)
                    _cursor = 0;

                Enemy e = _scratch[_cursor++];
                if (e == null || e.gameObject.activeSelf == false)
                    continue;

                Vector3 pos = e.transform.position;

                bool visible = IsVisible(cam, pos);

                if (visible == false && e.IsBoss == false && PlanarDistSq(pos, center) > despawnSq)
                {
                    Forget(e);
                    spawner.Despawn(e);
                    continue;
                }

                Apply(e, visible);
            }
        }

        private void Apply(Enemy enemy, bool visible)
        {
            if (_states.TryGetValue(enemy, out EnemyCullState state) == false)
            {
                state = new EnemyCullState(enemy);
                _states[enemy] = state;
            }
            state.SetVisible(visible, _offscreenTickInterval);
        }

        private static bool IsVisible(Camera cam, Vector3 worldPos)
        {
            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            if (vp.z < 0f)
                return false;

            float m = CommonConfig.SurvivorView.OffscreenMargin * 0.05f;
            return vp.x > -m && vp.x < 1f + m && vp.y > -m && vp.y < 1f + m;
        }

        private static float PlanarDistSq(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
