using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    [InitializeOnLoad]
    public static class McpBridgeAutoStart
    {
        private const string AutoStartKey = "MCPForUnity.AutoStartOnLoad";
        private const string AppliedKey = "Jinhyeong.McpBridgeAutoStart.Applied";

        static McpBridgeAutoStart()
        {
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
