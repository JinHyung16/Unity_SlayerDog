using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using Jinhyeong_Managers;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{

    /// <summary>
    /// 스킬 DB 레지스트리.
    /// 원본은 _DataExporter/GameData (경로 기반 모듈 트리)이고, 여기서 읽는 건 그 빌드 산출물이다.
    /// 슬롯·토큰은 빌드 타임에 이미 해석돼 있어서 런타임이 할 일은 트리를 그대로 옮겨 담는 것뿐이다.
    /// </summary>
    public static class SkillRegistry
    {
        public const string ResourceSkill = "GameData/Skill";

        private static readonly Dictionary<int, SkillDefinition> _byId =
            new Dictionary<int, SkillDefinition>(32);

        private static Task _loadingTask;

        public static int Count { get { return _byId.Count; } }
        public static IEnumerable<SkillDefinition> All { get { return _byId.Values; } }
        public static bool IsLoaded { get; private set; }

        public static void Add(SkillDefinition def)
        {
            if (def == null || def.Meta == null)
                return;
            _byId[def.Meta.Id] = def;
        }

        public static SkillDefinition Get(int skillId)
        {
            return _byId.TryGetValue(skillId, out SkillDefinition def) ? def : null;
        }

        public static bool TryGet(int skillId, out SkillDefinition def)
        {
            return _byId.TryGetValue(skillId, out def);
        }

        public static void Clear()
        {
            _byId.Clear();
            IsLoaded = false;
        }

        public static Task LoadAsync()
        {
            if (IsLoaded)
            {
                return Task.CompletedTask;
            }
            // 이미 메인스레드면 LoadInternalAsync 가 동기로 끝나 finally 가 대입보다 먼저 돈다.
            // 그러면 완료된 태스크가 필드에 남아 다음 로드(씬 리로드)를 영원히 건너뛴다 — 완료 여부로 거른다.
            if (_loadingTask != null && _loadingTask.IsCompleted == false)
            {
                return _loadingTask;
            }
            Task task = LoadInternalAsync();
            _loadingTask = task.IsCompleted ? null : task;
            return task;
        }

        private static async Task LoadInternalAsync()
        {
            try
            {
                await UniTask.SwitchToMainThread();
                Load();
            }
            finally
            {
                _loadingTask = null;
            }
        }

        /// <summary>
        /// Resources 에서 동기 로드. 에디터 툴이나 테스트에서 직접 부를 수 있게 public 으로 둔다.
        /// </summary>
        public static void Load()
        {
            Clear();

            TextAsset asset = Resources.Load<TextAsset>(ResourceSkill);
            if (asset == null)
            {
                Debug.LogError($"[SkillRegistry] '{ResourceSkill}' 을 찾을 수 없습니다. _DataExporter 에서 export 를 돌렸는지 확인하세요.");
                return;
            }

            JsonValue root;
            try
            {
                root = JsonValue.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[SkillRegistry] {ResourceSkill}.json 파싱 실패 — {e.Message}");
                return;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }

            if (root.IsArray == false)
            {
                Debug.LogError($"[SkillRegistry] {ResourceSkill}.json 최상위가 배열이 아닙니다");
                return;
            }

            for (int i = 0; i < root.Count; i++)
            {
                SkillDefinition def = BuildSkill(root[i], i);
                if (def == null)
                    continue;
                Add(def);
            }

            SkillBuffRegistry.Load();
            PartRegistry.Load();
            PartOptionRegistry.Load();
            RoundRegistry.Load();
            SynergyRegistry.Load();
            EnemyTypeRegistry.Load();
            WaveEventRegistry.Load();

            IsLoaded = true;
        }

        private static SkillDefinition BuildSkill(JsonValue row, int index)
        {
            int id = row.GetInt("id");
            if (id <= 0)
            {
                Debug.LogWarning($"[SkillRegistry] {index}번 항목에 유효한 id 가 없습니다 — 건너뜀");
                return null;
            }

            SkillDefinition def = new SkillDefinition
            {
                Meta = new SkillData
                {
                    Id = id,
                    Name = row.GetString("name"),
                    Description = row.GetString("desc"),
                    MaxLevel = Math.Max(1, row.GetInt("maxLevel", 1)),
                    VisualPath = row.GetString("visualPath"),
                    MethodPath = row.GetString("methodPath"),
                },
                Root = BuildNode(row["tree"], id),
            };

            BuildLevels(def, row["levels"], id);
            return def;
        }

        private static SkillNodeData BuildNode(JsonValue node, int skillId)
        {
            if (node.IsObject == false)
            {
                return null;
            }

            string typeRaw = node.GetString("type");
            if (Enum.TryParse(typeRaw, false, out ESkillNodeType nodeType) == false)
            {
                Debug.LogWarning($"[SkillRegistry] skill {skillId}: 알 수 없는 node type '{typeRaw}'");
                nodeType = ESkillNodeType.None;
            }

            SkillNodeData data = new SkillNodeData
            {
                NodeId = node.GetInt("id"),
                NodeType = nodeType,
            };

            foreach (KeyValuePair<string, JsonValue> pair in node["params"].Members)
            {
                if (Enum.TryParse(pair.Key, false, out ESkillParamKey key) == false || key == ESkillParamKey.None)
                {
                    Debug.LogWarning($"[SkillRegistry] skill {skillId} node {data.NodeId}: 알 수 없는 param '{pair.Key}'");
                    continue;
                }
                data.Params[key] = pair.Value.ToRawString();
            }

            JsonValue children = node["children"];
            for (int i = 0; i < children.Count; i++)
            {
                SkillNodeData child = BuildNode(children[i], skillId);
                if (child == null)
                    continue;
                data.Children.Add(child);
            }

            return data;
        }

        private static void BuildLevels(SkillDefinition def, JsonValue levels, int skillId)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                JsonValue row = levels[i];
                int level = row.GetInt("level");
                if (level < 1)
                {
                    continue;
                }

                SkillLevelData data = new SkillLevelData
                {
                    SkillId = skillId,
                    Level = level,
                };

                foreach (KeyValuePair<string, JsonValue> pair in row["modifiers"].Members)
                {
                    if (Enum.TryParse(pair.Key, false, out ESkillParamKey key) == false || key == ESkillParamKey.None)
                    {
                        Debug.LogWarning($"[SkillRegistry] skill {skillId} level {level}: 알 수 없는 modifier '{pair.Key}'");
                        continue;
                    }
                    data.Modifiers[key] = pair.Value.AsFloat();
                }

                def.Levels[level] = data;
                if (level > def.Meta.MaxLevel)
                {
                    def.Meta.MaxLevel = level;
                }
            }
        }

        public static void CollectVisualKeys(ICollection<string> buffer)
        {
            if (buffer == null)
                return;

            foreach (SkillDefinition def in _byId.Values)
            {
                if (def == null)
                    continue;
                CollectVisualKeys(def.Root, buffer);
            }
        }

        private static void CollectVisualKeys(SkillNodeData node, ICollection<string> buffer)
        {
            if (node == null)
                return;

            string visual = node.GetString(ESkillParamKey.Visual);
            if (string.IsNullOrEmpty(visual) == false)
            {
                buffer.Add(visual);
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                CollectVisualKeys(node.Children[i], buffer);
            }
        }

        public static async UniTask PreloadVisualsAsync()
        {
            HashSet<string> keys = new HashSet<string>();
            CollectVisualKeys(keys);

            // 아트가 하나도 없어도 루트 프리팹은 있어야 스킬이 나간다
            keys.Add(CommonConfig.Skill.RootPrefabKey);
            keys.Add(CommonConfig.Skill.HitFxKey);
            keys.Add(CommonConfig.Skill.ChainFxKey);
            keys.Add(CommonConfig.Skill.XpGemKey);
            keys.Add(CommonConfig.Skill.DamageTextKey);

            AddressableManager am = AddressableManager.Ensure();
            await am.LoadAllAsync(keys);
        }
    }
}
