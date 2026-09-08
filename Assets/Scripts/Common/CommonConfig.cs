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

            /// <summary>
            /// 스킬 계수의 기준이 되는 공격력. 기획서의 모든 스킬 수치가 「공격력의 N%」라
            /// 이 값이 100 이어야 계수와 실제 피해가 1:1 로 읽힌다.
            /// </summary>
            public const float AttackPower = 100f;

            public const float AutoAttackRange = 2.2f;

            /// <summary>
            /// 피격 후 무적 시간(초). 뱀서류에서 이게 없으면 붙은 적 수만큼 초당 피격이 나와
            /// 어떤 수치도 밸런스가 안 잡힌다. [사고] 없을 때 라운드1에서 5.7초 만에 죽었다.
            /// </summary>
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

            // 뱀서류는 "항상 추격"이다. 화면 가장자리 스폰(14~20u)보다 훨씬 크게 잡아야
            // 스폰 직후부터 플레이어를 물고 들어온다. 8f 로 두면 가장자리에서 순찰만 하며 쌓인다.
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

            /// <summary>
            /// 스킬 로직 컴포넌트가 박혀 있는 루트 프리팹의 Addressable 주소.
            /// 아트는 이 프리팹의 자식으로 붙는다 — 아트 프리팹에는 스크립트가 없다.
            /// </summary>
            public const string RootPrefabKey = "Skill_Root";

            /// <summary>짧은 연출용 프리팹. 로직 컴포넌트가 박혀 있고 풀에서 재사용된다.</summary>
            public const string HitFxKey = "Fx_Hit";
            public const string ChainFxKey = "Fx_Chain";
            public const string XpGemKey = "Fx_XpGem";
            public const string DamageTextKey = "Fx_DamageText";
        }

        public static class Spawner
        {
            /// <summary>
            /// 런 시작 시 미리 뿌리는 적. 0 이 맞다 — 뱀서류는 적이 화면 밖에서 걸어 들어와야 하고,
            /// 시작부터 둘러싸여 있으면 첫 스킬 쿨다운이 돌기도 전에 죽는다.
            /// </summary>
            public const int InitialEnemyCount = 0;
            public const float InitialSpawnRadius = 7f;

            /// <summary>
            /// 미리 만들어두는 적 인스턴스 수. 뱀서류는 몬스터가 계속 쏟아지므로,
            /// 0으로 두면 플레이 중에 Instantiate 가 터져서 프레임이 끊긴다.
            /// </summary>
            public const int PrewarmCount = 200;

            /// <summary>동시에 살아 있을 수 있는 최대 적. 이 위로는 스폰을 멈춘다.</summary>
            public const int MaxAlive = 300;

            /// <summary>
            /// 런 시작 때 미리 만들어 둘 연출 개수.
            /// 피격 이펙트·데미지 텍스트는 한 프레임에 수십 개가 동시에 뜬다.
            /// 경험치 젬은 주울 때까지 남아 있어 더 많이 쌓인다.
            /// </summary>
            public const int PrewarmHitFx = 120;
            public const int PrewarmDamageText = 120;
            public const int PrewarmXpGem = 200;
            public const int PrewarmPickup = 8;
        }

        /// <summary>타격감 관련 표준값.</summary>
        public static class Combat
        {
            /// <summary>피격 시 밀려나는 초기 속도(유닛/초). 감쇠로 금방 멎는다.</summary>
            public const float KnockbackSpeed = 6f;
            public const float KnockbackDecay = 28f;

            public const float DamageTextLife = 0.6f;
            public const float DamageTextRise = 1.2f;
        }

        /// <summary>런(1회 플레이) 진행 관련.</summary>
        public static class Run
        {
            public const float XpPerGem = 3f;

            /// <summary>이 반경에 들어오면 젬이 플레이어에게 날아온다.</summary>
            public const float MagnetRadius = 2.5f;
            public const float MagnetAccel = 40f;
            public const float PickupDistance = 0.5f;
        }

        /// <summary>
        /// 성좌 스킬 트리의 «모양»과 «값». 노드는 데이터에 없다 — 이 상수들과 계열 표(ConstellationRegion)
        /// 로부터 시드 하나로 매번 똑같이 만들어진다. 노드 300개를 표로 들고 있으면 한 줄 고칠 때마다
        /// 표 전체를 다시 굽게 되고, 그러면 「모양을 조금 넓혀 보자」가 하루짜리 일이 된다.
        ///
        /// [주의] <see cref="Seed"/> 나 반지름을 바꾸면 노드 id 가 밀려 **이미 산 노드가 옮겨간다.**
        /// 세이브가 깔린 뒤에 건드릴 값이 아니다.
        /// </summary>
        public static class Constellation
        {
            public const int Seed = 20260908;

            /// <summary>링 반지름(그래프 유닛). 개수가 곧 링 수다.</summary>
            public static readonly float[] RingRadius = { 240f, 400f, 560f, 720f, 880f };

            /// <summary>성단 내부 궤도 반경. 이게 크면 성단끼리 겹친다.</summary>
            public const float OrbitRadius = 66f;

            /// <summary>길을 쪼개는 간격. 작을수록 잔가지 노드가 많아진다 — 노드 수를 좌우하는 값이다.</summary>
            public const float SegmentLength = 92f;

            /// <summary>성단 하나에 딸린 잔가지 수. 안쪽 링은 적게 둬서 초반이 답답하지 않게 한다.</summary>
            public const int InnerLeafCount = 3;
            public const int OuterLeafCount = 4;

            public const int CostSmall = 1;
            public const int CostLevel = 2;
            public const int CostNotable = 5;
            public const int CostSkill = 6;
            public const int CostGate = 4;
            public const int CostKeystone = 20;

            /// <summary>링이 하나 오를 때 비용 배수. 바깥이 비싸야 안쪽을 먼저 사는 이유가 생긴다.</summary>
            public const float CostGrowth = 1.4f;

            /// <summary>레벨 노드의 레벨당 비용 배수.</summary>
            public const float LevelCostGrowth = 1.25f;

            public const int MaxNodeLevel = 4;

            /// <summary>레벨 노드로 승격될 확률과 전체 상한. 상한이 없으면 바깥 링이 전부 레벨 노드가 된다.</summary>
            public const float LevelChance = 0.55f;
            public const int LevelNodeMax = 24;

            /// <summary>같은 링 이웃 성단을 잇는 확률. 1이면 링이 전부 닫히고, 0이면 순수 트리가 된다.</summary>
            public const float NeighborLinkChance = 0.75f;

            /// <summary>바깥 값 보정 — 링이 오를수록 잔가지 하나가 조금씩 세진다.</summary>
            public const float ValueRingGain = 0.15f;

            /// <summary>관문을 여는 데 필요한 클리어 횟수. 계열마다 하나씩 선다.</summary>
            public const int GateNeedClear = 1;

            /// <summary>키스톤까지 뻗는 가지의 마디 간격.</summary>
            public const float SpurStep = 110f;

            /// <summary>
            /// 이보다 가까우면 새 노드를 만들지 않고 «있는 노드에 잇는다».
            /// [사고] 길과 성단이 만나는 자리에 노드가 겹쳐 찍혀 화면에서 무엇을 누르는지 알 수 없었다.
            /// 겹침을 나중에 지우면 간선이 끊기므로, 만들 때 합치는 편이 안전하다 — 합쳐진 자리는 교차로가 된다.
            /// </summary>
            public const float MergeDistance = 44f;

            /// <summary>
            /// 세로 눌림. 성좌를 정원으로 두면 16:9 화면에서 «가로가 남고 세로가 모자란다» —
            /// 전체를 담으려면 세로에 맞춰야 하고, 그러면 별이 필요 이상으로 작아진다.
            /// 마지막에 Y 만 눌러 타원으로 만들면 같은 창에 별을 더 크게 담을 수 있다.
            /// </summary>
            public const float AspectY = 0.62f;
        }

        /// <summary>
        /// UI 캔버스 기준. **프로젝트당 한 번** 정하고 창마다 다르게 두지 않는다 (Platform.md).
        /// [사고] 로비만 PC 로 고치고 HUD 는 세로 기준으로 남아 있던 적이 있다 — 같은 화면에 두 기준이 섞이면
        /// 어느 쪽이 맞는지 볼 때마다 헷갈린다. 값은 여기 한 곳에서만 읽는다.
        /// </summary>
        public static class UI
        {
            public static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

            /// <summary>0 = 폭 기준, 1 = 높이 기준. 0.5 는 대각 보정 — 16:9 PC 에서 상하·좌우 어느 쪽도 잘리지 않게.</summary>
            public const float MatchWidthOrHeight = 0.5f;
        }

        /// <summary>세로 화면 서바이버 뷰 설정.</summary>
        public static class SurvivorView
        {
            // 살짝 쿼터뷰. 완전 탑다운(70°+)이면 캐릭터 정수리만 보이고,
            // 너무 낮추면(40° 미만) 세로 시야가 안 나와 위아래 적이 안 보인다.
            public const float CameraPitch = 50f;

            // Yaw 는 0 을 유지한다. 화면 위 = 월드 전방이라야 조이스틱 입력이 1:1 로 맞는다.
            // 정통 쿼터뷰처럼 45° 돌리면 조작 방향이 어긋나 손맛이 나빠진다.
            public const float CameraYaw = 0f;

            // 각도를 눕힌 만큼 지면이 덜 보이므로 거리로 보정한다.
            public const float CameraDistance = 19f;
            public const float CameraFov = 50f;

            // 2D 전환: 오쏘그래픽 세로 반높이. 기존 원근(거리 19 · FOV 50)이 만들던
            // 세로 시야 17.7유닛과 같은 화면이 나오도록 맞춘 값이다.
            public const float CameraOrthoSize = 8.85f;

            /// <summary>
            /// 스폰 링 반지름. 카메라에 안 잡히는 바깥이되 너무 멀면 안 된다.
            /// 실측(pitch 50 / dist 19 / fov 50 / 9:16)으로 보이는 지면이 대략 X 13 · Z 21 이라
            /// 화면 반경은 세로로 ~10.5. 링을 14 로 두면 화면 밖에서 생기되 금방 시야에 들어온다.
            /// 22 로 뒀더니 걸어 들어오는 데만 한참 걸려 화면이 비었다.
            /// </summary>
            public const float SpawnRingRadius = 14f;

            /// <summary>
            /// 이 거리를 넘고 **화면 밖일 때** 풀로 회수한다.
            /// 거리 단독으로 판단하면 쿼터뷰의 먼 쪽(화면 위)에 보이는 적이 눈앞에서 사라진다.
            /// </summary>
            public const float DespawnRadius = 30f;

            /// <summary>화면 밖 판정 여유. 화면 경계에서 바로 꺼지면 가장자리에서 깜빡인다.</summary>
            public const float OffscreenMargin = 3f;
        }
    }
}
