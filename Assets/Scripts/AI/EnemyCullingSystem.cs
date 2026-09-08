using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;

namespace Jinhyeong_AI
{

    /// <summary>
    /// 화면 밖 적의 비용을 죽인다. 몬스터가 수백 마리 깔리는 게임이라 이게 없으면 프레임이 안 나온다.
    ///
    /// Unity 는 렌더러를 알아서 프러스텀 컬링하지만, **Update·AI·Animator 는 계속 돈다.**
    /// 진짜 비용은 거기 있으므로 직접 끈다:
    ///   1. 화면 밖  → Animator 정지 + AI 틱 간격 늘림 (완전히 멈추면 적이 다가오질 않는다)
    ///   2. 너무 멀리 → 풀로 회수 (뒤에 처진 적을 계속 굴릴 이유가 없다)
    ///
    /// 적마다 컴포넌트를 붙이지 않고 스포너의 활성 목록을 한 곳에서 훑는다.
    /// 컴포넌트 수백 개의 Update 를 도는 것보다 루프 하나가 싸다.
    /// </summary>
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

            // 전부 매 프레임 검사하지 않고 커서를 굴리며 나눠 본다
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

                // 회수는 "화면 밖 + 너무 멂" 두 조건이 모두 맞을 때만 한다.
                // 거리만 보면 쿼터뷰의 먼 쪽(화면 위)에 보이는 적을 눈앞에서 지워버린다.
                // [사고] 보스가 플레이어보다 느려 뒤처지면 화면 밖으로 밀려 **풀로 회수**됐다.
                // 등장은 했는데 죽지도 살아 있지도 않아, 12라운드 클리어가 원천적으로 불가능했다.
                // 하나뿐인 개체는 비용을 아낄 대상이 아니다.
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

        /// <summary>
        /// 뷰포트 좌표로 판정한다. 여유(margin)를 두는 이유는 화면 경계에서 켜졌다 꺼졌다 하면
        /// 가장자리 적이 눈에 띄게 버벅이기 때문이다.
        /// </summary>
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
