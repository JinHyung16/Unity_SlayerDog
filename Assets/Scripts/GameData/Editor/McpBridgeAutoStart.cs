using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    /// <summary>
    /// MCP For Unity 브리지가 에디터 실행 시 자동으로 뜨도록 EditorPref 를 켜둔다.
    ///
    /// 브리지가 떠 있어야 외부(Claude Code 등)에서 에디터를 직접 조작할 수 있다.
    /// 패키지 기본값이 꺼짐(HttpAutoStartHandler 가 GetBool(..., false))이라 매번 손으로 켜야 했다.
    ///
    /// 패키지 어셈블리를 참조하지 않고 EditorPrefs 키만 직접 쓴다 — 참조를 걸면
    /// 패키지를 빼는 순간 이 프로젝트가 컴파일되지 않는다.
    /// </summary>
    [InitializeOnLoad]
    public static class McpBridgeAutoStart
    {
        // MCPForUnity/Editor/Constants/EditorPrefKeys.cs 의 AutoStartOnLoad 와 같은 문자열
        private const string AutoStartKey = "MCPForUnity.AutoStartOnLoad";
        private const string AppliedKey = "Jinhyeong.McpBridgeAutoStart.Applied";

        static McpBridgeAutoStart()
        {
            // 한 번만 켠다. 사용자가 일부러 끈 걸 매 리로드마다 되돌리면 안 된다.
            if (EditorPrefs.GetBool(AppliedKey, false))
            {
                return;
            }
            EditorPrefs.SetBool(AppliedKey, true);

            if (EditorPrefs.GetBool(AutoStartKey, false))
            {
                return;
            }

            EditorPrefs.SetBool(AutoStartKey, true);
            Debug.Log("[McpBridgeAutoStart] MCP 브리지 자동 시작을 켰습니다. 다음 에디터 재시작부터 적용됩니다. " +
                      "지금 바로 띄우려면 Ctrl+Shift+M 으로 MCP 창을 열어 시작하세요.");
        }

        [MenuItem("Jinhyeong/GameData/MCP 브리지 자동시작 끄기", false, 40)]
        private static void Disable()
        {
            EditorPrefs.SetBool(AutoStartKey, false);
            EditorPrefs.SetBool(AppliedKey, true);
            Debug.Log("[McpBridgeAutoStart] MCP 브리지 자동 시작을 껐습니다.");
        }
    }
}
