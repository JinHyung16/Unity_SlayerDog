using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_SkillSystem
{

    public class SkillContext
    {
        public SkillObject Caster;
        public Vector3 OriginPosition;
        public Vector3 Direction = Vector3.right;
        public int Level = 1;

        public SkillLevelData LevelData;

        /// <summary>착용한 파츠 옵션이 만든 이 스킬 전용 보정. 없으면 SkillModifier.None.</summary>
        public SkillModifier Modifier = SkillModifier.None;

        public int Depth = 0;

        public bool AttackPending;

        public bool ManualCast;

        public readonly List<Damageable> Targets = new List<Damageable>(8);

        public SkillNodeData HitNode;
        public SkillNodeData DespawnNode;
        public readonly List<SkillNodeData> DebuffNodes = new List<SkillNodeData>(2);

        public void Reset(SkillObject caster, int level, SkillLevelData levelData, int depth = 0)
        {
            Caster = caster;
            Level = level;
            LevelData = levelData;
            Depth = depth;
            OriginPosition = caster != null ? caster.transform.position : Vector3.zero;
            Direction = caster != null ? caster.transform.forward : Vector3.right;
            Targets.Clear();
            HitNode = null;
            DespawnNode = null;
            DebuffNodes.Clear();
            AttackPending = false;
            ManualCast = false;
            // 호출부가 곧바로 덮어쓰지만, 안 덮어쓰는 경로에서 앞 스킬 보정이 새는 걸 막는다.
            Modifier = SkillModifier.None;
        }
    }
}
