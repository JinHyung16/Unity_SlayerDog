using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem.BT;

namespace Jinhyeong_GameData.Editor
{

    /// <summary>
    /// 스킬 노드 어휘 검사 — 「enum 에 있는데 런타임이 안 받는 노드」를 찾아 익스포터에 알린다.
    ///
    /// [사고] 이 목록을 `_DataExporter/config.json` 에 **사람이 손으로** 적어 두고 있었다.
    /// 구현을 해도 그 줄을 안 지우면 익스포터가 계속 막고, 반대로 구현을 안 했는데 지우면
    /// 데이터가 통과한 뒤 런타임에 조용히 아무 일도 안 한다. 어느 쪽이든 사람이 틀리는 구조였다.
    ///
    /// 이제 진실은 C# 한 곳이다 — <see cref="BTNodeRegistry"/> 가 실제 등록된 것을 세고,
    /// 이 도구가 그 결과를 config.json 에 적어 준다. 손으로 맞출 것이 없다.
    /// </summary>
    public static class SkillNodeCheck
    {
        private const string ConfigPath = "_DataExporter/config.json";

        [MenuItem("Jinhyeong/GameData/스킬 노드 어휘 검사", false, 11)]
        public static void Run()
        {
            string report = BTNodeRegistry.Report();
            bool changed = SyncConfig(out string note);

            Debug.Log(report + note);

            if (changed)
                AssetDatabase.Refresh();
        }

        /// <summary>배치모드에서 부르는 진입점. 미구현이 config 와 어긋나면 알린다.</summary>
        public static void RunBatch()
        {
            SyncConfig(out string note);
            Debug.Log(BTNodeRegistry.Report() + note);
        }

        /// <summary>
        /// config.json 의 UnimplementedNodes 를 실제 등록 현황으로 덮어쓴다.
        /// JSON 전체를 파싱하지 않고 그 배열만 갈아 끼운다 — 나머지 설정을 건드릴 이유가 없고,
        /// 통째로 다시 쓰면 주석 키(`_UnimplementedNodes`)나 키 순서가 흐트러진다.
        /// </summary>
        private static bool SyncConfig(out string note)
        {
            note = string.Empty;

            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ConfigPath);
            if (File.Exists(path) == false)
            {
                note = $"\n  config.json 을 찾지 못했다: {path}";
                return false;
            }

            IReadOnlyList<ESkillNodeType> missing = BTNodeRegistry.Unimplemented;

            StringBuilder listBuilder = new StringBuilder();
            listBuilder.Append("[");
            for (int i = 0; i < missing.Count; i++)
            {
                if (i > 0)
                    listBuilder.Append(",");
                listBuilder.Append("\n    \"").Append(missing[i]).Append("\"");
            }
            if (missing.Count > 0)
                listBuilder.Append("\n  ");
            listBuilder.Append("]");
            string replacement = "\"UnimplementedNodes\": " + listBuilder;

            string text = File.ReadAllText(path);
            int start = text.IndexOf("\"UnimplementedNodes\"", System.StringComparison.Ordinal);
            if (start < 0)
            {
                note = "\n  config.json 에 UnimplementedNodes 키가 없다 — 건드리지 않는다";
                return false;
            }
            int close = text.IndexOf(']', start);
            if (close < 0)
            {
                note = "\n  UnimplementedNodes 배열이 닫히지 않았다 — 건드리지 않는다";
                return false;
            }

            string before = text.Substring(start, close - start + 1);
            if (before == replacement)
            {
                note = "\n  config.json 과 일치한다";
                return false;
            }

            File.WriteAllText(path, text.Substring(0, start) + replacement + text.Substring(close + 1));
            note = $"\n  config.json 갱신 — 미구현 {missing.Count}종을 익스포터에 알렸다";
            return true;
        }
    }
}
