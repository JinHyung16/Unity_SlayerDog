using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.Skill
{

    /// <summary>
    /// DB 트리의 Hit 노드에 대응하는 컴포넌트 — 판정·피해·소멸 조건만 책임진다.
    ///
    /// LateUpdate 에서 도는 이유: SkillMethod 가 Update 에서 이동을 끝낸 뒤여야
    /// 이번 프레임의 이동 구간(PrevPos → 현재 위치)으로 스윕 판정을 할 수 있다.
    ///
    /// 거리 판정은 전부 XZ 평면이다. 이 프로젝트의 충돌 모델이 XZ 2D 라서,
    /// 총구 높이(1.0) 때문에 지면의 적을 놓치는 일이 없어야 한다.
    /// </summary>
    public class SkillHit : BaseBehaviour
    {
        private SkillConfig _config;
        private SkillMethod _method;
        private SkillVisual _visual;

        private bool _oneShotDone;

        private static readonly List<Damageable> _scanBuf = new List<Damageable>(64);
        private static readonly List<Vector3> _chainPts = new List<Vector3>(8);

        private void Awake()
        {
            _config = GetComponent<SkillConfig>();
            _method = GetComponent<SkillMethod>();
            _visual = GetComponent<SkillVisual>();
        }

        /// <summary>즉발(LaunchInstant)처럼 스폰과 동시에 때려야 하는 경우 LaunchExecutor 가 부른다.</summary>
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

            // 곡사/사행은 도착 프레임에 판정을 마치고 사라진다
            if (_config.Despawning == false && _method != null && _method.Arrived)
            {
                _config.Despawn();
                return;
            }

            CheckTimedDespawn();
            CheckSafetyDespawn();
        }

        /// <summary>
        /// 「N번 맞히면 사라진다」 스킬이 **한 번도 못 맞혔을 때** 기다려 주는 시간.
        /// 연출이 다 재생될 만큼은 되고, 다음 발사와 겹치지 않을 만큼은 짧아야 한다.
        /// </summary>
        private const float NoHitGrace = 0.6f;

        /// <summary>
        /// 어떤 스킬 오브젝트도 이 시간을 넘겨 살아 있지 않는다. 마지막 안전망이다.
        /// 가장 긴 지속형 스킬보다 넉넉해야 정상 동작을 자르지 않는다.
        /// </summary>
        private const float MaxLifetime = 12f;

        /// <summary>
        /// [사고] 즉발 스킬은 `DespawnAfterHits: 1` 로 사라지는데, **맞춘 게 있어야** 펄스가 돈다.
        /// 사거리에 적이 없으면 펄스가 영영 안 돌아 연출이 화면에 그대로 남았고,
        /// 으르렁(3초 쿨)이 빈 벌판에서 쌓여 화면 위쪽을 이펙트로 뒤덮었다.
        /// 세로 화면비로 바꾸기 전까지는 이게 화면 밖이라 보이지도 않았다.
        ///
        /// 발사 방식마다 조건을 하나씩 막으면 같은 사고가 다른 방식에서 또 난다.
        /// 「못 맞혔으면 곧 사라진다」와 「무슨 일이 있어도 이 시간이면 사라진다」 두 겹으로 둔다.
        /// </summary>
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

            // 미리 받은 대상이 유효하지 않으면(시전자 자신 등) 주변에서 다시 찾는다.
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

            // [사고] 팀 검사가 「직접 훑어 모은」 경로에만 있고 **미리 받은 대상 목록**에는 없었다.
            // TargetSelf 는 시전 위치를 정하려고 시전자를 목록에 넣는데, 그게 그대로 피해 대상이 되어
            // 광역 스킬이 자기를 때렸다. 수치가 작던 동안은 묻혀 있다가 계수를 제대로 맞추자 즉사로 드러났다.
            // 검사는 여기 한 곳에만 둔다 — 경로마다 따로 두면 또 어느 하나가 빠진다.
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
            // [사고] 아군 오사. 호출처마다 팀 검사를 흩어 뒀더니 연쇄·폭발 경로가 시드 대상
            // (seedTargets[0])을 검사 없이 그대로 때렸고, 시전자가 시드에 섞이면
            // **자기 스킬 전량을 자기가 맞았다** — 라운드1 에서 한 방에 85 피해로 즉사.
            // 피해는 여기 한 곳으로만 나가므로, 검사도 여기 한 곳에 둔다.
            if (IsEnemyAlive(d) == false)
                return false;

            // rawDamage 는 절대 피해가 아니라 **계수(%)** 다. 공격력과 곱하고 100 으로 나눈다.
            // [사고] 이 곱을 빼먹어 데이터가 기획서의 1/10 로 동작했고, 라운드1 에서 적 한 마리를
            // 20초 걸려 잡았다. 「스킬이 약하다」로 보이지만 실제로는 공식이 빠진 것이었다.
            float power = _config.Caster != null ? _config.Caster.AttackPower : 100f;
            float outMul = _config.Caster != null ? _config.Caster.OutgoingDamageMultiplier : 1f;
            float scaled = rawDamage * 0.01f * power * outMul;

            // 대상 조건부 연계. 「둔화된 적에게 +35%」처럼 **맞는 쪽 상태**로 갈리므로
            // 파라미터 조회가 아니라 여기서 곱한다.
            SkillModifier mod = _config.Modifier;
            if (mod != null && mod.VsDebuffDamageMul > 0f && d.Status != null
                && d.Status.HasDebuffKind(mod.VsDebuff))
            {
                scaled *= 1f + mod.VsDebuffDamageMul;
            }
            bool died = d.TakeDamage(scaled, _config.Caster);

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
