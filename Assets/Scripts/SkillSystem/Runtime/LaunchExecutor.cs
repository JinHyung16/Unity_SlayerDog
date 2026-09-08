using UnityEngine;
using Jinhyeong_GameData;
using Jinhyeong_Managers;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem.Skill;

namespace Jinhyeong_SkillSystem
{

    /// <summary>
    /// Launch 노드를 실제 스폰으로 옮기는 곳.
    ///
    /// 풀에서 나오는 오브젝트는 Skill_Root 프리팹이고 SkillConfig/SkillMethod/SkillHit/SkillVisual 이
    /// 이미 붙어 있다. 런타임 AddComponent 도, 프리미티브+new Material 폴백도 없다.
    /// </summary>
    public static class LaunchExecutor
    {
        public static bool Execute(SkillNodeData launchNode, SkillContext ctx)
        {
            if (launchNode == null)
                return false;

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

            // 곡사는 착탄해야 터진다. armed:false 로 띄워서 비행 중에 스친 적에게 폭발하지 않게 한다.
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

            // 눕힐지 세울지는 **그림이 안다**. 데이터(visual 모듈)가 들고 온 값을 그대로 쓴다.
            //
            // [사고] 예전엔 「자기버프면 세우고 나머지는 눕힌다」로 여기서 추측했다.
            // 그래서 정면에서 본 그림(발바닥 · 뼈 · 이빨 · 원반)이 전부 눕혀져 원근으로 찌그러졌고,
            // 화면에서 무엇이 나갔는지 읽히지 않았다. 스킬의 종류와 그림의 성질은 다른 축이다 —
            // 자기버프가 아니어도 세워야 읽히는 그림이 있고, 그 반대도 있다.
            visual.SetOrient(ParseOrient(launchNode.GetString(ESkillParamKey.VisualOrient)));

            visual.ApplyTint(launchNode.GetString(ESkillParamKey.VisualColor));
            visual.ApplyHitScale();
            visual.Play();

            return config;
        }

        /// <summary>
        /// 데이터의 방향 표기를 런타임 모드로 옮긴다.
        /// 값이 비었거나 모르는 문자열이면 Ground — 예전 기본값이라 바뀌어도 놀랄 일이 없다.
        /// </summary>
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
