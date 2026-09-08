using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem
{

    public static class TargetingResolver
    {
        private static readonly List<Damageable> _scratch = new List<Damageable>(64);
        private static readonly List<Damageable> _candidates = new List<Damageable>(64);

        public static void Resolve(SkillNodeData node, SkillLevelData level, SkillContext ctx)
        {
            ctx.Targets.Clear();
            if (node == null)
                return;

            ESkillTeam enemyTeam = SkillTeamUtil.Opposite(ctx.Caster != null ? ctx.Caster.Team : ESkillTeam.Friend);
            float range = node.GetFloat(ESkillParamKey.Range, level, 5f, ctx.Modifier);
            float rangeSq = range * range;

            switch (node.NodeType)
            {
                case ESkillNodeType.TargetSelf:
                {
                    Damageable self = ctx.Caster != null ? ctx.Caster.GetComponent<Damageable>() : null;
                    if (self != null)
                        ctx.Targets.Add(self);
                    return;
                }

                case ESkillNodeType.TargetNearest:
                {
                    Damageable best = FindNearest(ctx.OriginPosition, enemyTeam, rangeSq, out _);
                    if (best != null)
                        ctx.Targets.Add(best);
                    return;
                }

                case ESkillNodeType.TargetFarthest:
                {
                    Damageable best = FindFarthest(ctx.OriginPosition, enemyTeam, rangeSq);
                    if (best != null)
                        ctx.Targets.Add(best);
                    return;
                }

                case ESkillNodeType.TargetRandom:
                {
                    int max = node.GetInt(ESkillParamKey.MaxPerTarget, null, 1, ctx.Modifier);
                    GatherInRange(ctx.OriginPosition, enemyTeam, rangeSq, _candidates);
                    for (int i = 0; i < max && _candidates.Count > 0; i++)
                    {
                        int idx = Random.Range(0, _candidates.Count);
                        ctx.Targets.Add(_candidates[idx]);
                        _candidates.RemoveAt(idx);
                    }
                    return;
                }

                case ESkillNodeType.TargetAll:
                {
                    int max = node.GetInt(ESkillParamKey.MaxPerTarget, null, int.MaxValue, ctx.Modifier);
                    Damageable.GetAllOfTeam(enemyTeam, _candidates);
                    for (int i = 0; i < _candidates.Count && ctx.Targets.Count < max; i++)
                    {
                        ctx.Targets.Add(_candidates[i]);
                    }
                    return;
                }

                case ESkillNodeType.TargetNearestForward:
                {
                    Damageable best = FindNearest(ctx.OriginPosition, enemyTeam, rangeSq, out _);
                    if (best != null)
                    {
                        Vector3 d = best.transform.position - ctx.OriginPosition;
                        if (d.sqrMagnitude > 0.0001f)
                            ctx.Direction = d.normalized;
                        ctx.Targets.Add(best);
                    }
                    return;
                }

                case ESkillNodeType.TargetCone:
                {
                    float maxDist = node.GetFloat(ESkillParamKey.MaxDistance, level, range, ctx.Modifier);
                    float maxDistSq = maxDist * maxDist;
                    int max = node.GetInt(ESkillParamKey.MaxPerTarget, null, int.MaxValue, ctx.Modifier);
                    Damageable.GetAllOfTeam(enemyTeam, _candidates);
                    for (int i = 0; i < _candidates.Count && ctx.Targets.Count < max; i++)
                    {
                        Damageable d = _candidates[i];
                        Vector3 to = d.transform.position - ctx.OriginPosition;
                        if (to.sqrMagnitude > maxDistSq)
                            continue;
                        if (Vector3.Dot(to.normalized, ctx.Direction) < 0.7071f)
                            continue;
                        ctx.Targets.Add(d);
                    }
                    return;
                }
            }
        }

        private static Damageable FindNearest(Vector3 origin, ESkillTeam team, float maxSq, out float bestSq)
        {
            Damageable best = null;
            bestSq = float.MaxValue;
            Damageable.GetAllOfTeam(team, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
            {
                Damageable d = _scratch[i];
                float sq = PlanarDistSq(d.transform.position, origin);
                if (sq <= maxSq && sq < bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        private static Damageable FindFarthest(Vector3 origin, ESkillTeam team, float maxSq)
        {
            Damageable best = null;
            float bestSq = -1f;
            Damageable.GetAllOfTeam(team, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
            {
                Damageable d = _scratch[i];
                float sq = PlanarDistSq(d.transform.position, origin);
                if (sq <= maxSq && sq > bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        private static void GatherInRange(Vector3 origin, ESkillTeam team, float maxSq, List<Damageable> outList)
        {
            outList.Clear();
            Damageable.GetAllOfTeam(team, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
            {
                Damageable d = _scratch[i];
                if (PlanarDistSq(d.transform.position, origin) <= maxSq)
                    outList.Add(d);
            }
        }

        private static float PlanarDistSq(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
