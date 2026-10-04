using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem.Skill
{

    public class SkillConfig : BaseBehaviour
    {
        public string PoolKey = PoolManager.KeyEmpty;

        public SkillNodeData LaunchNode { get; private set; }
        public SkillNodeData HitNode { get; private set; }
        public SkillNodeData DespawnNode { get; private set; }
        public SkillLevelData LevelData { get; private set; }
        public SkillObject Caster { get; private set; }

        public SkillModifier Modifier { get; private set; } = SkillModifier.None;
        public ESkillTeam EnemyTeam { get; private set; }

        public float SpawnTime { get; private set; }
        public bool Despawning { get; private set; }

        public bool Armed;

        public int PulseCount;
        public Vector3 PrevPos;
        public Vector3 StartPos;

        public readonly List<SkillNodeData> DebuffNodes = new List<SkillNodeData>(2);
        public readonly Dictionary<Damageable, float> LastHitTime = new Dictionary<Damageable, float>(16);

        public void Initialize(SkillContext ctx, SkillNodeData launchNode, bool armed)
        {
            LaunchNode = launchNode;
            HitNode = ctx.HitNode;
            DespawnNode = ctx.DespawnNode;
            LevelData = ctx.LevelData;
            Caster = ctx.Caster;
            Modifier = ctx.Modifier != null ? ctx.Modifier : SkillModifier.None;
            EnemyTeam = SkillTeamUtil.Opposite(ctx.Caster != null ? ctx.Caster.Team : ESkillTeam.Friend);

            DebuffNodes.Clear();
            DebuffNodes.AddRange(ctx.DebuffNodes);

            LastHitTime.Clear();
            PulseCount = 0;
            SpawnTime = Time.time;
            StartPos = transform.position;
            PrevPos = transform.position;
            Despawning = false;
            Armed = armed;
        }

        public float GetHitFloat(ESkillParamKey key, float fallback)
        {
            return HitNode != null ? HitNode.GetFloat(key, LevelData, fallback, Modifier) : fallback;
        }

        public int GetHitInt(ESkillParamKey key, int fallback)
        {
            return HitNode != null ? HitNode.GetInt(key, null, fallback, Modifier) : fallback;
        }

        public float GetLaunchFloat(ESkillParamKey key, float fallback)
        {
            return LaunchNode != null ? LaunchNode.GetFloat(key, LevelData, fallback, Modifier) : fallback;
        }

        public void Despawn()
        {
            if (Despawning)
                return;
            Despawning = true;

            if (PoolManager.Instance != null
                && string.IsNullOrEmpty(PoolKey) == false
                && PoolKey != PoolManager.KeyEmpty)
            {
                PoolManager.Instance.Pool_Skill_Return(PoolKey, gameObject);
                return;
            }
            Destroy(gameObject);
        }

        protected override void OnDisabled()
        {
            LaunchNode = null;
            HitNode = null;
            DespawnNode = null;
            LevelData = null;
            Caster = null;
            DebuffNodes.Clear();
            LastHitTime.Clear();
            PulseCount = 0;
            Armed = false;
        }
    }
}
