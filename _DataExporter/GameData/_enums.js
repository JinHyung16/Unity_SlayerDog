/**
 * enum 원본 (SSOT).
 *
 * 여기가 유일한 정의처다. 빌드하면 C# 로 GameEnum.Generated.cs 가 나온다.
 * 'None' 은 파싱 실패 폴백이라 익스포터가 항상 0번에 넣는다 (여기 안 적어도 됨).
 *
 * 이 목록 = 런타임 엔진이 실제로 실행할 수 있는 노드/파라미터의 전부다.
 * 여기에 없는 걸 DB 에 쓰면 빌드가 깨진다. 새 동작이 필요하면 여기 추가 + C# 노드 구현이 세트.
 */
module.exports = {
    // 조합의 축. 연출 색과 별개로, **플레이 감각**이 같은 스킬을 묶는다.
    ESkillFamily: [
        'Wave',
        'Throw',
        'Field',
        'Melee',
        'Gaze',
        'Aura'
    ],

    // 파츠가 붙는 몸 부위. 부위당 파츠 4종이고, 캐릭터 고유 파츠가 한 부위를 잠근다.
    EPartSlot: [
        'Mouth',
        'Feet',
        'Tail',
        'Head',
        'Neck',
        'Back'
    ],

    // 옵션 1개가 올리는 특성. 옵션 1개 = 특성 1개만 - 복합 효과는 데이터로 못 만든다.
    EPartTrait: [
        'Damage',
        'Cooldown',
        'CritChance',
        'CritDamage',
        'Area',
        'Count',
        'Duration',
        // 생존 축. [사고] 이게 없을 때 성장이 전부 공격이라 12분을 버틸 방법이 없었고,
        // 라운드 2(116초)에서 곡선이 끊겼다. 스킬별 보정이 아니라 캐릭터에 직접 걸린다.
        'MaxHp',
        'Lifesteal',
        'MoveSpeed',
        'Unique'
    ],

    ESkillTriggerType: [
        'OnTick',
        'OnAttack',
        'OnOreBreak'
    ],

    ESkillNodeType: [
        // 흐름 제어
        'Sequence',
        'Selector',
        'Parallel',
        'Inverter',
        'Cooldown',
        'Chance',

        // 발동 조건
        'TriggerOnTick',
        'TriggerOnAttack',
        'TriggerOnOreBreak',  // 미구현 (config.UnimplementedNodes)

        // 대상 선정
        'TargetSelf',
        'TargetNearest',
        'TargetFarthest',
        'TargetRandom',
        'TargetAll',
        'TargetNearestForward',
        'TargetCone',

        // 발사 방식
        'LaunchInstant',      // 대상 위치에 즉시 생성
        'LaunchStraight',     // 직선
        'LaunchArc',          // 포물선 (발사 시점 착탄지 고정)
        'LaunchCurve',        // 좌우로 흔들리며 진행 (발사 시점 착탄지 고정)
        'LaunchHoming',       // 유도 — 대상을 계속 추적

        // 소멸 조건
        'DespawnAfterTime',
        'DespawnAfterHits',
        'DespawnAfterBounces',   // 미구현 (config.UnimplementedNodes)
        'DespawnOnWall',         // 미구현 (config.UnimplementedNodes)

        // 타격
        'HitSingle',
        'HitArea',
        'HitBeam',
        'HitChain',
        'HitDeathBurst',

        // 상태 변화
        'BuffSelf',
        'DebuffOnHit'
    ],

    ESkillParamKey: [
        'Cooldown',
        'Chance',
        'Threshold',
        'Range',
        'Index',
        'HalfAngle',
        'Angle',
        'Absolute',
        'Damage',
        'Radius',
        'Length',
        'Width',
        'DamageInterval',
        'MaxPerTarget',
        'Value',
        'Speed',
        'MaxDistance',
        'TurnRate',
        'Duration',
        'ArcHeight',
        'MaxBounces',
        'Visual',
        // 이펙트 색. 그림은 흰색 한 벌이고 색만 스킬마다 다르다 -
        // 같은 그림을 계열색으로 물들여 쓰면 스프라이트가 몇 배로 늘지 않는다.
        'VisualColor',
        // 이펙트를 바닥에 눕힐지, 카메라를 향해 세울지.
        //
        // [사고] 「자기버프면 세우고 나머지는 눕힌다」로 클라가 추측하고 있었다.
        // 그래서 정면에서 본 그림(발바닥 · 뼈 · 이빨)이 전부 눕혀져 원근으로 찌그러졌고,
        // 화면에서 무엇이 나갔는지 읽히지 않았다.
        // 기준은 스킬의 종류가 아니라 **그림의 성질**이다 —
        //   Ground    방향 · 범위가 정보인 그림 (부채꼴 · 빔 · 장판 · 먼지)
        //   Billboard 정체성이 정보인 그림 (발바닥 · 뼈 · 원반 · 공 · 슬리퍼)
        // 그림이 아는 것이므로 visual 모듈이 들고 있어야 한다.
        'VisualOrient',
        'RequireAll',
        'ModifierKey',
        'BuffId',
        'DebuffId',
        // 연발 — 한 번 발동에 N 발. 간격(초) 0 이면 동시, 각도(도) 0 이면 같은 방향. 개수 증강(CountAdd)이 BurstCount 에 더해진다
        'BurstCount',
        'BurstInterval',
        'BurstSpread'
    ],

    // 같은 효과가 다시 걸렸을 때의 처리. 데이터로 정해야 화상=갱신 / 중독=중첩 같은 게
    // C# 수정 없이 나온다.
    EStatusStackMode: [
        'Refresh',   // 지속시간만 초기화 (기본)
        'Stack',     // maxStacks 까지 개별 인스턴스로 누적
        'Ignore'     // 이미 걸려 있으면 무시
    ],

    EBuffType: [
        'DamageBoost',
        'AttackSpeedBoost',
        'HealOverTime',
        'Shield',
        'MoveSpeedBoost',
        'MaxHpBoost'
    ],

    // 레벨업 증강의 종류
    EDebuffType: [
        'DamageOverTime',
        'SlowReduce',
        'DamageReceiveIncrease',
        'Stun'
    ]
};
