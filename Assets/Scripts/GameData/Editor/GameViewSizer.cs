using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{
    public static class GameViewSizer
    {
        private const string SizeName = "Jinhyeong Shot";

        private static bool _warned;

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

        private static void Warn(string reason)
        {
            if (_warned)
                return;
            _warned = true;
            Debug.LogWarning($"[GameViewSizer] 화면비를 맞추지 못했습니다 ({reason}) — 지금 크기 그대로 진행합니다");
        }
    }
}
