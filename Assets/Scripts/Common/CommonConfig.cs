using UnityEngine;

namespace Jinhyeong_Common
{

    public static class CommonConfig
    {
        public static class Physics
        {
            public const float Gravity = -9.81f;
        }

        public static class Player
        {
            public const float Hp = 100f;
            public const float MoveSpeed = 5f;
            public const float TurnSpeed = 720f;

            public const float AttackDamage = 10f;
            public const float AttackRange = 2.2f;
            public const float AttackHalfAngleDeg = 60f;
            public const float AttackCooldown = 0.35f;

            public const float AttackPower = 100f;

            public const float AutoAttackRange = 2.2f;

            public const float HitInvulnerability = 0.5f;
        }

        public static class Enemy
        {
            public const float Hp = 50f;
            public const float MoveSpeed = 3.5f;
            public const float TurnSpeed = 360f;

            public const float AttackDamage = 5f;
            public const float AttackRange = 2.0f;
            public const float AttackHalfAngleDeg = 60f;
            public const float AttackCooldown = 1.0f;

            public const float DetectionRange = 60f;
            public const float LoseSightRange = 80f;
            public const float AIAttackRange = 1.9f;
            public const float AIAttackInterval = 1.0f;

            public const float StandoffDistance = 1.3f;
            public static readonly KeyCode AttackSkillKey = KeyCode.Alpha1;

            public const float FleeHpPercent = 0.2f;
            public const float FleeSpeedMultiplier = 1.3f;
            public const float FleeDuration = 2f;
            public const float FleeReengageTime = 3f;
            public const float FleeSafeDistance = 7f;

            public const float PatrolRadius = 5f;
            public const float PatrolArrivalDistance = 0.4f;
            public const float PatrolWaitTime = 1.5f;

            public const float ChaseSpeedMultiplier = 1.0f;
        }

        public static class Skill
        {
            public const float MuzzleHeight = 1.0f;

            public const string RootPrefabKey = "Skill_Root";

            public const string HitFxKey = "Fx_Hit";
            public const string ChainFxKey = "Fx_Chain";
            public const string XpGemKey = "Fx_XpGem";
            public const string DamageTextKey = "Fx_DamageText";
            // 스킬 적중 버스트(스킬 색 파편) · 이동형 스킬 잔상
            public const string HitBurstKey = "Fx_HitBurst";
            public const string GhostKey = "Fx_Ghost";
            public const int MaxBurstsPerFrame = 8;
            public const float GhostInterval = 0.045f;
            public const float GhostMinStep = 0.25f;
            public const float GhostLife = 0.22f;
            public const float GhostAlpha = 0.55f;
        }

        public static class Spawner
        {
            public const int InitialEnemyCount = 0;
            public const float InitialSpawnRadius = 7f;

            public const int PrewarmCount = 200;

            public const int MaxAlive = 300;

            public const int PrewarmHitFx = 120;
            public const int PrewarmDamageText = 120;
            public const int PrewarmXpGem = 200;
            public const int PrewarmPickup = 8;
            public const int PrewarmHitBurst = 60;
            public const int PrewarmGhost = 80;
        }

        public static class Combat
        {
            public const float KnockbackSpeed = 6f;
            public const float KnockbackDecay = 28f;

            public const float DamageTextLife = 0.6f;
            public const float DamageTextRise = 1.2f;
        }

        public static class Run
        {
            // 스피커가 기본 공격 앰프(고유 파츠 옵션)를 줄 확률 — 볼트 개많이잡아/개많이잡아 음악 전투 기획서 3절
            public const float AmpChance = 0.3f;
            // 스피커 간격 = 기본 + 장착 파츠 수 × 가산 (초). 목표: 6칸 완성이 4~5분 (볼트 개많이잡아/개많이잡아 음악 전투 기획서 6절)
            public const float DropIntervalBase = 14f;
            public const float DropIntervalPerPart = 6f;
            // 6칸이 다 찬 뒤 — 간격을 줄이고 앰프 비중을 올린다. QA1: 다 찬 뒤 56초 간격이라 성장이 멈춰 전원 라운드 8~10 사망
            public const float DropIntervalFull = 20f;
            public const float AmpChanceFull = 0.5f;

            public const float XpPerGem = 3f;

            public const float MagnetRadius = 2.5f;
            public const float MagnetAccel = 40f;
            public const float PickupDistance = 0.5f;
        }

        public static class Constellation
        {
            public const int Seed = 20260908;

            public static readonly float[] RingRadius = { 312f, 520f, 728f, 936f, 1144f };   // ×1.3 (10-04 간격 확대)

            public const float OrbitRadius = 86f;

            public const float SegmentLength = 120f;

            public const int InnerLeafCount = 3;
            public const int OuterLeafCount = 4;

            public const int CostSmall = 1;
            public const int CostLevel = 2;
            public const int CostNotable = 5;
            public const int CostSkill = 6;
            public const int CostGate = 4;
            public const int CostKeystone = 20;

            public const float CostGrowth = 1.4f;

            public const float LevelCostGrowth = 1.25f;

            public const int MaxNodeLevel = 4;

            public const float LevelChance = 0.55f;
            public const int LevelNodeMax = 24;

            public const float NeighborLinkChance = 0.75f;

            public const float ValueRingGain = 0.15f;

            public const int GateNeedClear = 1;

            public const float SpurStep = 143f;

            public const float MergeDistance = 57f;

            public const float AspectY = 0.74f;
        }

        public static class UI
        {
            public static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

            public const float MatchWidthOrHeight = 0.5f;
        }

        public static class SurvivorView
        {
            public const float CameraPitch = 50f;

            public const float CameraYaw = 0f;

            public const float CameraDistance = 19f;
            public const float CameraFov = 50f;

            public const float CameraOrthoSize = 8.85f;

            public const float SpawnRingRadius = 14f;

            public const float DespawnRadius = 30f;

            public const float OffscreenMargin = 3f;
        }
    }
}
