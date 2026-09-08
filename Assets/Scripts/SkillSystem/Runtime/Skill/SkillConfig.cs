using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem.Skill
{

    /// <summary>
    /// 스킬 인스턴스 하나의 공유 상태 + 수명 관리. Skill_Root 프리팹에 미리 박혀 있다.
    ///
    /// DB 축과 1:1로 자른 컴포넌트 중 'stat/노드 데이터' 쪽. SkillMethod(이동)·SkillHit(판정)·
    /// SkillVisual(연출)이 전부 여기서 값을 읽는다. 런타임 AddComponent 는 없다.
    /// </summary>
    public class SkillConfig : BaseBehaviour
    {
        public string PoolKey = PoolManager.KeyEmpty;

        public SkillNodeData LaunchNode { get; private set; }
        public SkillNodeData HitNode { get; private set; }
        public SkillNodeData DespawnNode { get; private set; }
        public SkillLevelData LevelData { get; private set; }
        public SkillObject Caster { get; private set; }

        /// <summary>발사 시점의 파츠 옵션 보정. 날아가는 동안 파츠를 바꿔도 이 탄은 안 변한다.</summary>
        public SkillModifier Modifier { get; private set; } = SkillModifier.None;
        public ESkillTeam EnemyTeam { get; private set; }

        public float SpawnTime { get; private set; }
        public bool Despawning { get; private set; }

        /// <summary>
        /// 판정 활성 여부. 곡사(Arc)는 착탄해야 터지므로 비행 중에는 false 다 —
        /// 이게 없으면 운석이 목표까지 가기 전에 스쳐 지나간 첫 적에게 터진다.
        /// </summary>
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

        /// <summary>풀로 돌려보낸다. 두 번 불려도 안전해야 한다 — 이동/판정 양쪽에서 호출된다.</summary>
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

        /// <summary>풀에서 꺼내 쓰기 전 초기화. 남은 참조가 다음 발사에 새면 엉뚱한 대상을 때린다.</summary>
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
