using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// 창의 진행을 맡는 Management (Client.md 「Manager 와 Management」). 씬 수명이다 — 창은 씬과 함께 죽는다.
    ///
    /// 하는 일 셋:
    ///   1. 키로 창을 올려 <c>UIRoot</c> 아래에 세운다 (<see cref="OpenAsync{T}"/>)
    ///   2. 깊이 — 대역 최소값 + 여는 순서×간격. 창은 자기 숫자를 정하지 않는다
    ///   3. 전체 크기 창이 열리면 **아래 창의 캔버스를 끈다**. 안 끄면 안 보이는 화면이 계속 리빌드된다 —
    ///      UI 성능의 가장 큰 단일 항목이다
    /// 그리고 중앙 틱 — 창마다 <c>Update()</c> 를 갖지 않고, 열린 창만 돌며 옵트인 인터페이스를 부른다.
    /// </summary>
    public class UIManagement : BaseBehaviour
    {
        private const string UIRootName = "UIRoot";
        private const float PerSecTick = 1f;

        private sealed class DepthInfo
        {
            public int Min;
            public int Interval;
            public int Count;

            public int Next
            {
                get { return Min + Count * Interval; }
            }
        }

        public static UIManagement Instance { get; private set; }

        /// <summary>창이 세워지는 자리. 씬에 있으면 쓰고 없으면 만든다.</summary>
        public Transform UIRoot { get; private set; }

        /// <summary>풀에 돌아간 조각이 대기하는 자리. 비활성.</summary>
        public Transform PoolRoot { get; private set; }

        private readonly Dictionary<EWindowType, DepthInfo> _depth = new Dictionary<EWindowType, DepthInfo>(10);
        private readonly List<BaseWindow> _opened = new List<BaseWindow>(8);

        private readonly List<IBaseWindowUpdate> _updates = new List<IBaseWindowUpdate>(4);
        private readonly List<IBaseWindowUpdatePerSec> _updatesPerSec = new List<IBaseWindowUpdatePerSec>(4);
        private readonly List<IBaseWindowLateUpdate> _lateUpdates = new List<IBaseWindowLateUpdate>(4);
        private float _perSecElapsed;

        public static UIManagement Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }
            GameObject go = new GameObject("_UIManagement");
            return go.AddComponent<UIManagement>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            GameObject root = GameObject.Find(UIRootName);
            if (root == null)
            {
                root = new GameObject(UIRootName);
            }
            UIRoot = root.transform;

            GameObject pool = new GameObject("_UIPool");
            pool.transform.SetParent(transform, false);
            pool.SetActive(false);
            PoolRoot = pool.transform;

            foreach (EWindowType type in System.Enum.GetValues(typeof(EWindowType)))
            {
                _depth[type] = new DepthInfo { Min = WindowDepth.Min(type), Interval = WindowDepth.Interval(type) };
            }
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }
            // 정적 풀이 이전 씬의 인스턴스를 물고 있으면 누수다. 씬을 떠날 때 비운다.
            UILoader.ClearAll();
            for (int i = 0; i < _opened.Count; i++)
            {
                if (_opened[i] != null)
                {
                    _opened[i].OnStateChanged -= HandleStateChanged;
                }
            }
            _opened.Clear();
            Instance = null;
        }

        /// <summary>
        /// 창을 올려 세운다. **열지는 않는다** — 돌려받은 창의 <c>Open(...)</c> 을 호출자가 부른다.
        /// 열리면 상태 이벤트로 깊이와 뒤 창 표시가 따라온다.
        /// </summary>
        public async UniTask<T> OpenAsync<T>(WindowKey<T> key) where T : BaseWindow
        {
            T window = await WindowFactory.LoadAsync(key, UIRoot);
            if (window == null)
            {
                return null;
            }
            window.OnStateChanged -= HandleStateChanged;
            window.OnStateChanged += HandleStateChanged;
            return window;
        }

        public T Find<T>() where T : BaseWindow
        {
            for (int i = 0; i < _opened.Count; i++)
            {
                T typed = _opened[i] as T;
                if (typed != null)
                {
                    return typed;
                }
            }
            return null;
        }

        public bool IsOpen<T>() where T : BaseWindow
        {
            return Find<T>() != null;
        }

        /// <summary>위에서부터 전부 강제로 닫는다. 씬 전환 직전 등.</summary>
        public void CloseAll()
        {
            for (int i = _opened.Count - 1; i >= 0; i--)
            {
                BaseWindow w = _opened[i];
                if (w != null)
                {
                    w.ForcedClose();
                }
            }
        }

        private void HandleStateChanged(BaseWindow window)
        {
            if (window.WindowState == EWindowState.Opened)
            {
                HandleOpenWindow(window);
            }
            else if (window.WindowState == EWindowState.Closed)
            {
                HandleCloseWindow(window);
            }
        }

        private void HandleOpenWindow(BaseWindow window)
        {
            if (_opened.Contains(window))
            {
                return;
            }

            if (window.WindowType == EWindowType.ConstantDepth)
            {
                window.SetDepth(window.ConstantDepth);
            }
            else if (_depth.TryGetValue(window.WindowType, out DepthInfo info))
            {
                window.SetDepth(info.Next);
                info.Count++;
            }

            BaseWindow prev = _opened.Count > 0 ? _opened[_opened.Count - 1] : null;
            if (prev != null)
            {
                prev.OtherWindowOpened();
            }

            _opened.Add(window);
            RecomputeVisibility();
            RebuildTickLists();
        }

        private void HandleCloseWindow(BaseWindow window)
        {
            if (_opened.Remove(window) == false)
            {
                return;
            }

            if (window.WindowType != EWindowType.ConstantDepth
                && _depth.TryGetValue(window.WindowType, out DepthInfo info)
                && info.Count > 0)
            {
                info.Count--;
            }

            window.OnStateChanged -= HandleStateChanged;

            // 창 풀은 아직 없다 — 닫으면 지운다. 로비처럼 씬당 한 번 여는 창에는 이걸로 충분하고,
            // 자주 여닫는 창이 생기면 그때 UILoader 와 같은 풀을 창에도 붙인다.
            Destroy(window.gameObject);

            BaseWindow last = _opened.Count > 0 ? _opened[_opened.Count - 1] : null;
            if (last != null)
            {
                last.ReOpened();
            }

            RecomputeVisibility();
            RebuildTickLists();
        }

        /// <summary>
        /// 위에서 내려오며, 전체 크기 창에 덮인 아래 창의 캔버스를 끈다.
        /// 여닫을 때마다 전체를 다시 계산한다 — 「누가 누구를 껐는지」를 기억하는 것보다 틀릴 데가 없다.
        /// </summary>
        private void RecomputeVisibility()
        {
            bool covered = false;
            for (int i = _opened.Count - 1; i >= 0; i--)
            {
                BaseWindow w = _opened[i];
                if (w == null)
                {
                    continue;
                }
                w.CanvasEnable = covered == false;
                if (w.IsFullSizeWindow && WindowDepth.CoversBelow(w.WindowType))
                {
                    covered = true;
                }
            }
        }

        private void RebuildTickLists()
        {
            _updates.Clear();
            _updatesPerSec.Clear();
            _lateUpdates.Clear();
            for (int i = 0; i < _opened.Count; i++)
            {
                BaseWindow w = _opened[i];
                if (w is IBaseWindowUpdate u)
                {
                    _updates.Add(u);
                }
                if (w is IBaseWindowUpdatePerSec ps)
                {
                    _updatesPerSec.Add(ps);
                }
                if (w is IBaseWindowLateUpdate lu)
                {
                    _lateUpdates.Add(lu);
                }
            }
        }

        private void Update()
        {
            for (int i = 0; i < _updates.Count; i++)
            {
                _updates[i].OnUpdate();
            }

            if (_updatesPerSec.Count == 0)
            {
                return;
            }
            _perSecElapsed += Time.unscaledDeltaTime;
            if (_perSecElapsed < PerSecTick)
            {
                return;
            }
            _perSecElapsed = 0f;
            for (int i = 0; i < _updatesPerSec.Count; i++)
            {
                _updatesPerSec[i].OnUpdatePerSec();
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < _lateUpdates.Count; i++)
            {
                _lateUpdates[i].OnLateUpdate();
            }
        }
    }
}
