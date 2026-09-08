using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{
    /// <summary>
    /// Game 뷰를 타깃 고정 해상도로 맞춘다.
    ///
    /// [사고] 하네스 스크린샷 크기가 에디터 창을 따라가(520x273) 타깃과 달랐다.
    /// **틀린 화면비로 찍은 그림을 보고 UI 를 고치면** 고칠 때마다 다른 곳이 깨진다 —
    /// 로비 레이아웃을 세 번 고쳤는데 매번 다시 겹쳤던 이유가 이것이었다.
    ///
    /// [사고 2] 그걸 고치면서 이번엔 **세로 모바일로 오해**해 9:16 으로 맞췄다.
    /// 이 게임은 PC 다. 전제가 틀리면 도구를 고쳐도 방향만 반대로 틀린다 —
    /// 타깃 플랫폼은 추측하지 말고 확인한다. 지금 기준은 **1280x720 가로**다.
    ///
    /// Game 뷰 크기는 공개 API 가 없어 내부 타입을 리플렉션으로 만진다. 버전이 바뀌면 깨질 수 있으므로
    /// **실패해도 측정은 계속되게** 한다 — 화면비가 틀린 스크린샷이 측정 자체를 막을 이유는 없다.
    /// </summary>
    public static class GameViewSizer
    {
        /// <summary>Game 뷰 크기 목록에 추가할 항목 이름. 매번 새로 만들지 않으려고 이름으로 찾는다.</summary>
        private const string SizeName = "Jinhyeong Shot";

        private static bool _warned;

        /// <summary>지정 고정 해상도로 맞춘다. 이미 맞아 있으면 아무것도 하지 않는다.</summary>
        public static void SetShotSize(int width, int height)
        {
            try
            {
                Assembly editorAsm = typeof(UnityEditor.Editor).Assembly;

                Type sizesType = editorAsm.GetType("UnityEditor.GameViewSizes");
                Type sizeType = editorAsm.GetType("UnityEditor.GameViewSize");
                Type sizeTypeEnum = editorAsm.GetType("UnityEditor.GameViewSizeType");
                Type groupType = editorAsm.GetType("UnityEditor.GameViewSizeGroup");
                Type gameViewType = editorAsm.GetType("UnityEditor.GameView");
                if (sizesType == null || sizeType == null || sizeTypeEnum == null
                    || groupType == null || gameViewType == null)
                {
                    Warn("내부 타입을 찾지 못했습니다");
                    return;
                }

                Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                object sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)
                    .GetValue(null);

                object group = sizesType.GetMethod("GetGroup")
                    .Invoke(sizes, new object[] { (int)GameViewSizeGroupType.Standalone });

                MethodInfo getCount = groupType.GetMethod("GetTotalCount");
                MethodInfo getSize = groupType.GetMethod("GetGameViewSize");

                int count = (int)getCount.Invoke(group, null);
                int index = -1;
                for (int i = 0; i < count; i++)
                {
                    object s = getSize.Invoke(group, new object[] { i });
                    string name = (string)sizeType.GetProperty("baseText").GetValue(s);
                    if (name == SizeName)
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0)
                {
                    // FixedResolution 이어야 Game 뷰가 창 크기와 무관하게 그 해상도로 렌더한다.
                    object fixedRes = Enum.Parse(sizeTypeEnum, "FixedResolution");
                    ConstructorInfo ctor = sizeType.GetConstructor(
                        new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
                    object made = ctor.Invoke(new object[] { fixedRes, width, height, SizeName });
                    groupType.GetMethod("AddCustomSize").Invoke(group, new[] { made });
                    index = (int)getCount.Invoke(group, null) - 1;
                }

                EditorWindow view = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (view == null)
                {
                    Warn("Game 뷰 창을 찾지 못했습니다");
                    return;
                }

                MethodInfo select = gameViewType.GetMethod("SizeSelectionCallback",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (select == null)
                {
                    Warn("크기 선택 메서드를 찾지 못했습니다");
                    return;
                }
                select.Invoke(view, new object[] { index, null });
                view.Repaint();
            }
            catch (Exception e)
            {
                Warn(e.Message);
            }
        }

        /// <summary>같은 경고를 매 런 반복하지 않는다 — 리포트가 이걸로 덮이면 진짜 문제가 안 보인다.</summary>
        private static void Warn(string reason)
        {
            if (_warned)
                return;
            _warned = true;
            Debug.LogWarning($"[GameViewSizer] 화면비를 맞추지 못했습니다 ({reason}) — 지금 크기 그대로 진행합니다");
        }
    }
}
