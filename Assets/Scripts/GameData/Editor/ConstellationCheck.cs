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

    /// <summary>
    /// 성좌를 «재는» 자리. 그리기 전에 숫자로 먼저 본다 —
    /// 화면을 만들고 나서 「노드가 너무 많다」를 알면 화면을 두 번 만든다.
    ///
    /// 재는 것은 넷이다.
    ///   ① 모양   — 노드/간선 수, 계열별 분포
    ///   ② 값     — 전부 사는 데 드는 슈퍼 DNA 총합 → 판당 수급으로 나눈 «완주 판수»
    ///   ③ 성립성 — <see cref="ConstellationValidator"/> (도달성 · 고아 · 중복 · 겹침)
    ///   ④ 왕복   — 저장했다 읽으면 같은 그래프가 나오는가
    ///
    /// ④가 있는 이유: 편집기는 사람이 클릭해야 도는데, 저장 형식이 깨진 것은 «다음에 열 때»
    /// 알게 된다. 그때는 이미 한나절 작업이 사라진 뒤다. 형식은 클릭 없이 재야 한다.
    /// </summary>
    public static class ConstellationCheck
    {
        /// <summary>런 하나가 주는 슈퍼 DNA 의 대략치. 완주 판수를 뽑는 분모다.</summary>
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

        /// <summary>
        /// 지금 그래프를 편집기가 읽는 자리(SSOT)에 쓴다. 사람이 창을 열지 않아도
        /// 「데이터로 만든 성좌」 흐름을 처음 한 번 깔 수 있다.
        /// </summary>
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

        // ───────────────────────── 항목 ─────────────────────────

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

        /// <summary>저장했다 다시 읽으면 같은 그래프인가. 편집기를 열지 않고 저장 형식을 잰다.</summary>
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
