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

        public SkillModifier Modifier = SkillModifier.None;

        public int Depth = 0;

        public bool AttackPending;

        public bool ManualCast;

        public readonly List<Damageable> Targets = new List<Damageable>(8);

        public SkillNodeData HitNode;
        public SkillNodeData DespawnNode;
        public readonly List<SkillNodeData> DebuffNodes = new List<SkillNodeData>(2);

        // 연발의 다음 발은 몇 프레임 뒤에 쏜다 — 원본 컨텍스트는 그 사이 다음 발동에 재사용되므로 복사해 둔다
        public SkillContext CloneForLaunch()
        {
            SkillContext c = new SkillContext();
            c.Caster = Caster;
            c.OriginPosition = OriginPosition;
            c.Direction = Direction;
            c.Level = Level;
            c.LevelData = LevelData;
            c.Modifier = Modifier;
            c.Depth = Depth;
            c.Targets.AddRange(Targets);
            c.HitNode = HitNode;
            c.DespawnNode = DespawnNode;
            c.DebuffNodes.AddRange(DebuffNodes);
            return c;
        }

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
            Modifier = SkillModifier.None;
        }
    }
}
