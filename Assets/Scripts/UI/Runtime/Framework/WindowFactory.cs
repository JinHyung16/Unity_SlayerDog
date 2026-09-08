using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// 키로 창 프리팹을 올리고 인스턴스를 만든다. 열지는 않는다 — 여는 것은 창의 <c>Open(...)</c> 이다.
    ///
    /// 캔버스 설정은 **프리팹에 저장된 값을 믿지 않는다.** 창을 여는 코드가 정한다 (PrefabRule.md).
    /// CanvasScaler 값은 프로젝트당 하나(<see cref="CommonConfig.UI"/>) — 창마다 다르게 두면 같은 UI 가 기기마다 다르게 밀린다.
    /// </summary>
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
            GameObject prefab = await am.LoadAsync(key.Path);
            if (prefab == null)
            {
                Debug.LogError($"[WindowFactory] '{key.Path}' 를 로드하지 못했다 — 어드레서블 등록을 확인하라 (UIPrefabBuilder)");
                return null;
            }

            // 비활성으로 세운다. 열기 전에 Awake 가 돌아도 부품 배선만 하지 데이터는 안 읽는다.
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

            // 창이 쓰는 조각을 먼저 다 올린다. 이후 UILoader.Get 은 동기다.
            IReadOnlyList<string> preload = window.PreloadPaths;
            if (preload != null && preload.Count > 0)
            {
                await am.LoadAllAsync(preload);
            }

            ConfigureCanvas(window);
            return window;
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
