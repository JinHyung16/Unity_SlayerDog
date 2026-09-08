using System;
using System.Collections.Generic;
using Jinhyeong_GameData;
using Jinhyeong_GameData.Json;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{
    /// <summary>
    /// 파츠 옵션 테이블. 옵션 1개 = 특성 1개만 올린다 (복합 효과는 데이터로 만들 수 없다).
    ///
    /// 파츠를 주울 때 그 파츠에 존재하는 특성 중에서만 뽑아야 하므로 partId 로 묶어 둔다.
    /// </summary>
    public static class PartOptionRegistry
    {
        public const string ResourceOption = "GameData/PartOption";

        private static readonly Dictionary<int, PartOptionData> _byId = new Dictionary<int, PartOptionData>(128);
        private static readonly List<PartOptionData> _all = new List<PartOptionData>(128);
        private static readonly Dictionary<int, List<PartOptionData>> _byPart =
            new Dictionary<int, List<PartOptionData>>(32);

        public static int Count { get { return _all.Count; } }
        public static IReadOnlyList<PartOptionData> All { get { return _all; } }

        public static PartOptionData Get(int id)
        {
            return _byId.TryGetValue(id, out PartOptionData v) ? v : null;
        }

        public static IReadOnlyList<PartOptionData> ForPart(int partId)
        {
            return _byPart.TryGetValue(partId, out List<PartOptionData> list) ? list : null;
        }

        public static void Clear()
        {
            _byId.Clear();
            _all.Clear();
            _byPart.Clear();
        }

        public static void Load()
        {
            Clear();

            TextAsset asset = Resources.Load<TextAsset>(ResourceOption);
            if (asset == null)
            {
                Debug.LogWarning($"[PartOptionRegistry] '{ResourceOption}' 을 찾을 수 없습니다");
                return;
            }

            JsonValue rows;
            try
            {
                rows = JsonValue.Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[PartOptionRegistry] 파싱 실패 — {e.Message}");
                return;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                PartOptionData d = PartOptionData.FromJson(rows[i]);
                if (d == null || d.Id == 0)
                    continue;
                _byId[d.Id] = d;
                _all.Add(d);

                if (_byPart.TryGetValue(d.PartId, out List<PartOptionData> list) == false)
                {
                    list = new List<PartOptionData>(6);
                    _byPart[d.PartId] = list;
                }
                list.Add(d);
            }
        }
    }
}
