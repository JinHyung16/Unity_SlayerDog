using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_Constellation
{

    public static class ConstellationGraphIO
    {
        public const string ResourcePath = "GameData/ConstellationGraph";

        public const string SourcePath = "_DataExporter/GameData/_raw/ConstellationGraph.json";

        public const int Version = 1;

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
