using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Parts
{
    public enum EPartGrade
    {
        Common = 1,
        Rare = 2,
        Legendary = 3
    }

    public class PartInstance
    {
        public PartData Data;
        public EPartGrade Grade = EPartGrade.Common;
        public readonly List<PartOptionData> Options = new List<PartOptionData>(3);

        public int PartId { get { return Data != null ? Data.Id : 0; } }
        public int SkillId { get { return Data != null ? Data.SkillId : 0; } }
        public EPartSlot Slot { get { return Data != null ? Data.Slot : EPartSlot.None; } }
        public string DisplayName { get { return Data != null ? Data.DisplayName : string.Empty; } }

        public static EPartGrade RollGrade()
        {
            float r = Random.value;
            if (r < 0.10f)
                return EPartGrade.Legendary;
            if (r < 0.40f)
                return EPartGrade.Rare;
            return EPartGrade.Common;
        }

        public static PartInstance Create(PartData data, EPartGrade grade)
        {
            if (data == null)
                return null;

            PartInstance inst = new PartInstance { Data = data, Grade = grade };
            IReadOnlyList<PartOptionData> pool = PartOptionRegistry.ForPart(data.Id);
            if (pool == null || pool.Count == 0)
                return inst;

            int slots = (int)grade;
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

        public const int MaxOptions = 6;

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

            m.CooldownMul = Mathf.Max(0.2f, m.CooldownMul);
            return m;
        }

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
