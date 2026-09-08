/**
 * 파츠 DB 스캐폴더.
 *
 * 파츠 24종과 옵션 90여 종은 (파츠 x 특성) 조합이라 손으로 적으면 오타가 런타임까지 산다.
 * 아래 스펙 표 하나를 입력으로 GameData 밑에 실제 .js 파일들을 찍어낸다.
 *
 * 찍어낸 .js 가 DB 본체다 — 수치를 고칠 땐 그 파일을 직접 고치면 된다.
 * 이 스크립트를 다시 돌리면 덮어쓰므로, 표 자체를 고쳐서 다시 돌리는 쪽을 권한다.
 *
 * 실행: node tools/gen_parts.js
 */
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', 'GameData');

// ── 특성 정의 ──────────────────────────────────────────────
// 증강 1개 = 특성 1개만. 복합 효과는 만들지 않는다.
const TRAITS = {
    Damage:     { idx: 1, value: 0.30, max: 3, suffix: '강화', desc: v => `피해량 +${Math.round(v * 100)}%` },
    Cooldown:   { idx: 2, value: 0.10, max: 3, suffix: '가속', desc: v => `쿨타임 -${Math.round(v * 100)}%` },
    CritChance: { idx: 3, value: 0.10, max: 2, suffix: '예리', desc: v => `치명타 확률 +${Math.round(v * 100)}%p` },
    CritDamage: { idx: 4, value: 0.25, max: 2, suffix: '급소', desc: v => `치명타 피해량 +${Math.round(v * 100)}%p` },
    Area:       { idx: 5, value: 0.20, max: 2, suffix: '확장', desc: v => `범위 +${Math.round(v * 100)}%` },
    Count:      { idx: 6, value: 1,    max: 2, suffix: '증식', desc: () => '개수 +1' },
    Duration:   { idx: 7, value: 0.25, max: 2, suffix: '연장', desc: v => `지속시간 +${Math.round(v * 100)}%` },
    Unique:     { idx: 8, value: 0,    max: 2, suffix: '고유', desc: () => '' },

    // 생존 축. 스킬이 아니라 **몸**에 걸린다 — PartLoadout.ApplyBodyTraits 가 한 번만 적용한다.
    // 이게 없으면 성장이 전부 공격이라 12분을 버틸 방법이 없다.
    MaxHp:      { idx: 9,  value: 0.15, max: 2, suffix: '튼튼', desc: v => `최대 체력 +${Math.round(v * 100)}%` },
    Lifesteal:  { idx: 10, value: 1.4,  max: 3, suffix: '흡혈', desc: v => `처치 시 체력 +${v}` },
    MoveSpeed:  { idx: 11, value: 0.06, max: 2, suffix: '날쌘', desc: v => `이동속도 +${Math.round(v * 100)}%` }
};

/**
 * 계열이 가져가는 생존 방식. 계열 선택이 곧 「어떻게 살아남을지」가 되도록 하나씩만 준다.
 * 전 파츠에 3종을 다 열면 생존이 운에 달리고, 계열의 성격도 사라진다.
 */
const FAMILY_SURVIVAL = {
    Melee: ['Lifesteal', 'MaxHp'],      // 물고 뜯으며 버틴다
    Gaze:  ['Lifesteal', 'MoveSpeed'],  // 한 방이 크고, 거리를 유지한다
    Aura:  ['Lifesteal', 'MaxHp'],      // 자기버프 계열은 몸으로 버틴다
    Wave:  ['Lifesteal', 'MaxHp'],      // 붙은 적을 쓸어내려면 맞으면서 써야 한다
    Field: ['Lifesteal', 'MoveSpeed'],  // 달려야 장판이 깔린다
    Throw: ['Lifesteal', 'MoveSpeed']   // 거리를 유지하는 것이 곧 방어다
};

// ── 연출 모듈 (색 + 이펙트 키) ─────────────────────────────
// 이펙트 스프라이트는 흰색 한 벌만 반입했고 색은 런타임 틴트다. color 가 그 틴트값.
// 연출은 **스킬 이름과 맞아야 한다**. 예전엔 9종을 돌려써서 「물기」와 「으르렁」과 「하품」이
// 전부 같은 음파 그림으로 나갔다 — 이름이 붙은 스킬은 그 이름이 화면에 보여야 한다.
// Fx_* 중 fx_teeth ~ fx_dogdust 는 build_skillfx.py 로 직접 그린 것들이다.
const VISUALS = {
    bite:   { color: '#FFF3E0', desc: '물기',       P: 'Fx_Teeth',    I: 'Fx_Teeth',     C: 'Fx_Paw' },
    growl:  { color: '#FFB86B', desc: '으르렁',     P: 'Fx_RingSpike',I: 'Fx_RingSpike', C: 'Fx_Growl' },
    bark:   { color: '#7FD8FF', desc: '짖기',       P: 'Fx_Bark',     I: 'Fx_Bark',      C: 'Fx_Bark' },
    yawn:   { color: '#C8B6FF', desc: '하품',       P: 'Fx_Yawn',     I: 'Fx_Yawn',      C: 'Fx_Yawn' },
    sonar:  { color: '#9FE8FF', desc: '귀 쫑긋',    P: 'Fx_Sonar',    I: 'Fx_Sonar',     C: 'Fx_Sonar' },
    howl:   { color: '#8FB8FF', desc: '울음',       P: 'Fx_Howl',     I: 'Fx_Howl',      C: 'Fx_Howl' },
    claw:   { color: '#FFFFFF', desc: '발톱',       P: 'Fx_Claw3',    I: 'Fx_Claw3',     C: 'Fx_Paw' },
    tail:   { color: '#FFE0B0', desc: '꼬리',       P: 'Fx_Crescent', I: 'Fx_Crescent',  C: 'Fx_Crescent' },
    bone:   { color: '#FFF6E0', desc: '뼈다귀',     P: 'Fx_Bone',     I: 'Fx_Explosion', C: 'Fx_Bone' },
    kibble: { color: '#FFA23C', desc: '사료',       P: 'Fx_Kibble',   I: 'Fx_TsExplosion', C: 'Fx_Kibble' },
    frisbee:{ color: '#FF9CD2', desc: '원반',       P: 'Fx_Frisbee',  I: 'Fx_Frisbee',   C: 'Fx_Frisbee' },
    ball:   { color: '#D8FF6B', desc: '테니스공',   P: 'Fx_Ball',     I: 'Fx_Hit',       C: 'Fx_Ball' },
    slipper:{ color: '#FFD08A', desc: '슬리퍼',     P: 'Fx_Slipper',  I: 'Fx_Paw',       C: 'Fx_Slipper' },
    bread:  { color: '#FFE9B0', desc: '식빵',       P: 'Fx_Bread',    I: 'Fx_Bread',     C: 'Fx_Bread' },
    spark:  { color: '#9FD8FF', desc: '정전기',     P: 'Fx_Static',   I: 'Fx_Static',    C: 'Fx_Static' },
    scent:  { color: '#A8E86B', desc: '냄새',       P: 'Fx_Scent',    I: 'Fx_Scent',     C: 'Fx_Scent' },
    beam:   { color: '#FF6A6A', desc: '눈빛 빔',    P: 'Fx_EyeBeam',  I: 'Fx_EyeBeam',   C: 'Fx_EyeBeam' },
    gaze:   { color: '#C89AFF', desc: '관통 시선',  P: 'Fx_Gaze',     I: 'Fx_Gaze',      C: 'Fx_Gaze' },
    shield: { color: '#FFE68A', desc: '보호막',     P: 'Fx_RingDense',I: 'Fx_RingDense', C: 'Fx_RingDense' },
    awaken: { color: '#FF9A5A', desc: '각성',       P: 'Fx_Swirl',    I: 'Fx_Swirl',     C: 'Fx_Swirl' },
    zoom:   { color: '#9CE8FF', desc: '질주',       P: 'Fx_Zoom',     I: 'Fx_Zoom',      C: 'Fx_Zoom' },
    taunt:  { color: '#C0C8D8', desc: '도발',       P: 'Fx_Taunt',    I: 'Fx_Taunt',     C: 'Fx_Taunt' },
    dust:   { color: '#C9A87C', desc: '흙먼지',     P: 'Fx_TsDust',   I: 'Fx_TsDust',    C: 'Fx_TsDust' }
};

// ── 적 ─────────────────────────────────────────────────────
const ENEMIES = [
    { id: 5001, name: 'Grunt',   disp: '꼬물이', sprite: 'grunt',   hp: 50,  spd: 3.2, dmg: 6, scale: 1.0,  color: '#FFFFFF', xp: 3,  w: 60, t: 0,   desc: '기본 — 물량의 기준' },
    { id: 5002, name: 'Runner',  disp: '쌩쌩이', sprite: 'runner',  hp: 30,  spd: 5.2, dmg: 5,  scale: 0.95, color: '#FFFFFF', xp: 4,  w: 25, t: 45,  desc: '빠르고 약함 — 뒤를 잡는다' },
    { id: 5003, name: 'Brute',   disp: '우락이', sprite: 'brute',   hp: 200, spd: 2.0, dmg: 14, scale: 1.35, color: '#FFFFFF', xp: 12, w: 12, t: 120, desc: '느리고 튼튼 — 단일 딜 검증용' },
    { id: 5004, name: 'Spitter', disp: '침뱉이', sprite: 'spitter', hp: 80,  spd: 2.5, dmg: 9, scale: 1.05, color: '#FFFFFF', xp: 10, w: 15, t: 240, desc: '거리를 둔다 — 근접 빌드 견제' },
    { id: 5005, name: 'Swarm',   disp: '바글이', sprite: 'swarm',   hp: 20,  spd: 4.0, dmg: 3,  scale: 0.75, color: '#FFFFFF', xp: 2,  w: 30, t: 360, desc: '다수 — 광역 없으면 파묻힌다' },
    // 보스 전용. w: 0 이라 가중치 추첨에서 빠진다 — 잡졸로는 절대 안 나오고 웨이브가 id 로 직접 부른다.
    // 우락이를 3배로 키워 쓰던 것을 대체한다. 하나뿐인 개체는 제 모습이 있어야 보스로 읽힌다.
    { id: 5006, name: 'MotherShip', disp: '납치선', sprite: 'ufo', hp: 240, spd: 2.6, dmg: 16, scale: 1.0, color: '#FFFFFF', xp: 200, w: 0, t: 99999, desc: '보스 — 라운드12 납치선' }
];

// ── 웨이브 ─────────────────────────────────────────────────
const WAVES = [
    { id: 6001, name: 'EliteRush', desc: '정예 러시 — 라운드4부터 45초마다', time: 180, repeat: 45, count: 4, hp: 4, dmg: 1.5, scale: 1.3, xp: 5, banner: '개쎈 외계인이 몰려옵니다!' },
    // [사고] 타입을 안 정하면 보스가 그때 등장 가능한 적 중 아무거나로 뽑힌다.
    // 꼬물이(50)로 나올 때와 우락이(200)로 나올 때 체력이 4배 차이라 보스전이 매판 다른 게임이 된다.
    // 납치선 고정 — 느리고 튼튼해야 「피하면서 깎는」 보스전이 성립한다.
    { id: 6002, name: 'Boss',      desc: '보스 납치선 — 라운드12 시작', time: 660, repeat: 0, count: 1, hp: 4.5, dmg: 1.0, scale: 1.7, xp: 1, spd: 1.6, banner: '납치선 강림!', boss: 1, type: 5006 }
];


// ── 라운드 ─────────────────────────────────────────────────
// 1분 x 12라운드. 12라운드에 보스가 나오고 처치하면 클리어.
// 난이도는 **물량**으로 올린다 — 체력을 가파르게 올리면 후반이 「안 죽는 적」이 되어 체감만 나빠진다.
const ROUNDS = [
    // [실측] 곡선을 세 번 헛짚었다.
    //   세 번째 — 플레이어 중복 스폰을 고쳐 화력이 절반이 된 직후, 피해 +15% 와 회복 삭감을
    //   **동시에** 얹었더니 라운드 7 에서 죽었다. 큰 변수가 하나 바뀐 직후에는 한 번에 하나만 만진다.
    // [실측] 곡선을 두 번 헛짚었다.
    //   완만할 때 — 라운드 9~12 가 97~100% 로 무풍이었다
    //   가파를 때 — 라운드당 HP 35% 씩 빠져 9~10 이 절벽이 됐고, 클리어가 파츠 뽑기 운에 갈렸다
    // 목표는 그 사이다: 후반에 **라운드당 20% 안팎**으로 깎여, 잘 굴린 판은 보스를 보고
    // 헐겁게 굴린 판은 10~11 에서 끝난다.
    { r: 1,  gap: 1.6,  batch: 1, alive: 18,  hp: 1.0,  dmg: 0.6,  banner: '' },
    { r: 2,  gap: 1.4,  batch: 1, alive: 24,  hp: 1.1,  dmg: 0.7,  banner: '' },
    { r: 3,  gap: 1.2,  batch: 1, alive: 32,  hp: 1.2,  dmg: 0.9,  banner: '' },
    { r: 4,  gap: 1.0,  batch: 2, alive: 42,  hp: 1.35, dmg: 1.1,  banner: '개쎈 외계인 출현!' },
    { r: 5,  gap: 0.9,  batch: 2, alive: 52,  hp: 1.5,  dmg: 1.4,  banner: '' },
    { r: 6,  gap: 0.8,  batch: 2, alive: 64,  hp: 1.7,  dmg: 1.7,  banner: '' },
    { r: 7,  gap: 0.7,  batch: 3, alive: 76,  hp: 1.9,  dmg: 2.0,  banner: '' },
    { r: 8,  gap: 0.65, batch: 3, alive: 88,  hp: 2.1,  dmg: 2.45, banner: '' },
    { r: 9,  gap: 0.6,  batch: 3, alive: 98,  hp: 2.4,  dmg: 2.75, banner: '' },
    { r: 10, gap: 0.55, batch: 4, alive: 108, hp: 2.7,  dmg: 3.1,  banner: '' },
    { r: 11, gap: 0.55, batch: 3, alive: 108, hp: 3.1,  dmg: 3.5,  banner: '납치선이 다가온다...' },
    // 보스 라운드는 잡졸을 거의 멈춘다. 잡졸에 딜이 다 새면 보스를 잡을 수가 없다.
    { r: 12, gap: 2.5,  batch: 1, alive: 30,  hp: 3.6,  dmg: 4.0,  banner: '납치선 강림!' }
];


// ── 계열 ───────────────────────────────────────────────────
// 조합의 축. 연출 색(visual)과 별개다 — 「정전기 털」이 파동인 건 자기중심으로 퍼지는
// **플레이 감각**이 같아서지 소리라서가 아니다.
const FAMILY = {
    // 파동 — 자기중심 광역 + 둔화
    bark_cone: 'Wave', growl_aoe: 'Wave', yawn: 'Wave',
    static_fur: 'Wave', ear_sonar: 'Wave', pack_chain: 'Wave',
    // 투척 — 원거리, 안전
    kibble_rain: 'Throw', kibble_bomb: 'Throw', frisbee: 'Throw',
    tennis_ball: 'Throw', slipper: 'Throw',
    // 장판 — 지나간 자리로 딜
    dig_roll: 'Field', bone_mine: 'Field', tail_copter: 'Field', afterimage_dash: 'Field',
    // 근접 — 붙어야 세다
    bite_heal: 'Melee', claw_volley: 'Melee', tail_pierce: 'Melee',
    tail_whip: 'Melee', bread_slice: 'Melee',
    // 시선 — 단일 고위력
    eye_beam: 'Gaze', scent_homing: 'Gaze', third_eye: 'Gaze',
    // 각성 — 자기버프
    zoomies: 'Aura', alpha_mode: 'Aura', fur_barrier: 'Aura', ignore_taunt: 'Aura'
};

const FAMILY_KR = {
    Wave: '파동', Throw: '투척', Field: '장판',
    Melee: '근접', Gaze: '시선', Aura: '각성'
};

// ── 계열 시너지 ────────────────────────────────────────────
// 같은 계열을 2개 모으면 1단계, 4개 모으면 2단계가 **자동으로** 발동한다.
// 증강 1개 = 특성 1개 원칙은 여기서도 지킨다 — 조합은 「효과의 복합」이 아니라 「조건」이 만든다.
const SYNERGIES = [
    { fam: 'Wave',  n: 2, name: '공명',       trait: 'Area',       v: 0.25, desc: '파동 스킬 범위 +25%' },
    { fam: 'Wave',  n: 4, name: '대공명',     trait: 'Damage',     v: 0.40, desc: '파동 스킬 피해 +40%' },
    { fam: 'Throw', n: 2, name: '연습 투구',  trait: 'Cooldown',   v: 0.15, desc: '투척 스킬 쿨타임 -15%' },
    { fam: 'Throw', n: 4, name: '강견',       trait: 'Damage',     v: 0.40, desc: '투척 스킬 피해 +40%' },
    { fam: 'Field', n: 2, name: '끈적한 흙',  trait: 'Duration',   v: 0.30, desc: '장판 스킬 지속 +30%' },
    { fam: 'Field', n: 4, name: '초토화',     trait: 'Damage',     v: 0.40, desc: '장판 스킬 피해 +40%' },
    { fam: 'Melee', n: 2, name: '이빨 갈기',  trait: 'Damage',     v: 0.25, desc: '근접 스킬 피해 +25%' },
    { fam: 'Melee', n: 4, name: '광견',       trait: 'CritChance', v: 0.15, desc: '근접 스킬 치명타 확률 +15%p' },
    { fam: 'Gaze',  n: 2, name: '집중',       trait: 'CritDamage', v: 0.40, desc: '시선 스킬 치명타 피해 +40%p' },
    { fam: 'Gaze',  n: 4, name: '꿰뚫는 시야', trait: 'Damage',    v: 0.50, desc: '시선 스킬 피해 +50%' },
    { fam: 'Aura',  n: 2, name: '지구력',     trait: 'Duration',   v: 0.30, desc: '각성 스킬 지속 +30%' },
    { fam: 'Aura',  n: 4, name: '폭주',       trait: 'Cooldown',   v: 0.25, desc: '각성 스킬 쿨타임 -25%' },

    // 3개 구간은 **생존**으로 보상한다. 2개(공격) → 3개(생존) → 4개(공격) 순으로 갈리면
    // 계열을 모으는 동안 공격과 생존이 번갈아 커져 곡선이 한쪽으로 무너지지 않는다.
    { fam: 'Melee', n: 3, name: '피의 맛',     trait: 'Lifesteal',  v: 1.2,  desc: `처치 시 체력 +1.2` },
    { fam: 'Gaze',  n: 3, name: '포식자의 눈', trait: 'Lifesteal',  v: 1.2,  desc: `처치 시 체력 +1.2` },
    { fam: 'Aura',  n: 3, name: '두꺼운 가죽', trait: 'Lifesteal',  v: 1.2,  desc: `처치 시 체력 +1.2` },
    { fam: 'Wave',  n: 3, name: '울림통',      trait: 'Lifesteal',  v: 1.2,  desc: `처치 시 체력 +1.2` },
    { fam: 'Field', n: 3, name: '흙먼지 질주', trait: 'Lifesteal',  v: 1.2,  desc: `처치 시 체력 +1.2` },
    { fam: 'Throw', n: 3, name: '가벼운 발놀림', trait: 'Lifesteal',  v: 1.2,  desc: `처치 시 체력 +1.2` }
];


// 스킬이 거는 디버프의 **종류**. 연계 시너지는 id 가 아니라 종류로 조건을 건다 —
// 「둔화면 무엇이든」이 데이터로 표현돼야 새 둔화 스킬이 늘어도 연계가 따라온다.
const DEBUFF_KIND = {
    '#debuff/burn': 'DamageOverTime',
    '#debuff/venom': 'DamageOverTime',
    '#debuff/chill': 'SlowReduce',
    '#debuff/drowsy': 'SlowReduce',
    '#debuff/expose': 'DamageReceiveIncrease',
    '#debuff/stun': 'Stun'
};

// ── 상태이상 연계 ──────────────────────────────────────────
// 그 상태이상을 거는 스킬을 하나라도 들고 있으면 발동하고, **그 상태의 적에게** 피해가 붙는다.
// 이게 「하품(피해 0 유틸)을 먹을 이유」를 만든다 — 자기는 안 때려도 남의 딜을 키운다.
const LINKS = [
    { key: 'slow',   kind: 'SlowReduce',            name: '사냥 본능', v: 0.35, desc: '둔화된 적에게 피해 +35%' },
    { key: 'burn',   kind: 'DamageOverTime',        name: '불붙은 사료', v: 0.30, desc: '화상·중독 걸린 적에게 피해 +30%' },
    { key: 'expose', kind: 'DamageReceiveIncrease', name: '약점 포착', v: 0.40, desc: '취약 걸린 적에게 피해 +40%' }
];

// ── 버프 / 디버프 ──────────────────────────────────────────
const BUFFS = [
    { id: 3001, key: 'rage',    name: 'Rage',    desc: '분노 — 공격력 증가',    type: 'DamageBoost',      dur: 8,   v0: 0.40 },
    { id: 3002, key: 'haste',   name: 'Haste',   desc: '신속 — 공격 속도 증가', type: 'AttackSpeedBoost', dur: 8,   v0: 0.40 },
    { id: 3003, key: 'regen',   name: 'Regen',   desc: '재생 — 지속 회복',      type: 'HealOverTime',     dur: 6,   v0: 0.02, tick: 1 },
    { id: 3004, key: 'barrier', name: 'Barrier', desc: '보호막',                type: 'Shield',           dur: 8,   v0: 0.15 },
    { id: 3005, key: 'sprint',  name: 'Sprint',  desc: '질주 — 이동 속도 증가', type: 'MoveSpeedBoost',   dur: 6,   v0: 0.40 },
    { id: 3006, key: 'leech',   name: 'Leech',   desc: '흡혈 — 문 순간 회복',   type: 'HealOverTime',     dur: 0.2, v0: 0.15, tick: 0.1 }
];

const DEBUFFS = [
    { id: 2001, key: 'burn',   name: 'Burn',   desc: '화상 — 지속 피해',        type: 'DamageOverTime',        dur: 3,   v0: 0.20, tick: 0.5 },
    { id: 2002, key: 'venom',  name: 'Venom',  desc: '중독 — 중첩 지속 피해',   type: 'DamageOverTime',        dur: 4,   v0: 0.10, tick: 0.5, stack: 'Stack', max: 5 },
    { id: 2003, key: 'chill',  name: 'Chill',  desc: '둔화 — 이동 속도 감소',   type: 'SlowReduce',            dur: 2,   v0: 0.40 },
    { id: 2004, key: 'expose', name: 'Expose', desc: '취약 — 받는 피해 증가',   type: 'DamageReceiveIncrease', dur: 4,   v0: 0.20 },
    { id: 2005, key: 'drowsy', name: 'Drowsy', desc: '졸음 — 강한 둔화',        type: 'SlowReduce',            dur: 3,   v0: 0.50 }
];

// ── 파츠 24종 + 캐릭터 고유 3종 ────────────────────────────
// t: 이 파츠가 가질 수 있는 특성. u: 고유 특성(이름/설명/효과값).
const P = (o) => o;
const PARTS = [
    // 입 (치아)
    P({ id: 2001, key: 'bite_heal', name: 'Bite Heal', disp: '개튼튼 치아', slot: 'Mouth', elem: 'bite', launch: 'instant',
        copy: '앞의 적을 물어 공격력 150% 피해, 물 때마다 체력을 조금 회복. 1.2초마다',
        target: { type: 'TargetNearestForward', params: { Range: '@stat.Range', HalfAngle: 60 } },
        stat: { Cooldown: 1.2, Range: 2.4, Damage: 150, Radius: 0.6 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        cast: { type: 'BuffSelf', params: { BuffId: '#buff/leech' } },
        t: ['Damage', 'Cooldown', 'CritChance', 'CritDamage'],
        names: { Damage: '치아 강화', Cooldown: '급한 입질', CritChance: '예리한 앞니', CritDamage: '급소 물기' },
        u: { name: '날카로운 송곳니', desc: '흡혈량 +1%p', field: 'StatusValueAdd', value: 0.01 } }),

    P({ id: 2002, key: 'bark_cone', name: 'Bark Cone', disp: '왈왈', slot: 'Mouth', elem: 'bark', launch: 'instant',
        copy: '전방 부채꼴 적에게 공격력 100% 피해 + 둔화. 2.5초마다',
        target: { type: 'TargetCone', params: { Range: '@stat.Range', HalfAngle: 30 } },
        stat: { Cooldown: 2.5, Range: 4, Damage: 100, Radius: 1.2 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        debuff: '#debuff/chill',
        t: ['Damage', 'Cooldown', 'Area'],
        names: { Damage: '데시벨 업', Cooldown: '쉴 새 없이', Area: '확성기' },
        u: { name: '우렁참', desc: '둔화량 +10%p', field: 'StatusValueAdd', value: 0.10 } }),

    P({ id: 2003, key: 'growl_aoe', name: 'Growl', disp: '으르렁', slot: 'Mouth', elem: 'growl', launch: 'instant',
        copy: '반경 3m 적에게 공격력 80% 피해 + 2초간 둔화. 3초마다',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 3, Range: 3, Damage: 80, Radius: 3 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        debuff: '#debuff/chill',
        t: ['Damage', 'Cooldown', 'Area'],
        names: { Damage: '성대 단련', Cooldown: '숨 고르기', Area: '메아리' },
        u: { name: '낮은 저음', desc: '둔화량 +10%p', field: 'StatusValueAdd', value: 0.10 } }),

    P({ id: 2004, key: 'yawn', name: 'Yawn', disp: '하품', slot: 'Mouth', elem: 'yawn', launch: 'instant',
        copy: '전방 90° 적을 3초간 크게 느리게 만든다 (피해 없음). 6초마다',
        target: { type: 'TargetCone', params: { Range: '@stat.Range', HalfAngle: 45 } },
        stat: { Cooldown: 6, Range: 5, Damage: 0, Radius: 1.5, Duration: 3 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        debuff: '#debuff/drowsy',
        t: ['Cooldown', 'Area', 'Duration'],
        names: { Cooldown: '잦은 하품', Area: '크게 하품', Duration: '늘어지는 하품' },
        u: { name: '전염', desc: '둔화량 +10%p', field: 'StatusValueAdd', value: 0.10 } }),

    // 발 (신발)
    P({ id: 2005, key: 'dig_roll', name: 'Dig Roll', disp: '뒹굴기', slot: 'Feet', elem: 'dust', launch: 'ground_field',
        copy: '구르는 동안 0.5초마다 반경 1.5m 공격력 30% 피해 (상시 장판)',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 4, Range: 2, Damage: 30, Radius: 1.5, Duration: 4, DamageInterval: 0.5 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius', DamageInterval: '@stat.DamageInterval' } },
        t: ['Damage', 'Area', 'Duration'],
        names: { Damage: '발톱 관리', Area: '넓게 뒹굴기', Duration: '더 뒹굴기' },
        u: { name: '신나게', desc: '피해 간격 -0.1초', field: 'IntervalAdd', value: -0.1 } }),

    P({ id: 2006, key: 'claw_volley', name: 'Claw Volley', disp: '발톱 세례', slot: 'Feet', elem: 'claw', launch: 'projectile/straight',
        copy: '전방으로 발톱을 날려 각 공격력 60% 피해. 2초마다',
        target: { type: 'TargetCone', params: { Range: '@stat.Range', HalfAngle: 30 } },
        stat: { Cooldown: 2, Range: 8, Damage: 60, Radius: 0.5, Speed: 16, MaxDistance: 9 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'Cooldown', 'CritChance', 'Count'],
        names: { Damage: '발톱 갈기', Cooldown: '연속 할퀴기', CritChance: '갈고리 발톱', Count: '다섯 발가락' } }),

    P({ id: 2007, key: 'zoomies', name: 'Zoomies', disp: '우다다 모드', slot: 'Feet', elem: 'zoom', launch: 'self_cast',
        copy: '6초간 이동속도 +40%. 12초마다',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 12, Range: 1, Damage: 0, Duration: 6 },
        cast: { type: 'BuffSelf', params: { BuffId: '#buff/sprint' } },
        t: ['Cooldown', 'Duration'],
        names: { Cooldown: '짧은 휴식', Duration: '오래 우다다' },
        u: { name: '개신남', desc: '이동속도 증가 +10%p', field: 'StatusValueAdd', value: 0.10 } }),

    P({ id: 2008, key: 'bone_mine', name: 'Bone Mine', disp: '뼈다귀 매설', slot: 'Feet', elem: 'bone', launch: 'instant',
        copy: '발밑에 뼈다귀를 묻는다. 적이 밟으면 반경 1.5m 공격력 200% 폭발. 4초마다',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 4, Range: 2, Damage: 200, Radius: 1.5 },
        hit: { type: 'HitDeathBurst', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'CritDamage', 'Area', 'Count'],
        names: { Damage: '왕뼈다귀', CritDamage: '정확한 매설', Area: '폭심 확장', Count: '다다익뼈' } }),

    // 꼬리
    P({ id: 2009, key: 'tail_pierce', name: 'Tail Swipe', disp: '꼬리 흔들기', slot: 'Tail', elem: 'tail', launch: 'projectile/piercing',
        copy: '반월형 기를 날려 적을 꿰뚫으며 공격력 120% 피해. 1.5초마다',
        target: { type: 'TargetNearestForward', params: { Range: '@stat.Range', HalfAngle: 70 } },
        stat: { Cooldown: 1.5, Range: 9, Damage: 120, Radius: 0.7, Speed: 14, MaxDistance: 11, MaxPerTarget: 3 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'Cooldown', 'CritDamage', 'Count'],
        names: { Damage: '꼬리 근육', Cooldown: '살랑살랑', CritDamage: '급소 가르기', Count: '쌍꼬리' },
        u: { name: '낭창낭창', desc: '관통 +1', field: 'CountAdd', value: 1 } }),

    P({ id: 2010, key: 'static_fur', name: 'Static Fur', disp: '정전기 털', slot: 'Tail', elem: 'spark', launch: 'instant',
        copy: '털에서 정전기가 튀어 모든 적에게 공격력 40% 피해. 6초마다',
        target: { type: 'TargetAll', params: { Range: '@stat.Range' } },
        stat: { Cooldown: 6, Range: 14, Damage: 40, Radius: 0.6 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'Cooldown', 'CritChance'],
        names: { Damage: '털 충전', Cooldown: '방전 가속', CritChance: '찌릿' } }),

    P({ id: 2011, key: 'tail_whip', name: 'Tail Whip', disp: '꼬리 채찍', slot: 'Tail', elem: 'tail', launch: 'projectile/straight',
        copy: '꼬리를 후려쳐 직선상 적에게 공격력 140% 피해. 2초마다',
        target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
        stat: { Cooldown: 2, Range: 8, Damage: 140, Radius: 0.7, Speed: 18, MaxDistance: 10 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'Cooldown', 'Count'],
        names: { Damage: '채찍 단련', Cooldown: '손목 스냅', Count: '회전 채찍' },
        u: { name: '긴 꼬리', desc: '사거리 +20%', field: 'AreaMul', value: 0.20 } }),

    P({ id: 2012, key: 'tail_copter', name: 'Tail Copter', disp: '꼬리 프로펠러', slot: 'Tail', elem: 'dust', launch: 'ground_field',
        copy: '꼬리를 돌려 반경 2m 에 0.4초마다 공격력 35% 피해. 8초마다',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 8, Range: 2, Damage: 35, Radius: 2, Duration: 3, DamageInterval: 0.4 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius', DamageInterval: '@stat.DamageInterval' } },
        cast: { type: 'BuffSelf', params: { BuffId: '#buff/sprint' } },
        t: ['Damage', 'Cooldown', 'Area', 'Duration'],
        names: { Damage: '프로펠러 강화', Cooldown: '빠른 재가동', Area: '넓은 회전', Duration: '오래 돌기' } }),

    // 머리 (고글)
    P({ id: 2013, key: 'eye_beam', name: 'Eye Beam', disp: '개눈빛 레이저', slot: 'Head', elem: 'beam', launch: 'beam',
        copy: '전방으로 눈빛 빔을 1.5초간 발사, 0.2초마다 공격력 25% 피해. 4초마다',
        target: { type: 'TargetNearestForward', params: { Range: '@stat.Range', HalfAngle: 45 } },
        stat: { Cooldown: 4, Range: 10, Damage: 25, Radius: 0.8, Length: 8, Width: 1.2, Duration: 1.5, DamageInterval: 0.2 },
        hit: { type: 'HitBeam', params: { Damage: '@stat.Damage', Length: '@stat.Length', Width: '@stat.Width', DamageInterval: '@stat.DamageInterval' } },
        t: ['Damage', 'Cooldown', 'CritDamage', 'Duration'],
        names: { Damage: '눈빛 집중', Cooldown: '눈 깜빡임', CritDamage: '동공 지진', Duration: '째려보기' } }),

    P({ id: 2014, key: 'scent_homing', name: 'Scent Track', disp: '냄새 추적', slot: 'Head', elem: 'scent', launch: 'projectile/homing',
        copy: '가장 먼 적을 유도 추적해 공격력 130% 피해 + 받는 피해 증가. 2초마다',
        target: { type: 'TargetFarthest', params: { Range: '@stat.Range' } },
        stat: { Cooldown: 2, Range: 13, Damage: 130, Radius: 0.7, Speed: 11, MaxDistance: 16, TurnRate: 200 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        debuff: '#debuff/expose',
        t: ['Damage', 'Cooldown', 'CritDamage', 'Count'],
        names: { Damage: '후각 발달', Cooldown: '킁킁 속사', CritDamage: '급소 냄새', Count: '킁킁 레이더' },
        u: { name: '진한 체취', desc: '받는 피해 증가 +5%p', field: 'StatusValueAdd', value: 0.05 } }),

    P({ id: 2015, key: 'ear_sonar', name: 'Ear Sonar', disp: '귀 쫑긋', slot: 'Head', elem: 'sonar', launch: 'instant',
        copy: '반경 4m 원형 음파로 공격력 60% 피해. 2.5초마다',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 2.5, Range: 4, Damage: 60, Radius: 4 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'Cooldown', 'Area', 'CritChance'],
        names: { Damage: '고주파', Cooldown: '예민한 귀', Area: '넓은 청각', CritChance: '초음파' } }),

    P({ id: 2016, key: 'third_eye', name: 'Third Eye', disp: '제3의 눈', slot: 'Head', elem: 'gaze', launch: 'projectile/piercing',
        copy: '가장 가까운 적을 관통하는 시선으로 공격력 180% 피해. 3초마다',
        target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
        stat: { Cooldown: 3, Range: 11, Damage: 180, Radius: 0.6, Speed: 22, MaxDistance: 13, MaxPerTarget: 3 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'Cooldown', 'CritDamage', 'Count'],
        names: { Damage: '응시', Cooldown: '빠른 시선', CritDamage: '꿰뚫는 눈', Count: '두 번 보기' } }),

    // 목 (목걸이)
    P({ id: 2017, key: 'pack_chain', name: 'Pack Howl', disp: '무리 울음', slot: 'Neck', elem: 'howl', launch: 'instant',
        copy: '울음이 가까운 적들을 연쇄로 타고 공격력 70% 피해. 2.5초마다',
        target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
        stat: { Cooldown: 2.5, Range: 9, Damage: 70, Radius: 4 },
        hit: { type: 'HitChain', params: { Damage: '@stat.Damage', Radius: '@stat.Radius', MaxBounces: 4 } },
        t: ['Damage', 'Cooldown', 'CritChance', 'Count'],
        names: { Damage: '우렁찬 울음', Cooldown: '쉴 틈 없는 합창', CritChance: '날 선 하울링', Count: '대가족' } }),

    P({ id: 2018, key: 'alpha_mode', name: 'Alpha Mode', disp: '개쌘놈 모드', slot: 'Neck', elem: 'awaken', launch: 'self_cast',
        copy: '8초간 공격속도 +40%. 16초마다',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 16, Range: 1, Damage: 0, Duration: 8 },
        cast: { type: 'BuffSelf', params: { BuffId: '#buff/haste' } },
        t: ['Cooldown', 'Duration'],
        names: { Cooldown: '빠른 재각성', Duration: '각성 연장' },
        u: { name: '개쌘 각성', desc: '공격속도 증가 +10%p', field: 'StatusValueAdd', value: 0.10 } }),

    P({ id: 2019, key: 'fur_barrier', name: 'Fur Barrier', disp: '털뭉치 보호막', slot: 'Neck', elem: 'shield', launch: 'self_cast',
        copy: '최대체력 15% 보호막을 8초간. 10초마다',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 10, Range: 1, Damage: 0, Duration: 8 },
        cast: { type: 'BuffSelf', params: { BuffId: '#buff/barrier' } },
        t: ['Cooldown', 'Duration'],
        names: { Cooldown: '빠른 털갈이', Duration: '질긴 털' },
        u: { name: '겹털', desc: '보호막량 +5%p', field: 'StatusValueAdd', value: 0.05 } }),

    P({ id: 2020, key: 'kibble_rain', name: 'Kibble Rain', disp: '사료 유성우', slot: 'Neck', elem: 'kibble', launch: 'projectile/arc',
        copy: '랜덤한 적 위치로 사료가 낙하해 반경 1.5m 공격력 80% 피해. 5초마다',
        target: { type: 'TargetRandom', params: { Range: '@stat.Range', Index: 3 } },
        stat: { Cooldown: 5, Range: 11, Damage: 80, Radius: 1.5, Speed: 12, ArcHeight: 4 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'Area', 'Count'],
        names: { Damage: '대용량 사료', Area: '광역 배급', Count: '사료 샤워' } }),

    // 등 (가방)
    P({ id: 2021, key: 'kibble_bomb', name: 'Kibble Bomb', disp: 'God-사료 폭탄', slot: 'Back', elem: 'kibble', launch: 'projectile/arc',
        copy: '사료를 던져 반경 2m 공격력 90% 피해 + 화상. 3초마다',
        target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
        stat: { Cooldown: 3, Range: 10, Damage: 90, Radius: 2, Speed: 13, ArcHeight: 3 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        debuff: '#debuff/burn',
        t: ['Damage', 'Cooldown', 'Area'],
        names: { Damage: '사료 증량', Cooldown: '배급 가속', Area: '곱빼기' },
        u: { name: '매운맛 사료', desc: '화상 초당 피해 +5%p', field: 'StatusValueAdd', value: 0.05 } }),

    P({ id: 2022, key: 'frisbee', name: 'Frisbee', disp: '원반 물어오기', slot: 'Back', elem: 'frisbee', launch: 'projectile/wave',
        copy: '원반이 흔들리며 날아가 공격력 110% 피해. 2초마다',
        target: { type: 'TargetRandom', params: { Range: '@stat.Range' } },
        stat: { Cooldown: 2, Range: 10, Damage: 110, Radius: 0.8, Speed: 12, ArcHeight: 1.5, MaxPerTarget: 2 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'CritChance', 'Count'],
        names: { Damage: '원반 연마', CritChance: '모서리 각', Count: '더블 캐치' },
        u: { name: '탄성 코팅', desc: '관통 +1', field: 'CountAdd', value: 1 } }),

    P({ id: 2023, key: 'tennis_ball', name: 'Tennis Ball', disp: '테니스공 물어와', slot: 'Back', elem: 'ball', launch: 'projectile/homing',
        copy: '가장 가까운 적을 쫓는 테니스공, 공격력 100% 피해. 1.8초마다',
        target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
        stat: { Cooldown: 1.8, Range: 10, Damage: 100, Radius: 0.6, Speed: 13, MaxDistance: 14, TurnRate: 240 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'Cooldown', 'CritDamage', 'Count'],
        names: { Damage: '공놀이 숙련', Cooldown: '물어와 물어와', CritDamage: '강속구', Count: '공 하나 더' } }),

    P({ id: 2024, key: 'slipper', name: 'Slipper Throw', disp: '슬리퍼 투척', slot: 'Back', elem: 'slipper', launch: 'projectile/straight',
        copy: '슬리퍼를 던져 공격력 300% 피해. 5초마다',
        target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
        stat: { Cooldown: 5, Range: 10, Damage: 300, Radius: 0.9, Speed: 20, MaxDistance: 12 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'Cooldown', 'CritChance', 'CritDamage'],
        names: { Damage: '슬리퍼 강화', Cooldown: '던지기 숙련', CritChance: '정확한 조준', CritDamage: '정통으로' } }),

    // 캐릭터 고유 — 트리 해금 대상이 아니다. owner 로 소유 캐릭터를 표시하고 드랍 풀에서 제외한다.
    P({ id: 2101, key: 'ignore_taunt', name: 'Ignore', disp: '개무시', slot: 'Neck', elem: 'taunt', launch: 'self_cast', owner: 1,
        copy: '3초간 단단해지며 주변 적을 끌어당긴다. 12초마다',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 12, Range: 3, Damage: 0, Duration: 3 },
        cast: { type: 'BuffSelf', params: { BuffId: '#buff/barrier' } },
        t: ['Cooldown', 'Duration'],
        names: { Cooldown: '쿨한 척', Duration: '시크한 표정' },
        u: { name: '완벽한 무시', desc: '보호막량 +10%p', field: 'StatusValueAdd', value: 0.10 } }),

    P({ id: 2102, key: 'bread_slice', name: 'Bread Slice', disp: '식빵 썰기', slot: 'Tail', elem: 'bread', launch: 'instant', owner: 2,
        copy: '엉덩이를 흔들어 사방 반경 3m 에 공격력 80% 피해. 2.5초마다',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 2.5, Range: 3, Damage: 80, Radius: 3 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
        t: ['Damage', 'Cooldown', 'Area'],
        names: { Damage: '두툼한 식빵', Cooldown: '빵칼 갈기', Area: '통밀빵' },
        u: { name: '겉바속촉', desc: '치명타 확률 +10%p', field: 'CritChanceAdd', value: 0.10 } }),

    P({ id: 2103, key: 'afterimage_dash', name: 'Afterimage', disp: '잔상 질주', slot: 'Feet', elem: 'dust', launch: 'ground_field', owner: 3,
        copy: '4초간 빨라지고 지나간 자리에 잔상이 남아 피해를 준다. 10초마다',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: 10, Range: 2, Damage: 50, Radius: 1.6, Duration: 4, DamageInterval: 0.3 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius', DamageInterval: '@stat.DamageInterval' } },
        cast: { type: 'BuffSelf', params: { BuffId: '#buff/sprint' } },
        t: ['Damage', 'Cooldown', 'Duration'],
        names: { Damage: '날카로운 잔상', Cooldown: '숨 고르기', Duration: '긴 잔상' },
        u: { name: '전력 질주', desc: '이동속도 증가 +15%p', field: 'StatusValueAdd', value: 0.15 } })
];

// ── 파일 쓰기 ──────────────────────────────────────────────
function write(rel, body)
{
    const abs = path.join(ROOT, rel);
    fs.mkdirSync(path.dirname(abs), { recursive: true });
    fs.writeFileSync(abs, body.replace(/\r?\n/g, '\n'), 'utf8');
}

function wipe(rel)
{
    const abs = path.join(ROOT, rel);
    if (fs.existsSync(abs))
    {
        fs.rmSync(abs, { recursive: true, force: true });
    }
}

const j = (v) => JSON.stringify(v, null, 4).replace(/"([A-Za-z_][A-Za-z0-9_]*)":/g, '$1:').replace(/"/g, "'");

function emitVisuals()
{
    wipe('modules/visual');
    for (const [key, v] of Object.entries(VISUALS))
    {
        write(`modules/visual/${key}.js`,
`module.exports = {
    color: '${v.color}',
    desc: '${v.desc}',
    values: {
        Projectile: '${v.P}',
        Impact: '${v.I}',
        Cast: '${v.C}',
        Color: '${v.color}'
    }
};
`);
    }
}

function emitStatuses()
{
    wipe('modules/buff');
    wipe('modules/debuff');
    for (const b of BUFFS)
    {
        write(`modules/buff/${b.key}.js`,
`module.exports = {
    id: ${b.id},
    name: '${b.name}',
    desc: '${b.desc}',
    type: '${b.type}',
    duration: ${b.dur},
    tickInterval: ${b.tick || 0},
    value0: ${b.v0},
    value1: 0,
    stackMode: '${b.stack || 'Refresh'}',
    maxStacks: ${b.max || 1}
};
`);
    }
    for (const d of DEBUFFS)
    {
        write(`modules/debuff/${d.key}.js`,
`module.exports = {
    id: ${d.id},
    name: '${d.name}',
    desc: '${d.desc}',
    type: '${d.type}',
    duration: ${d.dur},
    tickInterval: ${d.tick || 0},
    value0: ${d.v0},
    value1: 0,
    stackMode: '${d.stack || 'Refresh'}',
    maxStacks: ${d.max || 1}
};
`);
    }
}

function emitEnemies()
{
    wipe('modules/enemy');
    for (const e of ENEMIES)
    {
        write(`modules/enemy/${e.sprite}.js`,
`/** 적 타입. weight 로 추첨되고 minTime(초) 이후에만 등장한다. sprite 가 2D 비주얼 키. */
module.exports = {
    id: ${e.id},
    name: '${e.name}',
    desc: '${e.disp} — ${e.desc}',
    sprite: '${e.sprite}',
    hp: ${e.hp},
    moveSpeed: ${e.spd},
    attackDamage: ${e.dmg},
    scale: ${e.scale},
    color: '${e.color}',
    xp: ${e.xp},
    weight: ${e.w},
    minTime: ${e.t}
};
`);
    }
}

function emitWaves()
{
    wipe('modules/wave');
    for (const w of WAVES)
    {
        write(`modules/wave/${w.name.toLowerCase()}.js`,
`module.exports = {
    id: ${w.id},
    name: '${w.name}',
    desc: '${w.desc}',
    time: ${w.time},
    repeat: ${w.repeat},
    count: ${w.count},
    hpMul: ${w.hp},
    damageMul: ${w.dmg},
    scaleMul: ${w.scale},
    xpMul: ${w.xp},
    banner: '${w.banner}',
    boss: ${w.boss ? true : false},
    enemyTypeId: ${w.type || 0},
    speedMul: ${w.spd || 1.0}
};
`);
    }
}

function emitRounds()
{
    wipe('modules/round');
    for (const r of ROUNDS)
    {
        write(`modules/round/r${String(r.r).padStart(2, '0')}.js`,
`/** 라운드 ${r.r} — ${r.r * 60 - 60}~${r.r * 60}초. 스폰 간격/한 번에/동시 상한이 난이도의 본체다. */
module.exports = {
    id: ${7000 + r.r},
    name: 'Round${r.r}',
    desc: '라운드 ${r.r}',
    round: ${r.r},
    spawnGap: ${r.gap},
    spawnBatch: ${r.batch},
    maxAlive: ${r.alive},
    hpMul: ${r.hp},
    damageMul: ${r.dmg},
    banner: '${r.banner}'
};
`);
    }
}

function emitSynergy()
{
    wipe('modules/synergy');
    let id = 8000;

    // 계열 시너지 — 넓게 모으는 보상
    for (const sy of SYNERGIES)
    {
        write(`modules/synergy/fam_${sy.fam.toLowerCase()}_${sy.n}.js`,
`/** ${FAMILY_KR[sy.fam]} 계열 ${sy.n}개 보유 시 자동 발동. */
module.exports = {
    id: ${++id},
    name: 'fam_${sy.fam.toLowerCase()}_${sy.n}',
    desc: '${FAMILY_KR[sy.fam]} ${sy.n}개 — ${sy.desc}',
    displayName: '${sy.name}',
    family: '${sy.fam}',
    reqFamilyCount: ${sy.n},
    reqSkillId: 0,
    reqOwnOptions: 0,
    reqDebuff: 'None',
    trait: '${sy.trait}',
    value: ${sy.v},
    vsDebuff: 'None'
};
`);
    }

    // 상태이상 연계 — 조합의 세 번째 축. 거는 스킬과 때리는 스킬이 따로라 조합이 생긴다.
    for (const lk of LINKS)
    {
        write(`modules/synergy/link_${lk.key}.js`,
`/** ${lk.name} — ${lk.kind} 를 거는 스킬을 보유하면 발동. 그 상태의 적에게만 붙는다. */
module.exports = {
    id: ${++id},
    name: 'link_${lk.key}',
    desc: '${lk.desc}',
    displayName: '${lk.name}',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: 0,
    reqOwnOptions: 0,
    reqDebuff: '${lk.kind}',
    trait: 'None',
    value: ${lk.v},
    vsDebuff: '${lk.kind}'
};
`);
    }

    // 특화 — 깊게 파는 보상. 한 스킬의 증강을 3개 이상 모으면 그 스킬만 대폭 강해진다.
    // 계열 4단계(+40%)보다 한 스킬 기준으로 더 세야 「하나만 몰빵」이 실제로 성립한다.
    for (const p of PARTS)
    {
        const dealsDamage = (p.stat.Damage || 0) > 0;
        const trait = dealsDamage ? 'Damage' : 'Duration';
        const v = dealsDamage ? 0.80 : 0.50;
        write(`modules/synergy/mas_${p.key}.js`,
`/** ${p.disp} 통달 — 이 스킬의 증강을 4개 이상 모으면 자동 발동. 전설(3개)을 주운 것만으로는 안 뜬다. */
module.exports = {
    id: ${++id},
    name: 'mas_${p.key}',
    desc: '${p.disp} 증강 4개 — ${dealsDamage ? '피해' : '지속시간'} +${Math.round(v * 100)}%',
    displayName: '${p.disp} 통달',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: ${p.id},
    reqOwnOptions: 4,
    reqDebuff: 'None',
    trait: '${trait}',
    value: ${v},
    vsDebuff: 'None'
};
`);
    }
    return id - 8000;
}

function emitStat(p)
{
    const lines = Object.entries(p.stat).map(([k, v]) => `        ${k}: ${v}`).join(',\n');
    write(`modules/stat/${p.key}.js`,
`/** ${p.disp} 수치. 트리에서 '@stat.<키>' 로 참조한다. */
module.exports = {
    desc: '${p.disp} — ${p.copy}',
    values: {
${lines}
    }
};
`);
}

function emitSkill(p)
{
    const slots = {};
    if (p.hit) slots.onHit = p.hit;
    if (p.debuff) slots.onDebuff = { type: 'DebuffOnHit', params: { DebuffId: p.debuff } };
    if (p.cast) slots.onCast = p.cast;

    write(`skills/${p.id}_${p.key}.js`,
`/** ${p.disp} — ${p.copy} */
module.exports = {
    id: ${p.id},
    name: '${p.name}',
    desc: '${p.copy}',
    maxLevel: 1,

    visual: '${p.elem}',
    stat: '${p.key}',
    launch: '${p.launch}',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: ${j(p.target)},

    slots: ${j(slots)}
};
`);
}

function emitPart(p)
{
    write(`modules/part/${p.key}.js`,
`/** 파츠 = 스킬 1개 + 옵션 슬롯. 착용하면 skillId 스킬이 자동 발동한다. */
module.exports = {
    id: ${p.id},
    name: '${p.name}',
    desc: '${p.copy}',
    displayName: '${p.disp}',
    slot: '${p.slot}',
    family: '${FAMILY[p.key] || 'None'}',
    appliesDebuff: '${DEBUFF_KIND[p.debuff] || 'None'}',
    skillId: ${p.id},
    weight: ${p.owner ? 0 : 10},
    owner: ${p.owner || 0}
};
`);
}

function emitOptions(p)
{
    const traits = p.t.slice();

    // 계열 생존 특성을 옵션 풀에 섞는다. 뽑기에 섞여야 「공격을 살까 생존을 살까」가
    // 매 파츠마다 굴러가고, 한 스킬을 깊게 파도 생존이 같이 큰다.
    const surv = FAMILY_SURVIVAL[FAMILY[p.key]] || [];
    for (const st of surv)
    {
        if (traits.includes(st) === false)
            traits.push(st);
    }

    for (const t of traits)
    {
        const spec = TRAITS[t];
        const disp = (p.names && p.names[t]) || `${p.disp} · ${spec.suffix}`;
        write(`modules/option/${p.key}_${t.toLowerCase()}.js`,
`module.exports = {
    id: ${4000 + (p.id - 2000) * 20 + spec.idx},
    name: '${p.key}_${t.toLowerCase()}',
    desc: '${p.disp} — ${spec.desc(spec.value)}',
    displayName: '${disp}',
    partId: ${p.id},
    trait: '${t}',
    value: ${spec.value},
    maxStack: ${spec.max}
};
`);
    }
    if (p.u)
    {
        const spec = TRAITS.Unique;
        write(`modules/option/${p.key}_unique.js`,
`module.exports = {
    id: ${4000 + (p.id - 2000) * 20 + spec.idx},
    name: '${p.key}_unique',
    desc: '${p.disp} — ${p.u.desc}',
    displayName: '${p.u.name}',
    partId: ${p.id},
    trait: 'Unique',
    value: ${p.u.value},
    maxStack: ${spec.max},
    uniqueField: '${p.u.field}'
};
`);
    }
}

function main()
{
    wipe('skills');
    wipe('modules/part');
    wipe('modules/option');
    wipe('modules/stat');
    wipe('modules/augment');

    emitVisuals();
    emitStatuses();
    emitEnemies();
    emitWaves();
    emitRounds();
    const syn = emitSynergy();

    let opts = 0;
    for (const p of PARTS)
    {
        emitStat(p);
        emitSkill(p);
        emitPart(p);
        emitOptions(p);
        opts += p.t.length + (p.u ? 1 : 0);
    }
    console.log(`파츠 ${PARTS.length}종 / 옵션 ${opts}종 / 시너지 ${syn}종 / 적 ${ENEMIES.length}종 / 라운드 ${ROUNDS.length} 생성 완료`);
}

main();
