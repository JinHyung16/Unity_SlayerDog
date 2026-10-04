using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_UI.Framework
{

    public static class WindowFactory
    {
        public static async UniTask<T> LoadAsync<T>(WindowKey<T> key, Transform parent) where T : BaseWindow
        {
            if (key == null || string.IsNullOrEmpty(key.Path))
            {
                Debug.LogError($"[WindowFactory] {typeof(T).Name} 의 키가 비어 있다");
                return null;
            }

            AddressableManager am = AddressableManager.Ensure();

            // 씬에 미리 배치된 창이 있으면 그것을 쓴다 (에디터에서 처음부터 보이게 — 10-04 사용자 지시).
            // 이미 열려 있는 것은 건드리지 않는다. 없으면 예전처럼 어드레서블에서 만든다
            T placed = FindPlaced<T>();
            if (placed != null)
            {
                placed.gameObject.SetActive(false);
                IReadOnlyList<string> placedPreload = placed.PreloadPaths;
                if (placedPreload != null && placedPreload.Count > 0)
                {
                    await am.LoadAllAsync(placedPreload);
                }
                ConfigureCanvas(placed);
                return placed;
            }

            GameObject prefab = await am.LoadAsync(key.Path);
            if (prefab == null)
            {
                Debug.LogError($"[WindowFactory] '{key.Path}' 를 로드하지 못했다 — 어드레서블 등록을 확인하라 (UIPrefabBuilder)");
                return null;
            }

            GameObject go = Object.Instantiate(prefab, parent, false);
            go.name = prefab.name;
            go.SetActive(false);

            T window = go.GetComponent<T>();
            if (window == null)
            {
                Debug.LogError($"[WindowFactory] '{key.Path}' 루트에 {typeof(T).Name} 이 없다");
                Object.Destroy(go);
                return null;
            }

            IReadOnlyList<string> preload = window.PreloadPaths;
            if (preload != null && preload.Count > 0)
            {
                await am.LoadAllAsync(preload);
            }

            ConfigureCanvas(window);
            return window;
        }

        private static T FindPlaced<T>() where T : BaseWindow
        {
            T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            UIManagement ui = UIManagement.Instance;
            for (int i = 0; i < found.Length; i++)
            {
                T w = found[i];
                if (w == null || w.gameObject.scene.IsValid() == false)
                {
                    continue;
                }
                if (ui != null && ui.PoolRoot != null && w.transform.IsChildOf(ui.PoolRoot))
                {
                    continue;
                }
                if (w.WindowState != EWindowState.Closed)
                {
                    continue;
                }
                return w;
            }
            return null;
        }

        private static void ConfigureCanvas(BaseWindow window)
        {
            Canvas canvas = window.Canvas;
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            CanvasScaler scaler = window.Scaler;
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = CommonConfig.UI.ReferenceResolution;
                scaler.matchWidthOrHeight = CommonConfig.UI.MatchWidthOrHeight;
            }
        }
    }
}
