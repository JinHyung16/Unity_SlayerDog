using System.Collections.Generic;
using System.Text;
using Jinhyeong_SkillSystem;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    /// <summary>
    /// _DataExporter 산출물을 플레이 없이 확인한다.
    /// 익스포터가 빌드 타임 검증을 하긴 하지만, C# 로더가 실제로 그걸 먹는지는 별개 문제라 여기서 한 번 더 본다.
    /// </summary>
    public static class GameDataValidator
    {
        [MenuItem("Jinhyeong/GameData/DB 검증", false, 10)]
        public static void Validate()
        {
            SkillRegistry.Load();

            if (SkillRegistry.IsLoaded == false)
            {
                Debug.LogError("[GameDataValidator] 로드 실패 — 위 에러를 확인하세요");
                return;
            }

            StringBuilder report = new StringBuilder(512);
            report.AppendLine($"[GameDataValidator] 스킬 {SkillRegistry.Count}개 / 버프 {SkillBuffRegistry.BuffCount}개 / 디버프 {SkillBuffRegistry.DebuffCount}개");

            int danglingRefs = 0;

            foreach (SkillDefinition def in SkillRegistry.All)
            {
                int nodeCount = CountNodes(def.Root);
                report.AppendLine($"  #{def.Meta.Id} {def.Meta.Name} — 노드 {nodeCount}개, 레벨 {def.Levels.Count}개 [{def.Meta.MethodPath}]");

                if (def.Root == null)
                {
                    Debug.LogWarning($"[GameDataValidator] skill {def.Meta.Id} 에 트리가 없습니다");
                }
                danglingRefs += CountDanglingStatusRefs(def.Root, def.Meta.Id);
            }

            HashSet<string> visuals = new HashSet<string>();
            SkillRegistry.CollectVisualKeys(visuals);
            report.AppendLine($"  참조 비주얼 키 {visuals.Count}개");

            int missingAddress = CheckAddressables(visuals, report);

            Debug.Log(report.ToString());

            if (danglingRefs > 0)
            {
                Debug.LogError($"[GameDataValidator] 존재하지 않는 버프/디버프를 가리키는 노드 {danglingRefs}개");
            }
            if (missingAddress > 0)
            {
                Debug.LogError($"[GameDataValidator] Addressable 에 없는 비주얼 키 {missingAddress}개 — 'Jinhyeong > GameData > 스킬 VFX 프리팹 재생성' 을 돌리세요");
            }
        }

        /// <summary>
        /// DB가 참조하는 비주얼 키가 전부 Addressable 주소로 등록돼 있는지 본다.
        /// 빠지면 런타임에 흰 구체 폴백으로 조용히 대체돼서 눈치채기 어렵다.
        /// </summary>
        private static int CheckAddressables(ICollection<string> keys, StringBuilder report)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                report.AppendLine("  [!] Addressable 설정 없음 — 주소 검사 건너뜀");
                return 0;
            }

            HashSet<string> addresses = new HashSet<string>();
            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>(64);
            settings.GetAllAssets(entries, false);
            for (int i = 0; i < entries.Count; i++)
            {
                addresses.Add(entries[i].address);
            }

            int missing = 0;
            foreach (string key in keys)
            {
                if (addresses.Contains(key))
                {
                    continue;
                }
                report.AppendLine($"  [!] Addressable 미등록: {key}");
                missing++;
            }

            report.AppendLine($"  Addressable 등록 확인: {keys.Count - missing}/{keys.Count}");
            return missing;
        }

        private static int CountNodes(SkillNodeData node)
        {
            if (node == null)
            {
                return 0;
            }
            int total = 1;
            for (int i = 0; i < node.Children.Count; i++)
            {
                total += CountNodes(node.Children[i]);
            }
            return total;
        }

        /// <summary>
        /// BuffId/DebuffId 는 익스포터가 경로로 검증하지만, 손으로 숫자를 박은 경우까지는 못 막는다.
        /// </summary>
        private static int CountDanglingStatusRefs(SkillNodeData node, int skillId)
        {
            if (node == null)
            {
                return 0;
            }

            int dangling = 0;

            if (node.HasParam(ESkillParamKey.BuffId))
            {
                int buffId = node.GetInt(ESkillParamKey.BuffId);
                if (SkillBuffRegistry.GetBuff(buffId) == null)
                {
                    Debug.LogError($"[GameDataValidator] skill {skillId} node {node.NodeId}: buff {buffId} 없음");
                    dangling++;
                }
            }

            if (node.HasParam(ESkillParamKey.DebuffId))
            {
                int debuffId = node.GetInt(ESkillParamKey.DebuffId);
                if (SkillBuffRegistry.GetDebuff(debuffId) == null)
                {
                    Debug.LogError($"[GameDataValidator] skill {skillId} node {node.NodeId}: debuff {debuffId} 없음");
                    dangling++;
                }
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                dangling += CountDanglingStatusRefs(node.Children[i], skillId);
            }
            return dangling;
        }
    }
}
