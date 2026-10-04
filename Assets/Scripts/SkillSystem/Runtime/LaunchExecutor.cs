using UnityEngine;
using Cysharp.Threading.Tasks;
using Jinhyeong_GameData;
using Jinhyeong_Managers;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem.Skill;

namespace Jinhyeong_SkillSystem
{

    public static class LaunchExecutor
    {
        public static bool Execute(SkillNodeData launchNode, SkillContext ctx)
        {
            if (launchNode == null)
                return false;

            // 연발 — BurstCount 가 있는 노드만. 없는 노드에 개수 증강이 새어 들어가지 않게 키 유무로 가른다
            if (launchNode.Params.ContainsKey(ESkillParamKey.BurstCount) == false)
                return ExecuteOne(launchNode, ctx);

            int count = launchNode.GetInt(ESkillParamKey.BurstCount, ctx.LevelData, 1, ctx.Modifier);
            if (count <= 1)
                return ExecuteOne(launchNode, ctx);

            float interval = launchNode.GetFloat(ESkillParamKey.BurstInterval, ctx.LevelData, 0f, null);
            float spread = launchNode.GetFloat(ESkillParamKey.BurstSpread, ctx.LevelData, 0f, null);
            Vector3 baseDir = ResolveDirection(ctx);

            bool any = false;
            for (int i = 0; i < count; i++)
            {
                float angle = spread > 0f ? (i - (count - 1) * 0.5f) * spread : 0f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
                if (interval <= 0f || i == 0)
                {
                    ctx.Direction = dir;
                    any |= ExecuteOne(launchNode, ctx);
                    continue;
                }
                SkillContext later = ctx.CloneForLaunch();
                later.Direction = dir;
                FireLater(launchNode, later, interval * i).Forget();
                any = true;
            }
            return any;
        }

        private static async UniTaskVoid FireLater(SkillNodeData launchNode, SkillContext ctx, float delay)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(delay));
            // 쏘는 사이 시전자가 죽거나 판이 끝났으면 버린다
            if (ctx.Caster == null || ctx.Caster.isActiveAndEnabled == false)
                return;
            ctx.OriginPosition = ctx.Caster.transform.position;
            ExecuteOne(launchNode, ctx);
        }

        private static bool ExecuteOne(SkillNodeData launchNode, SkillContext ctx)
        {
            switch (launchNode.NodeType)
            {
                case ESkillNodeType.LaunchInstant:
                    return LaunchInstant(launchNode, ctx);

                case ESkillNodeType.LaunchStraight:
                    return LaunchLinear(launchNode, ctx);

                case ESkillNodeType.LaunchArc:
                    return LaunchArc(launchNode, ctx);

                case ESkillNodeType.LaunchCurve:
                    return LaunchCurve(launchNode, ctx);

                case ESkillNodeType.LaunchHoming:
                    return LaunchHoming(launchNode, ctx);
            }
            return false;
        }

        private static bool LaunchInstant(SkillNodeData launchNode, SkillContext ctx)
        {
            if (ctx.Targets.Count == 0)
                return false;

            Vector3 spawnPos = TargetPos(ctx.Targets[0]);
            SkillConfig config = Spawn(launchNode, ctx, spawnPos, Quaternion.identity, armed: true);
            if (config == null)
                return false;

            config.GetComponent<SkillMethod>().InitInstant();
            config.GetComponent<SkillHit>().ImmediateHit(ctx.Targets);
            return true;
        }

        private static bool LaunchLinear(SkillNodeData launchNode, SkillContext ctx)
        {
            Vector3 dir = ResolveDirection(ctx);
            SkillConfig config = Spawn(launchNode, ctx, OriginPos(ctx), Quaternion.LookRotation(dir), armed: true);
            if (config == null)
                return false;

            config.GetComponent<SkillMethod>().InitLinear(
                dir,
                launchNode.GetFloat(ESkillParamKey.Speed, ctx.LevelData, 5f, ctx.Modifier),
                launchNode.GetFloat(ESkillParamKey.MaxDistance, ctx.LevelData, 10f, ctx.Modifier));
            return true;
        }

        private static bool LaunchHoming(SkillNodeData launchNode, SkillContext ctx)
        {
            Vector3 dir = ResolveDirection(ctx);
            SkillConfig config = Spawn(launchNode, ctx, OriginPos(ctx), Quaternion.LookRotation(dir), armed: true);
            if (config == null)
                return false;

            config.GetComponent<SkillMethod>().InitHoming(
                ctx.Targets.Count > 0 ? ctx.Targets[0] : null,
                dir,
                launchNode.GetFloat(ESkillParamKey.Speed, ctx.LevelData, 5f, ctx.Modifier),
                launchNode.GetFloat(ESkillParamKey.MaxDistance, ctx.LevelData, 10f, ctx.Modifier),
                launchNode.GetFloat(ESkillParamKey.TurnRate, ctx.LevelData, 180f, ctx.Modifier));
            return true;
        }

        private static bool LaunchArc(SkillNodeData launchNode, SkillContext ctx)
        {
            Vector3 origin = OriginPos(ctx);
            Vector3 endPos = EndPos(ctx, origin);

            SkillConfig config = Spawn(launchNode, ctx, origin, LookTo(endPos - origin), armed: false);
            if (config == null)
                return false;

            config.GetComponent<SkillMethod>().InitArc(
                origin, endPos,
                launchNode.GetFloat(ESkillParamKey.Speed, ctx.LevelData, 5f, ctx.Modifier),
                launchNode.GetFloat(ESkillParamKey.ArcHeight, ctx.LevelData, 2f, ctx.Modifier));
            return true;
        }

        private static bool LaunchCurve(SkillNodeData launchNode, SkillContext ctx)
        {
            Vector3 origin = OriginPos(ctx);
            Vector3 endPos = EndPos(ctx, origin);

            SkillConfig config = Spawn(launchNode, ctx, origin, LookTo(endPos - origin), armed: true);
            if (config == null)
                return false;

            config.GetComponent<SkillMethod>().InitCurve(
                origin, endPos,
                launchNode.GetFloat(ESkillParamKey.Speed, ctx.LevelData, 5f, ctx.Modifier),
                launchNode.GetFloat(ESkillParamKey.ArcHeight, ctx.LevelData, 1f, ctx.Modifier));
            return true;
        }

        private static SkillConfig Spawn(SkillNodeData launchNode, SkillContext ctx, Vector3 pos, Quaternion rot, bool armed)
        {
            string visualKey = launchNode.GetString(ESkillParamKey.Visual);
            if (PoolManager.Instance == null)
            {
                Debug.LogError("[LaunchExecutor] PoolManager 가 없어 스킬을 스폰할 수 없습니다");
                return null;
            }

            GameObject go = PoolManager.Instance.Pool_Skill_Get(visualKey);
            if (go == null)
            {
                Debug.LogError($"[LaunchExecutor] Skill_Root 스폰 실패 (visual '{visualKey}')");
                return null;
            }

            go.transform.SetPositionAndRotation(pos, rot);

            SkillConfig config = go.GetComponent<SkillConfig>();
            config.PoolKey = string.IsNullOrEmpty(visualKey) ? PoolManager.KeyEmpty : visualKey;
            config.Initialize(ctx, launchNode, armed);

            SkillVisual visual = go.GetComponent<SkillVisual>();

            visual.SetOrient(ParseOrient(launchNode.GetString(ESkillParamKey.VisualOrient)));

            visual.ApplyTint(launchNode.GetString(ESkillParamKey.VisualColor));
            visual.ApplyHitScale();
            visual.Play();

            return config;
        }

        private static Jinhyeong_View.SkillArtOrient.EMode ParseOrient(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return Jinhyeong_View.SkillArtOrient.EMode.Ground;
            }
            if (raw == "Billboard")
            {
                return Jinhyeong_View.SkillArtOrient.EMode.Billboard;
            }
            if (raw == "Ground")
            {
                return Jinhyeong_View.SkillArtOrient.EMode.Ground;
            }

            Debug.LogWarning($"[LaunchExecutor] 알 수 없는 VisualOrient '{raw}' — Ground 로 둔다");
            return Jinhyeong_View.SkillArtOrient.EMode.Ground;
        }

        private static Quaternion LookTo(Vector3 dir)
        {
            return dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir.normalized) : Quaternion.identity;
        }

        private static Vector3 EndPos(SkillContext ctx, Vector3 origin)
        {
            return ctx.Targets.Count > 0 ? TargetPos(ctx.Targets[0]) : origin + ResolveDirection(ctx) * 5f;
        }

        private static Vector3 OriginPos(SkillContext ctx)
        {
            if (ctx.Caster != null)
                return ctx.Caster.MuzzlePosition;
            return ctx.OriginPosition + Vector3.up * CommonConfig.Skill.MuzzleHeight;
        }

        private static Vector3 TargetPos(Damageable d)
        {
            return d.transform.position + Vector3.up * CommonConfig.Skill.MuzzleHeight;
        }

        private static Vector3 ResolveDirection(SkillContext ctx)
        {
            if (ctx.Direction.sqrMagnitude > 0.0001f)
                return ctx.Direction.normalized;

            if (ctx.Targets.Count > 0 && ctx.Caster != null)
            {
                Vector3 d = ctx.Targets[0].transform.position - ctx.Caster.transform.position;
                if (d.sqrMagnitude > 0.0001f)
                    return d.normalized;
            }
            return ctx.Caster != null ? ctx.Caster.transform.forward : Vector3.right;
        }
    }
}
