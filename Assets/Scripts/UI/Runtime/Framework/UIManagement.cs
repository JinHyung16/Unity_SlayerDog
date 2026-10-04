using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_UI.Framework
{

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

        public Transform UIRoot { get; private set; }

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

            Destroy(window.gameObject);

            BaseWindow last = _opened.Count > 0 ? _opened[_opened.Count - 1] : null;
            if (last != null)
            {
                last.ReOpened();
            }

            RecomputeVisibility();
            RebuildTickLists();
        }

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
