using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem.BT;

namespace Jinhyeong_GameData.Editor
{

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

        public static void RunBatch()
        {
            SyncConfig(out string note);
            Debug.Log(BTNodeRegistry.Report() + note);
        }

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
