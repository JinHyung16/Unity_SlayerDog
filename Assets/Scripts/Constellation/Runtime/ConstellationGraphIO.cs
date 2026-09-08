using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_Constellation
{

    /// <summary>
    /// 성좌 그래프의 «중간 형식» 읽기·쓰기.
    ///
    /// 도구는 최종 저장 형식을 직접 만들지 않는다 — JSON 한 덩이만 뱉고, 굽기는 한 곳(export.js)에서 한다.
    /// 도구가 최종 형식을 직접 쓰면, 사람이 손으로 고친 경로와 도구가 쓴 경로가 서로 다른 결과를 낸다.
    ///
    /// 저장하는 것은 **배치와 연결**뿐이다. 이름·설명은 특성에서 나오므로(<see cref="ConstellationNaming"/>)
    /// 읽을 때 다시 만든다 — 저장에 적어 두면 값을 조정할 때마다 이름이 어긋난다.
    ///
    /// 간선은 «방향 없는 쌍»이라 한 번만 적는다. 읽을 때 <see cref="ConstellationGraph.Link"/> 가
    /// 양쪽 이웃 목록을 채운다 — 그 함수 하나가 이 그래프의 공식 전부다.
    /// </summary>
    public static class ConstellationGraphIO
    {
        /// <summary>런타임이 읽는 자리. 이 파일이 있으면 «손으로 만든 성좌»가 시드 생성보다 우선한다.</summary>
        public const string ResourcePath = "GameData/ConstellationGraph";

        /// <summary>도구가 쓰는 자리(SSOT). 여기서 굽혀 위 Resources 로 간다.</summary>
        public const string SourcePath = "_DataExporter/GameData/_raw/ConstellationGraph.json";

        public const int Version = 1;

        // ───────────────────────── 읽기 ─────────────────────────

        /// <summary>Resources 에 구워진 그래프를 읽는다. 없으면 false — 부르는 쪽이 생성기로 넘어간다.</summary>
        public static bool TryLoadBaked(ConstellationGraph graph)
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                return false;
            }

            string text = asset.text;
            Resources.UnloadAsset(asset);
            return TryParse(text, graph);
        }

        public static bool TryParse(string text, ConstellationGraph graph)
        {
            if (graph == null || string.IsNullOrEmpty(text))
            {
                return false;
            }

            JsonValue root;
            try
            {
                root = JsonValue.Parse(text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[ConstellationGraphIO] 파싱 실패 — {e.Message}");
                return false;
            }

            JsonValue nodes = root["nodes"];
            if (nodes.IsArray == false || nodes.Count == 0)
            {
                // 빈 입력은 거부한다. 통과시키면 성좌가 통째로 사라진 채 게임이 뜬다.
                Debug.LogWarning("[ConstellationGraphIO] 노드가 비어 있어 읽지 않았습니다");
                return false;
            }

            graph.Clear();

            for (int i = 0; i < nodes.Count; i++)
            {
                ConstellationNode node = ReadNode(nodes[i]);
                if (node == null || node.Id == 0)
                {
                    continue;
                }
                ConstellationNaming.Apply(node);
                graph.Add(node);
            }

            JsonValue edges = root["edges"];
            for (int i = 0; i < edges.Count; i++)
            {
                JsonValue pair = edges[i];
                if (pair.Count < 2)
                {
                    continue;
                }
                graph.Link(pair[0].AsInt(), pair[1].AsInt());
            }
            return true;
        }

        private static ConstellationNode ReadNode(JsonValue row)
        {
            ConstellationNode n = new ConstellationNode();
            n.Id = row.GetInt("id");
            n.Type = ParseEnum(row.GetString("type"), EConstellationNodeType.Small);
            n.Family = ParseEnum(row.GetString("family"), ESkillFamily.None);
            n.X = row.GetFloat("x");
            n.Y = row.GetFloat("y");
            n.Ring = row.GetInt("ring");
            n.Cost = row.GetInt("cost");
            n.MaxLevel = Mathf.Max(1, row.GetInt("maxLevel", 1));
            n.Trait = ParseEnum(row.GetString("trait"), EPartTrait.None);
            n.Value = row.GetFloat("value");
            n.FamilyScoped = row["familyScoped"].AsBool();
            n.SkillId = row.GetInt("skillId");
            n.NeedClear = row.GetInt("needClear");
            n.DisplayName = row.GetString("name");
            n.Description = row.GetString("desc");
            return n;
        }

        private static T ParseEnum<T>(string text, T fallback) where T : struct
        {
            if (string.IsNullOrEmpty(text))
            {
                return fallback;
            }
            return Enum.TryParse(text, false, out T v) ? v : fallback;
        }

        // ───────────────────────── 쓰기 ─────────────────────────

        /// <summary>
        /// 중간 형식으로 직렬화한다. 사람이 열어 볼 수 있게 줄을 나눈다 —
        /// 한 줄짜리 JSON 은 diff 가 통째로 바뀌어 무엇이 달라졌는지 볼 수 없다.
        /// </summary>
        public static string Serialize(ConstellationGraph graph)
        {
            StringBuilder sb = new StringBuilder(64 * 1024);
            sb.Append("{\n");
            sb.Append("    \"version\": ").Append(Version).Append(",\n");
            sb.Append("    \"nodes\": [\n");

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                ConstellationNode n = graph.Nodes[i];
                sb.Append("        {");
                sb.Append("\"id\": ").Append(n.Id);
                sb.Append(", \"type\": \"").Append(n.Type).Append('"');
                sb.Append(", \"family\": \"").Append(n.Family).Append('"');
                sb.Append(", \"x\": ").Append(F(n.X));
                sb.Append(", \"y\": ").Append(F(n.Y));
                sb.Append(", \"ring\": ").Append(n.Ring);
                sb.Append(", \"cost\": ").Append(n.Cost);
                sb.Append(", \"maxLevel\": ").Append(n.MaxLevel);
                sb.Append(", \"trait\": \"").Append(n.Trait).Append('"');
                sb.Append(", \"value\": ").Append(F(n.Value));
                sb.Append(", \"familyScoped\": ").Append(n.FamilyScoped ? "true" : "false");
                sb.Append(", \"skillId\": ").Append(n.SkillId);
                sb.Append(", \"needClear\": ").Append(n.NeedClear);

                // 이름은 특성에서 나온다. 스스로 이름을 드는 종류만 적는다 (표시 정보는 한 곳에만).
                if (ConstellationNaming.HoldsOwnName(n.Type))
                {
                    sb.Append(", \"name\": ").Append(Q(n.DisplayName));
                    sb.Append(", \"desc\": ").Append(Q(n.Description));
                }

                sb.Append('}');
                sb.Append(i == graph.Nodes.Count - 1 ? "\n" : ",\n");
            }

            sb.Append("    ],\n");
            sb.Append("    \"edges\": [\n");
            for (int i = 0; i < graph.EdgeCount; i++)
            {
                sb.Append("        [").Append(graph.EdgeA[i]).Append(", ").Append(graph.EdgeB[i]).Append(']');
                sb.Append(i == graph.EdgeCount - 1 ? "\n" : ",\n");
            }
            sb.Append("    ]\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        private static string F(float v)
        {
            // 소수점 자리를 고정해 둔다. 문화권 설정에 따라 쉼표가 찍히면 JSON 이 통째로 깨진다.
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Q(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "\"\"";
            }
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
