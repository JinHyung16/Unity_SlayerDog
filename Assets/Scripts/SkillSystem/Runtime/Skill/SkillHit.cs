using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.Skill
{

    public class SkillHit : BaseBehaviour
    {
        private SkillConfig _config;
        private SkillMethod _method;
        private SkillVisual _visual;

        private bool _oneShotDone;

        private static readonly List<Damageable> _scanBuf = new List<Damageable>(64);
        private static readonly List<Vector3> _chainPts = new List<Vector3>(8);

        // 하네스 계측: 이번 판에 적을 맞힌 스킬 / 버스트를 낸 스킬
        public static readonly HashSet<string> HitKeys = new HashSet<string>();
        public static readonly HashSet<string> BurstKeys = new HashSet<string>();

        public static void ResetBurstWatch()
        {
            HitKeys.Clear();
            BurstKeys.Clear();
        }

        private void Awake()
        {
            _config = GetComponent<SkillConfig>();
            _method = GetComponent<SkillMethod>();
            _visual = GetComponent<SkillVisual>();
        }

        public void ImmediateHit(IList<Damageable> seedTargets)
        {
            ProcessHit(immediate: true, seedTargets: seedTargets);
        }

        private void LateUpdate()
        {
            if (_config == null || _config.Despawning)
                return;

            if (_config.Armed)
                ProcessHit(immediate: false, seedTargets: null);

            _config.PrevPos = transform.position;

            if (_config.Despawning == false && _method != null && _method.Arrived)
            {
                _config.Despawn();
                return;
            }

            CheckTimedDespawn();
            CheckSafetyDespawn();
        }

        private const float NoHitGrace = 0.6f;

        private const float MaxLifetime = 12f;

        private void CheckSafetyDespawn()
        {
            if (_config.Despawning)
                return;

            float age = Time.time - _config.SpawnTime;

            SkillNodeData ds = _config.DespawnNode;
            bool byHits = ds != null && ds.NodeType == ESkillNodeType.DespawnAfterHits;
            if (byHits && _config.PulseCount == 0 && age >= NoHitGrace)
            {
                _config.Despawn();
                return;
            }

            if (age >= MaxLifetime)
                _config.Despawn();
        }

        private void ProcessHit(bool immediate, IList<Damageable> seedTargets)
        {
            SkillNodeData hit = _config.HitNode;
            if (hit == null)
                return;

            switch (hit.NodeType)
            {
                case ESkillNodeType.HitSingle:
                    ProcessSingle(seedTargets);
                    break;
                case ESkillNodeType.HitArea:
                    ProcessAoE(seedTargets);
                    break;
                case ESkillNodeType.HitBeam:
                    if (immediate == false)
                        ProcessBeamTick();
                    break;
                case ESkillNodeType.HitChain:
                    if (_oneShotDone == false)
                        ProcessBounce(seedTargets);
                    break;
                case ESkillNodeType.HitDeathBurst:
                    ProcessExplode(seedTargets);
                    break;
            }
        }

        private void ProcessSingle(IList<Damageable> seedTargets)
        {
            float radius = _config.GetHitFloat(ESkillParamKey.Radius, 0.5f);
            float damage = _config.GetHitFloat(ESkillParamKey.Damage, 1f);

            Damageable d = seedTargets != null && seedTargets.Count > 0 ? seedTargets[0] : null;
            if (IsEnemyAlive(d) == false)
                d = FindNearestEnemy(radius);

            if (TryDamageOnce(d, damage))
                BumpPulse();
        }

        private void ProcessAoE(IList<Damageable> seedTargets)
        {
            float radius = _config.GetHitFloat(ESkillParamKey.Radius, 0.5f);
            float damage = _config.GetHitFloat(ESkillParamKey.Damage, 1f);
            int maxPerPulse = _config.GetHitInt(ESkillParamKey.MaxPerTarget, int.MaxValue);

            int hits = 0;
            if (seedTargets != null)
            {
                for (int i = 0; i < seedTargets.Count && hits < maxPerPulse; i++)
                {
                    if (TryDamageOnce(seedTargets[i], damage))
                        hits++;
                }
            }

            if (hits < maxPerPulse)
            {
                GatherEnemiesInSweep(_config.PrevPos, transform.position, radius, _scanBuf);
                for (int i = 0; i < _scanBuf.Count && hits < maxPerPulse; i++)
                {
                    if (TryDamageOnce(_scanBuf[i], damage))
                        hits++;
                }
            }

            if (hits > 0)
                BumpPulse();
        }

        private void ProcessBeamTick()
        {
            float length = _config.GetHitFloat(ESkillParamKey.Length, 5f);
            float width = _config.GetHitFloat(ESkillParamKey.Width, 1f);
            float interval = _config.GetHitFloat(ESkillParamKey.DamageInterval, 0.5f);
            float damage = _config.GetHitFloat(ESkillParamKey.Damage, 1f);
            int maxPerTarget = _config.GetHitInt(ESkillParamKey.MaxPerTarget, int.MaxValue);

            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;
            forward.Normalize();

            float halfWidthSq = (width * 0.5f) * (width * 0.5f);
            float lengthSq = length * length;

            int ticked = 0;
            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (IsEnemyAlive(d) == false)
                    continue;

                Vector3 to = d.transform.position - origin;
                to.y = 0f;
                float along = Vector3.Dot(to, forward);
                if (along < 0f || along * along > lengthSq)
                    continue;
                if ((to - forward * along).sqrMagnitude > halfWidthSq)
                    continue;

                if (_config.LastHitTime.TryGetValue(d, out float lastT) && Time.time - lastT < interval)
                    continue;
                if (CountHitsOn(d) >= maxPerTarget)
                    continue;

                DealDamageTo(d, damage);
                _config.LastHitTime[d] = Time.time;
                ticked++;
            }

            if (ticked > 0)
                BumpPulse();
        }

        private void ProcessBounce(IList<Damageable> seedTargets)
        {
            float jumpRange = _config.GetHitFloat(ESkillParamKey.Range, 4f);
            float damage = _config.GetHitFloat(ESkillParamKey.Damage, 1f);
            int maxTargets = Mathf.Max(1, _config.HitNode.GetInt(ESkillParamKey.MaxBounces, _config.LevelData, 3, _config.Modifier));

            Damageable current = seedTargets != null && seedTargets.Count > 0
                ? seedTargets[0]
                : null;
            if (IsEnemyAlive(current) == false)
                current = FindNearestEnemy(jumpRange);

            if (current == null)
                return;

            _chainPts.Clear();
            Vector3 origin = _config.Caster != null ? _config.Caster.transform.position : transform.position;
            _chainPts.Add(origin + Vector3.up);

            DealDamageTo(current, damage);
            _config.LastHitTime[current] = Time.time;
            _chainPts.Add(current.transform.position + Vector3.up);

            for (int hit = 1; hit < maxTargets; hit++)
            {
                Damageable next = FindNearestUnchainedEnemy(current.transform.position, jumpRange);
                if (next == null)
                    break;
                DealDamageTo(next, damage);
                _config.LastHitTime[next] = Time.time;
                _chainPts.Add(next.transform.position + Vector3.up);
                current = next;
            }

            if (_visual != null)
                _visual.ShowChain(_chainPts);

            _oneShotDone = true;
            BumpPulse();
        }

        private void ProcessExplode(IList<Damageable> seedTargets)
        {
            float radius = _config.GetHitFloat(ESkillParamKey.Radius, 2f);
            float explodeRadius = _config.GetHitFloat(ESkillParamKey.Range, radius);
            float damage = _config.GetHitFloat(ESkillParamKey.Damage, 1f);

            Damageable primary = seedTargets != null && seedTargets.Count > 0
                ? seedTargets[0]
                : null;
            if (IsEnemyAlive(primary) == false)
                primary = FindNearestEnemy(radius);

            if (primary == null)
                return;
            if (_config.LastHitTime.ContainsKey(primary))
                return;

            Vector3 dyingPos = primary.transform.position;
            bool died = DealDamageTo(primary, damage);
            _config.LastHitTime[primary] = Time.time;

            if (died)
            {
                GatherEnemiesInRadius(dyingPos, explodeRadius, _scanBuf);
                for (int i = 0; i < _scanBuf.Count; i++)
                {
                    Damageable d = _scanBuf[i];
                    if (d == primary)
                        continue;
                    if (_config.LastHitTime.ContainsKey(d))
                        continue;
                    DealDamageTo(d, damage);
                    _config.LastHitTime[d] = Time.time;
                }
            }

            BumpPulse();
        }

        private void BumpPulse()
        {
            _config.PulseCount++;

            SkillNodeData ds = _config.DespawnNode;
            if (ds == null || ds.NodeType != ESkillNodeType.DespawnAfterHits)
                return;
            int max = Mathf.Max(1, ds.GetInt(ESkillParamKey.Value, null, 1, _config.Modifier));
            if (_config.PulseCount >= max)
                _config.Despawn();
        }

        private void CheckTimedDespawn()
        {
            SkillNodeData ds = _config.DespawnNode;
            if (ds == null || ds.NodeType != ESkillNodeType.DespawnAfterTime)
                return;
            float duration = ds.GetFloat(ESkillParamKey.Value, null, 1f, _config.Modifier);
            if (Time.time - _config.SpawnTime >= duration)
                _config.Despawn();
        }

        private bool TryDamageOnce(Damageable d, float damage)
        {
            if (d == null || d.IsAlive == false)
                return false;

            if (IsEnemyAlive(d) == false)
                return false;
            if (_config.LastHitTime.ContainsKey(d))
                return false;
            DealDamageTo(d, damage);
            _config.LastHitTime[d] = Time.time;
            return true;
        }

        private bool DealDamageTo(Damageable d, float rawDamage)
        {
            if (IsEnemyAlive(d) == false)
                return false;

            float power = _config.Caster != null ? _config.Caster.AttackPower : 100f;
            float outMul = _config.Caster != null ? _config.Caster.OutgoingDamageMultiplier : 1f;
            float scaled = rawDamage * 0.01f * power * outMul;

            SkillModifier mod = _config.Modifier;
            if (mod != null && mod.VsDebuffDamageMul > 0f && d.Status != null
                && d.Status.HasDebuffKind(mod.VsDebuff))
            {
                scaled *= 1f + mod.VsDebuffDamageMul;
            }
            bool died = d.TakeDamage(scaled, _config.Caster);

            // 적중 버스트 — 스킬 색 파편. 플레이어 쪽 피격 스파크는 Damageable 이 낸다
            string key = _config.PoolKey;
            HitKeys.Add(key);
            Color tint = _visual != null ? _visual.TintColor : Color.white;
            if (HitEffect.SpawnBurst(d.transform.position + Vector3.up * CommonConfig.Skill.MuzzleHeight, tint))
                BurstKeys.Add(key);

            for (int i = 0; i < _config.DebuffNodes.Count; i++)
            {
                int debuffId = _config.DebuffNodes[i].GetInt(ESkillParamKey.DebuffId, null, 0);
                if (debuffId <= 0)
                    continue;
                SkillDebuffData data = SkillBuffRegistry.GetDebuff(debuffId);
                if (data == null)
                {
                    Debug.LogWarning($"[SkillHit] DebuffId={debuffId} not in registry");
                    continue;
                }
                d.Status.ApplyDebuff(data, _config.Caster);
            }
            return died;
        }

        private int CountHitsOn(Damageable d)
        {
            return _config.LastHitTime.ContainsKey(d) ? 1 : 0;
        }

        private bool IsEnemyAlive(Damageable d)
        {
            return d != null && d.IsAlive && d.Team == _config.EnemyTeam;
        }

        private Damageable FindNearestEnemy(float maxRadius)
        {
            float maxSq = maxRadius * maxRadius;
            Damageable best = null;
            float bestSq = float.MaxValue;
            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (IsEnemyAlive(d) == false)
                    continue;
                float sq = SweepPlanarDistSq(_config.PrevPos, transform.position, d.transform.position);
                if (sq <= maxSq && sq < bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        private Damageable FindNearestUnchainedEnemy(Vector3 from, float maxRadius)
        {
            float maxSq = maxRadius * maxRadius;
            Damageable best = null;
            float bestSq = float.MaxValue;
            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (IsEnemyAlive(d) == false)
                    continue;
                if (_config.LastHitTime.ContainsKey(d))
                    continue;
                float sq = PointPlanarDistSq(from, d.transform.position);
                if (sq <= maxSq && sq < bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        private void GatherEnemiesInRadius(Vector3 origin, float radius, List<Damageable> outList)
        {
            outList.Clear();
            float rSq = radius * radius;
            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (IsEnemyAlive(d) == false)
                    continue;
                if (PointPlanarDistSq(d.transform.position, origin) <= rSq)
                    outList.Add(d);
            }
        }

        private void GatherEnemiesInSweep(Vector3 segStart, Vector3 segEnd, float radius, List<Damageable> outList)
        {
            outList.Clear();
            float rSq = radius * radius;
            IReadOnlyList<Damageable> all = Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Damageable d = all[i];
                if (IsEnemyAlive(d) == false)
                    continue;
                if (SweepPlanarDistSq(segStart, segEnd, d.transform.position) <= rSq)
                    outList.Add(d);
            }
        }

        private static float PointPlanarDistSq(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static float SweepPlanarDistSq(Vector3 segStart, Vector3 segEnd, Vector3 p)
        {
            float ax = segStart.x, az = segStart.z;
            float abx = segEnd.x - ax, abz = segEnd.z - az;
            float ab2 = abx * abx + abz * abz;
            if (ab2 < 1e-6f)
            {
                float ddx = p.x - ax;
                float ddz = p.z - az;
                return ddx * ddx + ddz * ddz;
            }
            float t = ((p.x - ax) * abx + (p.z - az) * abz) / ab2;
            t = Mathf.Clamp01(t);
            float ex = p.x - (ax + abx * t);
            float ez = p.z - (az + abz * t);
            return ex * ex + ez * ez;
        }

        protected override void OnDisabled()
        {
            _oneShotDone = false;
        }
    }
}
