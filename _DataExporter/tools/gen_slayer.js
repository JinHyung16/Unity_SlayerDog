/**
 * SlayerDog 전투 데이터 스캐폴더 (볼트 SlayerDog/SlayerDog 기획서.md 캐릭터 10 · TASKS D-1 · D-2).
 *
 * 1) 예전 음악 기본 공격 5종(basic_bass_drop 등)을 지우고 캐릭터 10명의 고유 공격(owner 1~10)을 쓴다
 *    — Master · Visual · Part · Option · 통달 시너지
 * 2) 무기 29종 — 동작 · 수치는 그대로, 이름(displayName) · 설명만 판타지 · 동양풍으로
 * 3) 계열 · 연계 시너지 이름, 옵션의 «무기이름 · 흡혈/튼튼/날쌘» 앞머리, 옵션 설명 앞머리를 지금 이름으로
 * 4) 맵 10 테이블 (Tables/map)
 *
 * 다시 돌려도 결과가 같다(멱등). 실행: node tools/gen_slayer.js && node export.js
 */
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', 'GameData');
// 아군 스킬 색은 채도 0.6 이하 (파스텔 세계에 맞춤). 적 공격(DANGER)만 짙은 붉은 주황으로 남겨 피아를 가른다
const FAM_COLOR = { Wave: '#66EDFF', Throw: '#B1FF66', Field: '#A66BFF', Melee: '#FF66BA', Gaze: '#669BFF', Aura: '#FFC266' };
const DEBUFF_ENUM = { '#debuff/chill': 'SlowReduce', '#debuff/burn': 'DamageOverTime', '#debuff/expose': 'DamageReceiveIncrease' };

function write(rel, body)
{
    const abs = path.join(ROOT, rel);
    fs.mkdirSync(path.dirname(abs), { recursive: true });
    fs.writeFileSync(abs, body.replace(/\r?\n/g, '\n'), 'utf8');
}

function remove(rel)
{
    const abs = path.join(ROOT, rel);
    if (fs.existsSync(abs))
    {
        fs.unlinkSync(abs);
        return 1;
    }
    return 0;
}

const js = (v, ind = 4) => JSON.stringify(v, null, ind).replace(/"([A-Za-z_][A-Za-z0-9_]*)":/g, '$1:').replace(/"/g, "'");

// ── 1) 예전 기본 공격 정리 ─────────────────────────────────
// 이 스크립트가 쓰는 basic_* 도 함께 지웠다 다시 쓴다 (키를 바꿔도 찌꺼기가 안 남는다)
let removed = 0;
for (const dir of ['Skill/Master', 'Skill/Visual', 'Skill/Part', 'Skill/Option', 'Tables/synergy'])
{
    for (const f of fs.readdirSync(path.join(ROOT, dir)))
    {
        if (/^(mas_)?basic_/.test(f))
            removed += remove(path.join(dir, f));
    }
}

// ── 고유 공격 10 — owner = RunSelection.Heroes 순서 = 기획서 캐릭터 표 순서 ──
// opt: 이 공격이 받는 옵션 (trait: [표시이름, 값, 최대중첩])
// 화력 기준: 예전 LV2 5/5 클리어 판의 기본 공격 단일 대상 초당 150~200, 광역은 그 절반 안팎
const HEROES = [
    { id: 2201, key: 'basic_iaido', name: 'Iaido', disp: '발도 일섬', owner: 1, family: 'Melee', slot: 'Neck',
      method: 'instant', fx: 'Fx_Iaido', impact: 'Fx_Hit', cast: 'Fx_Iaido', orient: 'Ground', color: '#FF8FC8',
      copy: '칼을 뽑아 전방 반달 반경 1.9m 에 공격력 200% 피해. 0.9초마다',
      target: { type: 'TargetCone', params: { Range: '@stat.Range', HalfAngle: 60 } },
      stat: { Cooldown: 0.9, Range: 3.0, Damage: 200, Radius: 1.9 },
      hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      opt: { Damage: ['벼린 칼날', 0.3, 3], Cooldown: ['빠른 발도', 0.1, 3], Area: ['긴 칼날', 0.2, 2], CritChance: ['일섬', 0.1, 2], CritDamage: ['급소 베기', 0.25, 2] } },

    { id: 2202, key: 'basic_pyeonjeon', name: 'Pyeonjeon', disp: '편전', owner: 2, family: 'Throw', slot: 'Back',
      method: 'projectile/burst', fx: 'Fx_Arrow', impact: 'Fx_Hit', cast: 'Fx_Muzzle', orient: 'Ground', color: '#FF7D66',
      copy: '짧은 화살 4발을 부채꼴로 쏘아 각 공격력 75% 피해. 1초마다',
      target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
      stat: { Cooldown: 1.0, Range: 9, Damage: 75, Radius: 0.5, Speed: 18, MaxDistance: 11, BurstCount: 4, BurstInterval: 0.05, BurstSpread: 14 },
      hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      opt: { Damage: ['강궁', 0.3, 3], Cooldown: ['속사', 0.1, 3], Count: ['한 발 더', 1, 2], CritChance: ['명궁', 0.1, 2] } },

    { id: 2203, key: 'basic_shuriken', name: 'Shuriken', disp: '수리검', owner: 3, family: 'Throw', slot: 'Feet',
      method: 'projectile/wave', fx: 'Fx_Shuriken', impact: 'Fx_HitBurst', cast: 'Fx_Shuriken', orient: 'Billboard', color: '#B48CFF',
      copy: '흔들리며 날아가는 표창이 적 3명까지 맞혀 공격력 85% 피해. 0.8초마다',
      target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
      stat: { Cooldown: 0.8, Range: 10, Damage: 85, Radius: 0.7, Speed: 13, ArcHeight: 1.0, MaxPerTarget: 3 },
      hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      opt: { Damage: ['독 바른 날', 0.3, 3], Cooldown: ['손놀림', 0.1, 3], Count: ['쌍수리검', 1, 2], CritChance: ['급소 노리기', 0.1, 2] } },

    { id: 2204, key: 'basic_palm_wave', name: 'Palm Wave', disp: '장풍', owner: 4, family: 'Wave', slot: 'Mouth',
      method: 'instant', fx: 'Fx_Palm', impact: 'Fx_Palm', cast: 'Fx_Palm', orient: 'Ground', color: '#FFD466',
      copy: '손바닥에서 기를 내뿜어 반경 2.6m 적에게 공격력 85% 피해 + 둔화. 1.2초마다',
      target: { type: 'TargetSelf' },
      stat: { Cooldown: 1.2, Range: 2.6, Damage: 85, Radius: 2.6 },
      hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      debuff: '#debuff/chill',
      opt: { Damage: ['내공', 0.3, 3], Cooldown: ['호흡법', 0.1, 3], Area: ['기공 확장', 0.2, 2], MaxHp: ['금강불괴', 0.15, 2] } },

    { id: 2205, key: 'basic_talisman', name: 'Soul Talisman', disp: '추혼부', owner: 5, family: 'Gaze', slot: 'Head',
      method: 'projectile/homing', fx: 'Fx_Talisman', impact: 'Fx_HitBurst', cast: 'Fx_Talisman', orient: 'Billboard', color: '#60F0B2',
      copy: '부적이 적을 쫓아가 터지며 반경 1.2m 공격력 130% 피해 + 받는 피해 증가. 0.9초마다',
      target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
      stat: { Cooldown: 0.9, Range: 11, Damage: 130, Radius: 1.2, Speed: 13, MaxDistance: 15, TurnRate: 260 },
      hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      debuff: '#debuff/expose',
      opt: { Damage: ['주사 먹', 0.3, 3], Cooldown: ['빠른 붓', 0.1, 3], Count: ['겹부적', 1, 2], CritDamage: ['혼 빼기', 0.25, 2] } },

    { id: 2206, key: 'basic_judgement', name: 'Judgement', disp: '심판', owner: 6, family: 'Field', slot: 'Back',
      method: 'ground_field', fx: 'Fx_Holy', impact: 'Fx_Holy', cast: 'Fx_Holy', orient: 'Ground', color: '#FFE27A',
      copy: '발밑에 성광을 2초간 깔아 반경 2.2m 에 0.4초마다 공격력 40% 피해. 2초마다',
      target: { type: 'TargetSelf' },
      stat: { Cooldown: 2.0, Range: 2.2, Damage: 40, Radius: 2.2, Duration: 2.0, DamageInterval: 0.4 },
      hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius', DamageInterval: '@stat.DamageInterval' } },
      opt: { Damage: ['성스러운 맹세', 0.3, 3], Cooldown: ['기도', 0.1, 3], Area: ['빛의 원', 0.2, 2], Duration: ['긴 심판', 0.25, 2], MaxHp: ['성기사의 갑주', 0.15, 2] } },

    { id: 2207, key: 'basic_fireball', name: 'Fireball', disp: '화염구', owner: 7, family: 'Wave', slot: 'Head',
      method: 'projectile/arc', fx: 'Fx_Fireball', impact: 'Fx_Blast', cast: 'Fx_Fireball', orient: 'Billboard', color: '#FF9F66',
      copy: '화염구가 날아가 터지며 반경 1.5m 공격력 120% 피해 + 화상. 1.1초마다',
      target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
      stat: { Cooldown: 1.1, Range: 10, Damage: 120, Radius: 1.5, Speed: 14, ArcHeight: 1.2 },
      hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      debuff: '#debuff/burn',
      opt: { Damage: ['화염 정수', 0.3, 3], Cooldown: ['빠른 영창', 0.1, 3], Area: ['대폭발', 0.2, 2], CritDamage: ['작열', 0.25, 2] } },

    { id: 2208, key: 'basic_axe_spin', name: 'Axe Spin', disp: '회전 도끼', owner: 8, family: 'Melee', slot: 'Tail',
      method: 'ground_field', fx: 'Fx_AxeWhirl', impact: 'Fx_AxeWhirl', cast: 'Fx_AxeWhirl', orient: 'Ground', color: '#8AD8FF',
      copy: '도끼를 1.2초간 휘돌려 반경 1.8m 에 0.3초마다 공격력 38% 피해. 1.6초마다',
      target: { type: 'TargetSelf' },
      stat: { Cooldown: 1.6, Range: 1.8, Damage: 38, Radius: 1.8, Duration: 1.2, DamageInterval: 0.3 },
      hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius', DamageInterval: '@stat.DamageInterval' } },
      opt: { Damage: ['날 세우기', 0.3, 3], Cooldown: ['광전사', 0.1, 3], Area: ['긴 자루', 0.2, 2], Duration: ['멈추지 않는 회전', 0.25, 2], Lifesteal: ['피의 축제', 1.4, 3] } },

    { id: 2209, key: 'basic_pierce_arrow', name: 'Piercing Arrow', disp: '관통 화살', owner: 9, family: 'Gaze', slot: 'Feet',
      method: 'projectile/piercing', fx: 'Fx_LongArrow', impact: 'Fx_Hit', cast: 'Fx_Muzzle', orient: 'Ground', color: '#8CFF6E',
      copy: '긴 화살이 일직선으로 적 4명을 꿰뚫어 공격력 120% 피해. 0.9초마다',
      target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
      stat: { Cooldown: 0.9, Range: 11, Damage: 120, Radius: 0.6, Speed: 24, MaxDistance: 13, MaxPerTarget: 4 },
      hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      opt: { Damage: ['사냥꾼의 촉', 0.3, 3], Cooldown: ['숙련된 시위', 0.1, 3], Count: ['꿰뚫는 화살', 1, 2], CritChance: ['매의 눈', 0.1, 2] } },

    { id: 2210, key: 'basic_grapeshot', name: 'Grapeshot', disp: '산탄 포격', owner: 10, family: 'Throw', slot: 'Tail',
      method: 'projectile/burst', fx: 'Fx_Pellet', impact: 'Fx_Hit', cast: 'Fx_Muzzle', orient: 'Billboard', color: '#FFC466',
      copy: '산탄 5발을 부채꼴로 뿌려 각 공격력 60% 피해. 1.1초마다',
      target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
      stat: { Cooldown: 1.1, Range: 8, Damage: 60, Radius: 0.6, Speed: 16, MaxDistance: 9, BurstCount: 5, BurstInterval: 0.02, BurstSpread: 28 },
      hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      opt: { Damage: ['화약 증량', 0.3, 3], Cooldown: ['재장전', 0.1, 3], Count: ['산탄 추가', 1, 2], CritDamage: ['근접 사격', 0.25, 2] } }
];

const TRAIT_DESC = {
    Damage: v => `피해량 +${Math.round(v * 100)}%`, Cooldown: v => `쿨타임 -${Math.round(v * 100)}%`,
    CritChance: v => `치명타 확률 +${Math.round(v * 100)}%p`, CritDamage: v => `치명타 피해량 +${Math.round(v * 100)}%p`,
    Area: v => `범위 +${Math.round(v * 100)}%`, Count: () => '개수 +1', Duration: v => `지속시간 +${Math.round(v * 100)}%`,
    Lifesteal: v => `처치 시 체력 +${v}`, MaxHp: v => `최대 체력 +${Math.round(v * 100)}%`, MoveSpeed: v => `이동속도 +${Math.round(v * 100)}%`
};

let optId = 7101;
let synId = 8201;
for (const s of HEROES)
{
    const color = s.color || FAM_COLOR[s.family];
    const slots = { onHit: s.hit };
    if (s.debuff)
        slots.onDebuff = { type: 'DebuffOnHit', params: { DebuffId: s.debuff } };

    write(`Skill/Master/${s.key}.js`,
`/** ${s.disp} — ${s.copy} */
module.exports = {
    id: ${s.id},
    name: '${s.name}',
    desc: '${s.copy}',
    maxLevel: 1,
    method: '${s.method}',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: ${js(s.target)},

    slots: ${js(slots)}
};
`);

    const values = Object.assign({ Projectile: s.fx, Impact: s.impact || s.fx, Cast: s.cast || s.fx, Orient: s.orient, Color: color }, s.stat);
    write(`Skill/Visual/${s.key}.js`,
`/** ${s.disp} — ${s.copy} */
module.exports = {
    color: '${color}',
    desc: "${s.disp} — ${s.copy}",
    values: ${JSON.stringify(values, null, 8).replace(/\n}$/, '\n    }')},
};
`);

    write(`Skill/Part/${s.key}.js`,
`/** 캐릭터 고유 공격 — 고유 파츠 (owner ${s.owner}). 보물상자에서 «고유 공격 강화»로 다시 나오면 옵션이 합쳐진다 */
module.exports = {
    id: ${s.id},
    name: '${s.name}',
    desc: '${s.copy}',
    displayName: '${s.disp}',
    slot: '${s.slot}',
    family: '${s.family}',
    appliesDebuff: '${s.debuff ? DEBUFF_ENUM[s.debuff] : 'None'}',
    skillId: ${s.id},
    weight: 10,
    owner: ${s.owner}
};
`);

    for (const [trait, [dn, v, max]] of Object.entries(s.opt))
    {
        write(`Skill/Option/${s.key}_${trait.toLowerCase()}.js`,
`module.exports = {
    id: ${optId++},
    name: '${s.key}_${trait.toLowerCase()}',
    desc: '${s.disp} — ${TRAIT_DESC[trait](v)}',
    displayName: '${dn}',
    partId: ${s.id},
    trait: '${trait}',
    value: ${v},
    maxStack: ${max}
};
`);
    }

    write(`Tables/synergy/mas_${s.key}.js`,
`/** ${s.disp} 통달 — 이 고유 공격의 증강을 4개 이상 모으면 자동 발동. */
module.exports = {
    id: ${synId++},
    name: 'mas_${s.key}',
    desc: '${s.disp} 증강 4개 — 피해 +80%',
    displayName: '${s.disp} 통달',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: ${s.id},
    reqOwnOptions: 4,
    reqDebuff: 'None',
    trait: 'Damage',
    value: 0.8,
    vsDebuff: 'None'
};
`);
}

// ── 2) 무기 29종: 이름 · 설명 (동작 · 수치는 그대로) ─────────────
const WEAPONS = {
    afterimage_dash: ['분신 질주', '4초간 빨라지고 지나간 자리에 분신이 남아 피해를 준다. 10초마다'],
    alpha_mode: ['광폭화', '8초간 공격속도 +40%. 16초마다'],
    bark_cone: ['서리 숨결', '전방 부채꼴 적에게 냉기를 뿜어 공격력 100% 피해 + 둔화. 2.5초마다'],
    bite_heal: ['흡혈 송곳니', '앞의 적을 물어 공격력 150% 피해, 물 때마다 체력을 조금 회복. 1.2초마다'],
    bone_mine: ['폭뢰 부적', '발밑에 폭뢰 부적을 붙인다. 적이 밟으면 반경 1.5m 공격력 200% 폭발. 4초마다'],
    bread_slice: ['대검 내려치기', '대검을 내려쳐 사방 반경 3m 에 공격력 80% 피해. 2.5초마다'],
    c_major_chord: ['유성 낙하', '유성 셋이 무작위 적 3명 위로 떨어져 반경 1.4m 공격력 90% 피해. 3초마다'],
    claw_volley: ['비도 연사', '전방으로 비도를 날려 각 공격력 60% 피해. 2초마다'],
    dig_roll: ['회전 검무', '검무를 추는 동안 0.5초마다 반경 1.5m 공격력 30% 피해 (상시 장판)'],
    ear_sonar: ['공명진', '반경 4m 원형 기파로 공격력 60% 피해. 2.5초마다'],
    eye_beam: ['빛의 창', '전방으로 빛의 창을 1.5초간 발사, 0.2초마다 공격력 25% 피해. 4초마다'],
    frisbee: ['차크람', '차크람이 흔들리며 날아가 공격력 110% 피해. 2초마다'],
    fur_barrier: ['수호 결계', '최대체력 15% 보호막을 8초간. 10초마다'],
    growl_aoe: ['사자후', '반경 3m 적에게 공격력 80% 피해 + 2초간 둔화. 3초마다'],
    ignore_taunt: ['부동심', '3초간 단단해지며 주변 적을 끌어당긴다. 12초마다'],
    kibble_bomb: ['화염 단지', '화염 단지를 던져 반경 2m 공격력 90% 피해 + 화상. 3초마다'],
    kibble_rain: ['화살비', '랜덤한 적 위치로 화살이 쏟아져 반경 1.5m 공격력 80% 피해. 5초마다'],
    metronome: ['맥동 룬', '1초마다 발밑 룬이 맥동해 반경 2.4m 공격력 35% 피해'],
    pack_chain: ['연쇄 번개', '번개가 가까운 적들을 연쇄로 타고 공격력 70% 피해. 2.5초마다'],
    scent_homing: ['추적 마탄', '가장 먼 적을 유도 추적해 공격력 130% 피해 + 받는 피해 증가. 2초마다'],
    slipper: ['쿠나이', '쿠나이를 던져 공격력 300% 피해. 5초마다'],
    static_fur: ['뇌운', '뇌운에서 번개가 떨어져 모든 적에게 공격력 40% 피해. 6초마다'],
    tail_copter: ['회오리 베기', '칼날을 돌려 반경 2m 에 0.4초마다 공격력 35% 피해. 8초마다'],
    tail_pierce: ['창 찌르기', '창을 내질러 적을 꿰뚫으며 공격력 120% 피해. 1.5초마다'],
    tail_whip: ['쇠사슬 채찍', '쇠사슬을 휘둘러 직선상 적에게 공격력 140% 피해. 2초마다'],
    tennis_ball: ['부메랑', '가장 가까운 적을 쫓는 부메랑, 공격력 100% 피해. 1.8초마다'],
    third_eye: ['심안 관통', '가장 가까운 적을 꿰뚫는 기로 공격력 180% 피해. 3초마다'],
    yawn: ['잠의 주문', '전방 90° 적을 3초간 크게 느리게 만든다 (피해 없음). 6초마다'],
    zoomies: ['질풍', '6초간 이동속도 +40%. 12초마다']
};

// 옵션 표시이름 — 여기 없는 옵션은 «무기이름 · 흡혈» 꼴만 아래에서 따라간다
const OPTION_RENAME = {
    c_major_chord_area: '큰 운석', c_major_chord_cooldown: '별자리 정렬', c_major_chord_count: '유성우', c_major_chord_damage: '무거운 운석',
    metronome_area: '넓은 룬', metronome_critchance: '정확한 맥동', metronome_damage: '각인 강화',
    // D-5 — 옛 개 소품 · 음악 이름이던 옵션을 무기 소재대로
    afterimage_dash_cooldown: '숨 고르기',
    afterimage_dash_damage: '날 선 분신',
    afterimage_dash_duration: '긴 분신',
    afterimage_dash_unique: '전력 질주',
    alpha_mode_cooldown: '빠른 재각성',
    alpha_mode_duration: '광기 연장',
    alpha_mode_unique: '피의 각성',
    bark_cone_area: '넓은 숨결',
    bark_cone_cooldown: '쉴 새 없이',
    bark_cone_damage: '혹한',
    bark_cone_unique: '얼음 송곳',
    bite_heal_bite_heal_critchance: '예리한 송곳니',
    bite_heal_bite_heal_critdamage: '급소 물기',
    bite_heal_cooldown: '굶주림',
    bite_heal_damage: '흡혈귀의 이빨',
    bite_heal_unique: '피의 갈증',
    bone_mine_area: '폭심 확장',
    bone_mine_bone_mine_critdamage: '정확한 인',
    bone_mine_count: '겹부적',
    bone_mine_damage: '강한 화약',
    bread_slice_area: '대지 가르기',
    bread_slice_cooldown: '칼날 갈기',
    bread_slice_damage: '무거운 대검',
    bread_slice_unique: '일도양단',
    c_major_chord_lifesteal: '별의 축복',
    c_major_chord_movespeed: '유성 걸음',
    claw_volley_claw_volley_critchance: '갈고리 칼날',
    claw_volley_cooldown: '연속 투척',
    claw_volley_count: '비도 하나 더',
    claw_volley_damage: '칼날 갈기',
    dig_roll_area: '넓은 검무',
    dig_roll_damage: '검기',
    dig_roll_duration: '끝없는 검무',
    dig_roll_unique: '신명',
    ear_sonar_area: '넓은 진',
    ear_sonar_cooldown: '빠른 각인',
    ear_sonar_damage: '공명 증폭',
    ear_sonar_ear_sonar_critchance: '정확한 파장',
    eye_beam_cooldown: '빛 모으기',
    eye_beam_damage: '성광 집중',
    eye_beam_duration: '긴 창',
    eye_beam_eye_beam_critdamage: '꿰뚫는 빛',
    frisbee_count: '쌍차크람',
    frisbee_damage: '황금 연마',
    frisbee_frisbee_critchance: '날 선 테두리',
    frisbee_unique: '되돌아오는 날',
    fur_barrier_cooldown: '빠른 결계',
    fur_barrier_duration: '질긴 결계',
    fur_barrier_unique: '이중 결계',
    growl_aoe_area: '메아리',
    growl_aoe_cooldown: '숨 고르기',
    growl_aoe_damage: '단전 단련',
    growl_aoe_unique: '산을 울리는 소리',
    ignore_taunt_cooldown: '명상',
    ignore_taunt_duration: '흔들림 없는 마음',
    ignore_taunt_unique: '태산',
    kibble_bomb_area: '큰 단지',
    kibble_bomb_cooldown: '빠른 투척',
    kibble_bomb_damage: '기름 증량',
    kibble_bomb_unique: '지옥불',
    kibble_rain_area: '넓은 화살비',
    kibble_rain_count: '화살 다발',
    kibble_rain_damage: '무거운 화살촉',
    metronome_lifesteal: '생명의 룬',
    metronome_maxhp: '수호 룬',
    pack_chain_cooldown: '쉴 틈 없는 번개',
    pack_chain_count: '긴 사슬',
    pack_chain_damage: '천둥 증폭',
    pack_chain_pack_chain_critchance: '날 선 번개',
    scent_homing_cooldown: '빠른 장전',
    scent_homing_count: '마탄 하나 더',
    scent_homing_damage: '마력 증폭',
    scent_homing_scent_homing_critdamage: '급소 추적',
    scent_homing_unique: '저주 각인',
    slipper_cooldown: '던지기 숙련',
    slipper_damage: '독 바른 날',
    slipper_slipper_critchance: '정확한 조준',
    slipper_slipper_critdamage: '정통으로',
    static_fur_cooldown: '빠른 뇌운',
    static_fur_damage: '천둥 충전',
    static_fur_static_fur_critchance: '번쩍',
    tail_copter_area: '넓은 회오리',
    tail_copter_cooldown: '빠른 재회전',
    tail_copter_damage: '날 세우기',
    tail_copter_duration: '오래 돌기',
    tail_pierce_cooldown: '빠른 찌르기',
    tail_pierce_count: '쌍창',
    tail_pierce_damage: '창술 단련',
    tail_pierce_tail_pierce_critdamage: '급소 꿰기',
    tail_pierce_unique: '관통 창',
    tail_whip_cooldown: '손목 스냅',
    tail_whip_count: '두 줄 사슬',
    tail_whip_damage: '무거운 사슬',
    tail_whip_unique: '긴 사슬',
    tennis_ball_cooldown: '빠른 회수',
    tennis_ball_count: '부메랑 하나 더',
    tennis_ball_damage: '단단한 나무',
    tennis_ball_tennis_ball_critdamage: '강속구',
    third_eye_cooldown: '빠른 시선',
    third_eye_count: '두 번 보기',
    third_eye_damage: '심안 단련',
    third_eye_third_eye_critdamage: '꿰뚫는 눈',
    yawn_area: '넓은 주문',
    yawn_cooldown: '잦은 주문',
    yawn_duration: '깊은 잠',
    yawn_unique: '전염되는 잠',
    zoomies_cooldown: '짧은 휴식',
    zoomies_duration: '긴 질풍',
    zoomies_unique: '바람의 가호'
};

function sub(t, re, rep)
{
    return t.replace(re, rep);
}

let renamed = 0;
for (const [key, [disp, copy]] of Object.entries(WEAPONS))
{
    const p = path.join(ROOT, 'Skill/Part', key + '.js');
    if (fs.existsSync(p) === false)
        continue;
    let t = fs.readFileSync(p, 'utf8');
    t = sub(t, /displayName: '[^']*'/, `displayName: '${disp}'`);
    t = sub(t, /desc: '[^']*'/, `desc: '${copy}'`);
    t = sub(t, /owner: [1-9][0-9]*/, 'owner: 0');
    fs.writeFileSync(p, t, 'utf8');

    const m = path.join(ROOT, 'Skill/Master', key + '.js');
    if (fs.existsSync(m))
    {
        let u = fs.readFileSync(m, 'utf8');
        u = sub(u, /^\/\*\*.*\*\//, `/** ${disp} — ${copy} */`);
        u = sub(u, /desc: '[^']*'/, `desc: '${copy}'`);
        fs.writeFileSync(m, u, 'utf8');
    }
    const v = path.join(ROOT, 'Skill/Visual', key + '.js');
    if (fs.existsSync(v))
    {
        let u = fs.readFileSync(v, 'utf8');
        u = sub(u, /^\/\*\*.*\*\//, `/** ${disp} — ${copy} */`);
        u = sub(u, /desc: "[^"]*"/, `desc: "${disp} — ${copy}"`);
        fs.writeFileSync(v, u, 'utf8');
    }
    const s = path.join(ROOT, 'Tables/synergy', 'mas_' + key + '.js');
    if (fs.existsSync(s))
    {
        let u = fs.readFileSync(s, 'utf8');
        u = sub(u, /displayName: '[^']*'/, `displayName: '${disp} 통달'`);
        u = sub(u, /desc: '[^—']*—/, `desc: '${disp} 증강 4개 —`);
        u = sub(u, /^\/\*\* [^—]*—/, `/** ${disp} 통달 —`);
        fs.writeFileSync(s, u, 'utf8');
    }
    renamed++;
}

// 무기 이펙트 (build_vfx.py 표) — [투사체, 착탄, 시전, 방향]. 색은 계열 색 그대로 (시너지 계열이 눈에 보이게)
const WEAPON_FX = {
    afterimage_dash: ['Fx_Afterimage', 'Fx_Afterimage', 'Fx_Gale', 'Ground'],
    alpha_mode: ['Fx_Rage', 'Fx_Rage', 'Fx_Rage', 'Billboard'],
    bark_cone: ['Fx_Frost', 'Fx_Frost', 'Fx_Frost', 'Ground'],
    bite_heal: ['Fx_Fang', 'Fx_Fang', 'Fx_Paw', 'Billboard'],
    bone_mine: ['Fx_Seal', 'Fx_Blast', 'Fx_Seal', 'Billboard'],
    bread_slice: ['Fx_Greatsword', 'Fx_Greatsword', 'Fx_Greatsword', 'Billboard'],
    c_major_chord: ['Fx_Meteor', 'Fx_Meteor', 'Fx_Meteor', 'Billboard'],
    claw_volley: ['Fx_Knife', 'Fx_Hit', 'Fx_Paw', 'Ground'],
    dig_roll: ['Fx_BladeSwirl', 'Fx_BladeSwirl', 'Fx_BladeSwirl', 'Ground'],
    ear_sonar: ['Fx_Rune', 'Fx_Rune', 'Fx_Rune', 'Ground'],
    eye_beam: ['Fx_LightRay', 'Fx_LightRay', 'Fx_LightRay', 'Ground'],
    frisbee: ['Fx_Chakram', 'Fx_HitBurst', 'Fx_Chakram', 'Billboard'],
    fur_barrier: ['Fx_Barrier', 'Fx_Barrier', 'Fx_Barrier', 'Billboard'],
    growl_aoe: ['Fx_Roar', 'Fx_Roar', 'Fx_Roar', 'Ground'],
    ignore_taunt: ['Fx_Steadfast', 'Fx_Steadfast', 'Fx_Steadfast', 'Billboard'],
    kibble_bomb: ['Fx_FireJar', 'Fx_Blast', 'Fx_FireJar', 'Billboard'],
    kibble_rain: ['Fx_ArrowRain', 'Fx_ArrowRain', 'Fx_ArrowRain', 'Billboard'],
    metronome: ['Fx_Rune', 'Fx_Rune', 'Fx_Rune', 'Ground'],
    pack_chain: ['Fx_Lightning', 'Fx_Hit', 'Fx_Lightning', 'Ground'],
    scent_homing: ['Fx_MagicShot', 'Fx_HitBurst', 'Fx_MagicShot', 'Billboard'],
    slipper: ['Fx_Kunai', 'Fx_Hit', 'Fx_Kunai', 'Ground'],
    static_fur: ['Fx_Thunder', 'Fx_Thunder', 'Fx_Thunder', 'Billboard'],
    tail_copter: ['Fx_BladeSwirl', 'Fx_BladeSwirl', 'Fx_BladeSwirl', 'Ground'],
    tail_pierce: ['Fx_Spear', 'Fx_Hit', 'Fx_Spear', 'Ground'],
    tail_whip: ['Fx_ChainWhip', 'Fx_Hit', 'Fx_ChainWhip', 'Ground'],
    tennis_ball: ['Fx_Boomerang', 'Fx_Hit', 'Fx_Boomerang', 'Billboard'],
    third_eye: ['Fx_QiBolt', 'Fx_Hit', 'Fx_QiBolt', 'Ground'],
    yawn: ['Fx_Sleep', 'Fx_Sleep', 'Fx_Sleep', 'Ground'],
    zoomies: ['Fx_Gale', 'Fx_Gale', 'Fx_Gale', 'Billboard']
};
let refx = 0;
for (const [key, [pr, im, ca, orient]] of Object.entries(WEAPON_FX))
{
    const v = path.join(ROOT, 'Skill/Visual', key + '.js');
    if (fs.existsSync(v) === false)
        continue;
    let u = fs.readFileSync(v, 'utf8');
    u = sub(u, /"Projectile": "[^"]*"/, `"Projectile": "${pr}"`);
    u = sub(u, /"Impact": "[^"]*"/, `"Impact": "${im}"`);
    u = sub(u, /"Cast": "[^"]*"/, `"Cast": "${ca}"`);
    u = sub(u, /"Orient": "[^"]*"/, `"Orient": "${orient}"`);
    fs.writeFileSync(v, u, 'utf8');
    refx++;
}
console.log(`[OK] 무기 이펙트 ${refx}종`);

// ── 3) 계열 · 연계 시너지 이름 ─────────────────────────────
const SYN_RENAME = {
    fam_aura_2: '투지', fam_aura_3: '전의 고양', fam_aura_4: '영웅의 기개',
    fam_field_2: '결계', fam_field_3: '진법', fam_field_4: '대진법',
    fam_gaze_2: '집중', fam_gaze_3: '매의 눈', fam_gaze_4: '심안',
    fam_melee_2: '연격', fam_melee_3: '검기', fam_melee_4: '무쌍',
    fam_throw_2: '투척술', fam_throw_3: '속사', fam_throw_4: '만개',
    fam_wave_2: '공명', fam_wave_3: '기공', fam_wave_4: '대기공',
    link_burn: '업화', link_expose: '약점 간파', link_slow: '빙결 족쇄'
};
for (const [key, disp] of Object.entries(SYN_RENAME))
{
    const f = path.join(ROOT, 'Tables/synergy', key + '.js');
    if (fs.existsSync(f) === false)
        continue;
    let s = fs.readFileSync(f, 'utf8');
    s = sub(s, /displayName: '[^']*'/, `displayName: '${disp}'`);
    fs.writeFileSync(f, s, 'utf8');
}

// ── 옵션 표시이름 · 설명 앞머리를 파츠의 지금 이름으로 ──────────────
{
    const nameById = {};
    for (const f of fs.readdirSync(path.join(ROOT, 'Skill/Part')))
    {
        const s = fs.readFileSync(path.join(ROOT, 'Skill/Part', f), 'utf8');
        const id = /id: (\d+)/.exec(s);
        const dn = /displayName: '([^']*)'/.exec(s);
        if (id && dn)
            nameById[id[1]] = dn[1];
    }
    let fixed = 0;
    for (const f of fs.readdirSync(path.join(ROOT, 'Skill/Option')))
    {
        const q = path.join(ROOT, 'Skill/Option', f);
        const s = fs.readFileSync(q, 'utf8');
        const pid = /partId: (\d+)/.exec(s);
        if (!pid || !nameById[pid[1]])
            continue;
        const part = nameById[pid[1]];
        let u = s.replace(/desc: '[^—']*—/, `desc: '${part} —`);
        u = u.replace(/displayName: '[^'·]* · (흡혈|튼튼|날쌘)'/, `displayName: '${part} · $1'`);
        const own = OPTION_RENAME[f.replace(/\.js$/, '')];
        if (own)
            u = u.replace(/displayName: '[^']*'/, `displayName: '${own}'`);
        if (u !== s)
        {
            fs.writeFileSync(q, u, 'utf8');
            fixed++;
        }
    }
    console.log(`[OK] 옵션 이름 · 설명 갱신 ${fixed}개`);
}

// ── 5) 보스 패턴 (TASKS D-4) — 보스별 C# 없이 데이터 스킬로 조립. 보스가 나올 때 맵의 bossSkills 를 장착한다
// 피해 % 는 보스 공격 수치(Tables/enemy boss · 라운드 · 맵 · 이벤트 배율 반영) 기준. 색은 위험 색 하나 (역할 색 예약)
const DANGER = '#FF5A2A';
const P = {
    fan: (n, spread, dmg, cd, fx, orient = 'Billboard') => ({ method: 'projectile/burst', fx, orient,
        target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
        stat: { Cooldown: cd, Range: 14, Damage: dmg, Radius: 0.6, Speed: 8, MaxDistance: 16, BurstCount: n, BurstInterval: 0, BurstSpread: spread },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } } }),
    lob: (r, dmg, cd, fx, impact) => ({ method: 'projectile/arc', fx, impact, orient: 'Billboard',
        target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
        stat: { Cooldown: cd, Range: 14, Damage: dmg, Radius: r, Speed: 7, ArcHeight: 4 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } } }),
    homing: (dmg, cd, fx) => ({ method: 'projectile/homing', fx, orient: 'Billboard',
        target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
        stat: { Cooldown: cd, Range: 16, Damage: dmg, Radius: 0.6, Speed: 5, MaxDistance: 18, TurnRate: 90 },
        hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } } }),
    zone: (r, dmg, dur, cd, fx) => ({ method: 'ground_field', fx, orient: 'Ground',
        target: { type: 'TargetSelf' },
        stat: { Cooldown: cd, Range: r, Damage: dmg, Radius: r, Duration: dur, DamageInterval: 0.5 },
        hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius', DamageInterval: '@stat.DamageInterval' } } })
};
// 맵키: [[스킬 id, 이름, 설명, 패턴], ...] — 맵키는 MAPS 와 같다
const BOSS_SKILLS = {
    snow_sakura: [[3001, '금쇠몽둥이 내려찍기', '몽둥이를 던져 반경 2.2m 폭발', P.lob(2.2, 150, 4, 'Fx_Meteor', 'Fx_Blast')],
                  [3002, '도깨비 불', '불덩이 5발 부채꼴', P.fan(5, 14, 60, 3, 'Fx_Fireball')]],
    palace_range: [[3003, '여우 구슬', '느리게 쫓아오는 여우불', P.homing(70, 1.5, 'Fx_MagicShot')],
                   [3004, '아홉 꼬리 불꽃', '사방 12발 원형 탄막', P.fan(12, 30, 50, 5, 'Fx_Fireball')]],
    moon_roof: [[3005, '그림자 쿠나이', '쿠나이 7발 부채꼴', P.fan(7, 10, 50, 2.5, 'Fx_Kunai', 'Ground')],
                [3006, '그림자 베기', '주위 반경 3m 2초 칼바람', P.zone(3, 40, 2, 6, 'Fx_BladeSwirl')]],
    bamboo_temple: [[3007, '나한 장풍', '주위 반경 3.5m 1.5초 기운', P.zone(3.5, 60, 1.5, 5, 'Fx_Palm')],
                    [3008, '철곤 내려찍기', '철곤을 던져 반경 2m 폭발', P.lob(2, 140, 4, 'Fx_Seal', 'Fx_Blast')]],
    mist_realm: [[3009, '추혼 부적', '쫓아오는 부적', P.homing(80, 1.8, 'Fx_Talisman')],
                 [3010, '부적 회오리', '사방 10발 원형 부적', P.fan(10, 36, 50, 5, 'Fx_Talisman')]],
    castle_square: [[3011, '타락의 검기', '검기 3발 부채꼴', P.fan(3, 20, 90, 3, 'Fx_QiBolt', 'Ground')],
                    [3012, '검은 회오리', '주위 반경 3m 1.5초 칼바람', P.zone(3, 50, 1.5, 5, 'Fx_BladeSwirl')]],
    mage_library: [[3013, '찢긴 책장', '사방 12발 원형 탄막', P.fan(12, 30, 50, 4, 'Fx_MagicShot')],
                   [3014, '저주 마탄', '쫓아오는 마탄', P.homing(80, 2, 'Fx_MagicShot')]],
    glacier_fjord: [[3015, '빙산 투척', '얼음 덩어리를 던져 반경 2.4m 폭발', P.lob(2.4, 150, 4, 'Fx_Meteor', 'Fx_Frost')],
                    [3016, '서리 장막', '주위 반경 3.5m 2초 냉기', P.zone(3.5, 40, 2, 6, 'Fx_Rune')]],
    giant_forest: [[3017, '독 포자 투척', '포자 덩어리를 던져 반경 2.2m 폭발', P.lob(2.2, 120, 3.5, 'Fx_FireJar', 'Fx_Blast')],
                   [3018, '뿌리 휘감기', '주위 반경 3m 1.5초 가시 뿌리', P.zone(3, 45, 1.5, 5, 'Fx_BladeSwirl')]],
    skull_deck: [[3019, '유령 산탄', '산탄 7발 부채꼴', P.fan(7, 12, 50, 2.5, 'Fx_Pellet')],
                 [3020, '유령 포격', '포탄을 던져 반경 2.2m 폭발', P.lob(2.2, 140, 4, 'Fx_Pellet', 'Fx_Blast')]]
};
for (const f of fs.readdirSync(path.join(ROOT, 'Skill/Master')))
{
    if (f.startsWith('boss_'))
    {
        remove(path.join('Skill/Master', f));
        remove(path.join('Skill/Visual', f));
    }
}
let bossSkillCount = 0;
for (const [mkey, list] of Object.entries(BOSS_SKILLS))
{
    for (const [id, disp, copy, pt] of list)
    {
        const key = `boss_${mkey}_${id}`;
        write(`Skill/Master/${key}.js`,
`/** 보스 패턴 ${disp} — ${copy} (맵 ${mkey}) */
module.exports = {
    id: ${id},
    name: '${key}',
    desc: '${disp} — ${copy}',
    maxLevel: 1,
    method: '${pt.method}',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: ${js(pt.target)},

    slots: ${js({ onHit: pt.hit })}
};
`);
        const values = Object.assign({ Projectile: pt.fx, Impact: pt.impact || 'Fx_Hit', Cast: pt.fx, Orient: pt.orient, Color: DANGER }, pt.stat);
        write(`Skill/Visual/${key}.js`,
`/** 보스 패턴 ${disp} — ${copy} */
module.exports = {
    color: '${DANGER}',
    desc: "${disp} — ${copy}",
    values: ${JSON.stringify(values, null, 8).replace(/\n}$/, '\n    }')},
};
`);
        bossSkillCount++;
    }
}
const bossSkillsOf = mkey => (BOSS_SKILLS[mkey] || []).map(x => x[0]).join(',');

// ── 4) 맵 10 (기획서 맵 표 순서) ─────────────────────────────
// 바닥 프리팹은 build_stage.py 가 Assets/_Art/Stage/<key>/ 에 쓰고 Art2DBuilder 가 obj_ground_<key> 로 굽는다
// 난이도 배율은 라운드 배율 위에 곱해진다. 챕터 첫 클리어 직전 성장(sim_tree.py power)의 공격 · 생존 비율 × 0.9 × (1 + 0.03 × (챕터-1)) — 볼트 기획서 진행 절
// 맵 표: 키 · 이름 · 출신 캐릭터 · 적 체력 배율 · 피해 배율 · 보스 · 한 줄 소개(로비 맵 선택에 보인다)
const MAPS = [
    ['snow_sakura', '벚꽃 설산', 1, 0.90, 0.90, '오니 장군', '눈 위로 꽃잎이 지는 산길. 오니 무리가 내려온다'],
    ['palace_range', '궁궐 활터', 2, 2.46, 1.81, '구미호', '과녁이 늘어선 궁궐 마당. 여우불이 담을 넘는다'],
    ['moon_roof', '달밤 기와지붕', 3, 2.32, 1.51, '그림자 쇼군', '보름달 아래 기와 위를 닌자들이 달린다'],
    ['bamboo_temple', '대나무 산사', 4, 5.29, 2.16, '철갑 나한', '종소리가 울리는 대숲 절. 돌부처가 눈을 뜬다'],
    ['mist_realm', '안개 선계', 5, 5.63, 2.12, '강시 대왕', '구름 위 선계. 부적을 붙인 강시가 뛰어온다'],
    ['castle_square', '성채 광장', 6, 5.98, 2.00, '타락 기사단장', '무너진 성의 광장. 해골 병사가 줄지어 선다'],
    ['mage_library', '마탑 서고', 7, 9.97, 2.95, '미믹 대마도서', '책장이 끝없는 마탑. 책이 이빨을 드러낸다'],
    ['glacier_fjord', '빙하 피오르', 8, 12.21, 2.49, '서리 거인', '얼어붙은 협만. 서리늑대가 떼로 몰려온다'],
    ['giant_forest', '거목 숲', 9, 18.40, 2.60, '썩은 엔트', '하늘을 가린 거목 숲. 버섯 정령이 깨어난다'],
    ['skull_deck', '해골섬 갑판', 10, 18.53, 3.03, '유령 선장', '해골섬으로 가는 유령선. 마지막 항해다']
];
for (const f of fs.existsSync(path.join(ROOT, 'Tables/map')) ? fs.readdirSync(path.join(ROOT, 'Tables/map')) : [])
    remove(path.join('Tables/map', f));
MAPS.forEach(([key, disp, hero, hp, dmg, boss, blurb], i) =>
{
    const order = i + 1;
    write(`Tables/map/m${String(order).padStart(2, '0')}_${key}.js`,
`/** 맵 ${order} — ${disp}. 출신 캐릭터 #${hero}. 앞 맵을 클리어하면 열린다 */
module.exports = {
    id: ${9000 + order},
    name: '${key}',
    desc: '${blurb}',
    displayName: '${disp}',
    order: ${order},
    groundKey: 'obj_ground_${key}',
    hero: ${hero},
    hpMul: ${hp},
    damageMul: ${dmg},
    boss: '${boss}',
    bossSkills: '${bossSkillsOf(key)}'
};
`);
});

console.log(`[OK] 보스 패턴 ${bossSkillCount}종`);
console.log(`[OK] 예전 기본 공격 파일 ${removed}개 삭제 · 고유 공격 ${HEROES.length}종 · 옵션 ${optId - 7101}개 · 통달 ${synId - 8201}개 · 이름 바꾼 무기 ${renamed}종`);
