using System.Collections.Generic;
using Jinhyeong_GameData;

namespace Jinhyeong_Constellation
{

    public class ConstellationNode
    {
        public int Id;
        public EConstellationNodeType Type;

        public ESkillFamily Family;

        public float X;
        public float Y;

        public int Ring;

        public int Cost;

        public int MaxLevel = 1;

        public EPartTrait Trait;
        public float Value;

        public bool FamilyScoped;

        public int SkillId;

        public int NeedClear;

        public string DisplayName = string.Empty;
        public string Description = string.Empty;

        public readonly List<int> Neighbors = new List<int>(4);
    }
}
