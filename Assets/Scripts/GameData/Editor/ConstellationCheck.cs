using System.Collections.Generic;
using System.IO;
using System.Text;
using Jinhyeong_Common;
using Jinhyeong_Constellation;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    public static class ConstellationCheck
    {
        private const float DnaPerRunEarly = 6f;
        private const float DnaPerRunLate = 30f;

        public static string Run()
        {
            StringBuilder sb = new StringBuilder(4096);

            PartRegistry.Load();
            ConstellationRegionRegistry.Load();

            ConstellationGraph graph = new ConstellationGraph();
            bool baked = ConstellationGraphIO.TryLoadBaked(graph);
            if (baked == false)
            {
                ConstellationGenerator.Build(graph);
            }

            sb.AppendLine("=== 성좌 실측 ===");
            sb.AppendLine(baked
                ? "출처: 구워진 그래프 (손으로 만든 것이 시드보다 우선)"
                : "출처: 시드 생성 (아직 저장된 그래프가 없다)");
            sb.AppendLine($"계열 {ConstellationRegionRegistry.Count}개 · 노드 {graph.Nodes.Count}개 · 간선 {graph.EdgeCount}개");

            AppendShape(sb, graph);
            AppendCost(sb, graph);
            AppendValidation(sb, graph);
            AppendRoundTrip(sb, graph);

            return sb.ToString();
        }

        public static string SaveDraft()
        {
            PartRegistry.Load();
            ConstellationRegionRegistry.Load();

            ConstellationGraph graph = new ConstellationGraph();
            ConstellationGenerator.Build(graph);

            ConstellationValidator.Result result = ConstellationValidator.Validate(graph);
            if (result.Ok == false)
            {
                StringBuilder fail = new StringBuilder(512);
                fail.AppendLine($"[성좌] 오류 {result.Errors.Count}건 — 저장하지 않았다");
                for (int i = 0; i < result.Errors.Count && i < 10; i++)
                {
                    fail.AppendLine($"  {result.Errors[i]}");
                }
                return fail.ToString();
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string path = Path.Combine(projectRoot,
                ConstellationGraphIO.SourcePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, ConstellationGraphIO.Serialize(graph), new UTF8Encoding(false));

            return $"[성좌] 초안을 SSOT 에 저장 — 노드 {graph.Nodes.Count} · 간선 {graph.EdgeCount}\n"
                   + $"  {path}\n  굽기: cd _DataExporter && node export.js";
        }

        private static void AppendShape(StringBuilder sb, ConstellationGraph graph)
        {
            Dictionary<EConstellationNodeType, int> byType = new Dictionary<EConstellationNodeType, int>(8);
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                byType.TryGetValue(graph.Nodes[i].Type, out int c);
                byType[graph.Nodes[i].Type] = c + 1;
            }

            sb.Append("종류: ");
            foreach (KeyValuePair<EConstellationNodeType, int> kv in byType)
            {
                sb.Append($"{kv.Key} {kv.Value}  ");
            }
            sb.AppendLine();

            for (int r = 0; r < ConstellationRegionRegistry.Count; r++)
            {
                ConstellationRegionData region = ConstellationRegionRegistry.All[r];
                int gate = 0;
                int notable = 0;
                int keystone = 0;
                int skill = 0;
                int total = 0;
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    ConstellationNode n = graph.Nodes[i];
                    if (n.Family != region.Family)
                    {
                        continue;
                    }
                    total++;
                    switch (n.Type)
                    {
                        case EConstellationNodeType.Gate: gate++; break;
                        case EConstellationNodeType.Notable: notable++; break;
                        case EConstellationNodeType.Keystone: keystone++; break;
                        case EConstellationNodeType.Skill: skill++; break;
                    }
                }
                sb.AppendLine($"  {region.DisplayName}: 노드 {total} · 스킬 {skill} · 성단중심 {notable} · 관문 {gate} · 키스톤 {keystone}");
            }

            int skillNodes = byType.TryGetValue(EConstellationNodeType.Skill, out int sn) ? sn : 0;
            sb.AppendLine(skillNodes == PartRegistry.Count
                ? $"스킬 배치: 파츠 {PartRegistry.Count}종 전부 성좌에 있다"
                : $"[문제] 파츠 {PartRegistry.Count}종 중 {skillNodes}종만 성좌에 있다");
        }

        private static void AppendCost(StringBuilder sb, ConstellationGraph graph)
        {
            long total = ConstellationValidator.TotalCost(graph);
            sb.AppendLine($"전부 사는 값: 슈퍼 DNA {total}");
            sb.AppendLine($"완주 판수: 초반 수급({DnaPerRunEarly:0}/판) {total / DnaPerRunEarly:0}판 · "
                          + $"후반 수급({DnaPerRunLate:0}/판) {total / DnaPerRunLate:0}판");

            Vector2 extent = graph.Extent;
            sb.AppendLine($"차지하는 크기 {extent.x:0} x {extent.y:0} — 창 1052x540 에 담으려면 배율 "
                          + $"{Mathf.Min(1052f * 0.94f / Mathf.Max(1f, extent.x), 540f * 0.94f / Mathf.Max(1f, extent.y)):0.00}");
        }

        private static void AppendValidation(StringBuilder sb, ConstellationGraph graph)
        {
            ConstellationValidator.Result result = ConstellationValidator.Validate(graph);
            if (result.Ok && result.Warnings.Count == 0)
            {
                sb.AppendLine("성립성: 이상 없음");
                return;
            }

            for (int i = 0; i < result.Errors.Count && i < 10; i++)
            {
                sb.AppendLine($"[문제] {result.Errors[i]}");
            }
            for (int i = 0; i < result.Warnings.Count && i < 10; i++)
            {
                sb.AppendLine($"[경고] {result.Warnings[i]}");
            }
            if (result.Errors.Count > 10 || result.Warnings.Count > 10)
            {
                sb.AppendLine($"    … 오류 {result.Errors.Count} · 경고 {result.Warnings.Count}");
            }
        }

        private static void AppendRoundTrip(StringBuilder sb, ConstellationGraph graph)
        {
            string text = ConstellationGraphIO.Serialize(graph);

            ConstellationGraph back = new ConstellationGraph();
            if (ConstellationGraphIO.TryParse(text, back) == false)
            {
                sb.AppendLine("[문제] 왕복: 저장한 것을 다시 읽지 못했다");
                return;
            }

            List<string> diffs = new List<string>(8);
            if (back.Nodes.Count != graph.Nodes.Count)
            {
                diffs.Add($"노드 {graph.Nodes.Count} → {back.Nodes.Count}");
            }
            if (back.EdgeCount != graph.EdgeCount)
            {
                diffs.Add($"간선 {graph.EdgeCount} → {back.EdgeCount}");
            }

            for (int i = 0; i < graph.Nodes.Count && diffs.Count < 5; i++)
            {
                ConstellationNode a = graph.Nodes[i];
                ConstellationNode b = back.Get(a.Id);
                if (b == null)
                {
                    diffs.Add($"#{a.Id} 이 사라졌다");
                    continue;
                }
                if (a.Type != b.Type || a.Family != b.Family || a.Trait != b.Trait
                    || a.Cost != b.Cost || a.MaxLevel != b.MaxLevel || a.SkillId != b.SkillId
                    || a.NeedClear != b.NeedClear || a.FamilyScoped != b.FamilyScoped)
                {
                    diffs.Add($"#{a.Id} 속성이 달라졌다");
                    continue;
                }
                if (Mathf.Abs(a.X - b.X) > 0.01f || Mathf.Abs(a.Y - b.Y) > 0.01f)
                {
                    diffs.Add($"#{a.Id} 좌표가 달라졌다 ({a.X:0.###},{a.Y:0.###}) → ({b.X:0.###},{b.Y:0.###})");
                    continue;
                }
                if (Mathf.Abs(a.Value - b.Value) > 0.0005f)
                {
                    diffs.Add($"#{a.Id} 값이 달라졌다 {a.Value} → {b.Value}");
                    continue;
                }
                if (a.Neighbors.Count != b.Neighbors.Count)
                {
                    diffs.Add($"#{a.Id} 이웃 수가 달라졌다 {a.Neighbors.Count} → {b.Neighbors.Count}");
                }
            }

            if (diffs.Count == 0)
            {
                sb.AppendLine($"왕복: 무손실 ({text.Length / 1024}KB)");
                return;
            }
            for (int i = 0; i < diffs.Count; i++)
            {
                sb.AppendLine($"[문제] 왕복: {diffs[i]}");
            }
        }
    }
}
