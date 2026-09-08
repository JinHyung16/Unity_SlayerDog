using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Parts
{
    /// <summary>파츠 등급. 등급 = 옵션 슬롯 수다. 다른 의미를 붙이지 않는다.</summary>
    public enum EPartGrade
    {
        Common = 1,
        Rare = 2,
        Legendary = 3
    }

    /// <summary>
    /// 필드에서 주운 파츠 한 개체.
    ///
    /// 같은 파츠라도 개체마다 옵션이 다르다 — 그래서 원형(PartData)과 인스턴스를 분리한다.
    /// 교체할지 버릴지는 옵션까지 비교해야 결정되므로, 이 클래스가 비교의 단위다.
    /// </summary>
    public class PartInstance
    {
        public PartData Data;
        public EPartGrade Grade = EPartGrade.Common;
        public readonly List<PartOptionData> Options = new List<PartOptionData>(3);

        public int PartId { get { return Data != null ? Data.Id : 0; } }
        public int SkillId { get { return Data != null ? Data.SkillId : 0; } }
        public EPartSlot Slot { get { return Data != null ? Data.Slot : EPartSlot.None; } }
        public string DisplayName { get { return Data != null ? Data.DisplayName : string.Empty; } }

        /// <summary>등급별 드랍 확률. 일반 60 / 희귀 30 / 전설 10.</summary>
        public static EPartGrade RollGrade()
        {
            float r = Random.value;
            if (r < 0.10f)
                return EPartGrade.Legendary;
            if (r < 0.40f)
                return EPartGrade.Rare;
            return EPartGrade.Common;
        }

        /// <summary>파츠 원형에서 개체를 만든다. 옵션은 그 파츠에 존재하는 특성 중에서만 뽑는다.</summary>
        public static PartInstance Create(PartData data, EPartGrade grade)
        {
            if (data == null)
                return null;

            PartInstance inst = new PartInstance { Data = data, Grade = grade };
            IReadOnlyList<PartOptionData> pool = PartOptionRegistry.ForPart(data.Id);
            if (pool == null || pool.Count == 0)
                return inst;

            int slots = (int)grade;
            // 중첩 한도를 넘는 옵션이 또 뽑히면 헛수고라 카운트를 세며 뽑는다.
            Dictionary<int, int> taken = new Dictionary<int, int>(4);
            int guard = 0;
            while (inst.Options.Count < slots && guard++ < 64)
            {
                PartOptionData pick = pool[Random.Range(0, pool.Count)];
                taken.TryGetValue(pick.Id, out int used);
                if (used >= Mathf.Max(1, pick.MaxStack))
                    continue;
                taken[pick.Id] = used + 1;
                inst.Options.Add(pick);
            }
            return inst;
        }

        /// <summary>한 파츠가 쌓을 수 있는 옵션 상한. 이걸 넘으면 한 스킬만 키워도 끝이 보여야 한다.</summary>
        public const int MaxOptions = 6;

        /// <summary>
        /// 같은 스킬의 파츠를 또 주웠을 때 옵션을 **합친다**.
        ///
        /// 교체가 아니라 누적이어야 「한 스킬을 깊게 판다」가 성립한다.
        /// 교체만 하면 등급 운으로 옵션 수가 정해지고, 특화가 실력이 아니라 운의 보상이 된다.
        /// 중첩 한도와 총 상한은 지킨다 — 안 그러면 같은 옵션 10중첩이 나온다.
        /// </summary>
        public int Merge(PartInstance other)
        {
            if (other == null || other.SkillId != SkillId)
                return 0;

            Dictionary<int, int> used = new Dictionary<int, int>(8);
            for (int i = 0; i < Options.Count; i++)
            {
                used.TryGetValue(Options[i].Id, out int c);
                used[Options[i].Id] = c + 1;
            }

            int added = 0;
            for (int i = 0; i < other.Options.Count && Options.Count < MaxOptions; i++)
            {
                PartOptionData o = other.Options[i];
                if (o == null)
                    continue;
                used.TryGetValue(o.Id, out int c);
                if (c >= Mathf.Max(1, o.MaxStack))
                    continue;
                used[o.Id] = c + 1;
                Options.Add(o);
                added++;
            }

            if (added > 0 && (int)other.Grade > (int)Grade)
                Grade = other.Grade;
            return added;
        }

        /// <summary>옵션을 모아 스킬 보정으로 만든다. 여기가 특성 → 실제 수치의 유일한 변환처다.</summary>
        public SkillModifier BuildModifier()
        {
            SkillModifier m = new SkillModifier();
            for (int i = 0; i < Options.Count; i++)
            {
                PartOptionData o = Options[i];
                if (o == null)
                    continue;

                if (o.Trait == EPartTrait.Unique)
                    ApplyUnique(m, o);
                else
                    m.AddTrait(o.Trait, o.Value);

            }

            // 쿨 감소가 100% 를 넘으면 매 프레임 발동해 게임이 터진다.
            m.CooldownMul = Mathf.Max(0.2f, m.CooldownMul);
            return m;
        }

        /// <summary>고유 옵션은 어느 필드를 건드릴지 데이터가 지정한다 — 코드에 파츠별 분기를 넣지 않는다.</summary>
        private static void ApplyUnique(SkillModifier m, PartOptionData o)
        {
            switch (o.UniqueField)
            {
                case "StatusValueAdd":
                    m.StatusValueAdd += o.Value;
                    break;
                case "CountAdd":
                    m.CountAdd += Mathf.RoundToInt(o.Value);
                    break;
                case "IntervalAdd":
                    m.IntervalAdd += o.Value;
                    break;
                case "AreaMul":
                    m.AreaMul += o.Value;
                    break;
                case "CritChanceAdd":
                    m.CritChanceAdd += o.Value;
                    break;
                default:
                    Debug.LogWarning($"[PartInstance] 알 수 없는 고유 필드 '{o.UniqueField}' (옵션 {o.Id})");
                    break;
            }
        }

        /// <summary>교체 UI 가 쓸 한 줄 요약.</summary>
        public string Describe()
        {
            if (Data == null)
                return string.Empty;
            if (Options.Count == 0)
                return DisplayName;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(DisplayName);
            for (int i = 0; i < Options.Count; i++)
            {
                sb.Append('\n');
                sb.Append("· ");
                sb.Append(Options[i].DisplayName);
            }
            return sb.ToString();
        }
    }
}
