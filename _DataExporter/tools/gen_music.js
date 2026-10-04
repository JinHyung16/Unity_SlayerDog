/**
 * 음악 전투 데이터 스캐폴더 (.claude/design.md).
 *
 * 1) 주인공 5명의 기본 공격 = 고유 파츠 (owner 1~5) — Master · Visual · Part · Option · 통달 시너지
 * 2) 새 무기 2종 — 다장조 화음 · 메트로놈
 * 3) 기존 무기 27종 — 동작은 그대로, 이름(displayName) · 설명만 음악 테마로. 예전 고유 3종은 owner 0 (무기 풀로)
 * 4) 연발 발사 방식 Method/projectile/burst.js
 *
 * 현재 DB 구조(GameData/Skill/*)에 직접 쓴다. tools/gen_parts.js 는 옛 구조(modules/)용이라 쓰지 않는다.
 * 실행: node tools/gen_music.js && node export.js
 */
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', 'GameData');
const FAM_COLOR = { Wave: '#2DE6FF', Throw: '#9BFF3C', Field: '#A66BFF', Melee: '#FF3DA8', Gaze: '#4D8BFF', Aura: '#FFB03A' };
const FAM_KR = { Wave: '파동', Throw: '투척', Field: '장판', Melee: '근접', Gaze: '시선', Aura: '각성' };

function write(rel, body)
{
    const abs = path.join(ROOT, rel);
    fs.mkdirSync(path.dirname(abs), { recursive: true });
    fs.writeFileSync(abs, body.replace(/\r?\n/g, '\n'), 'utf8');
}

const js = (v, ind = 4) => JSON.stringify(v, null, ind).replace(/"([A-Za-z_][A-Za-z0-9_]*)":/g, '$1:').replace(/"/g, "'");

// ── 연발 Method ────────────────────────────────────────────
write('Skill/Method/projectile/burst.js',
`/**
 * 연발 직선 투사체. 한 번 발동에 BurstCount 발을 BurstInterval 간격 · BurstSpread 각도로 쏜다.
 * 개수 증강(CountAdd)은 BurstCount 에 더해진다 (LaunchExecutor).
 */
module.exports = {
    desc: '연발 투사체 — N 발을 간격 · 각도로 나눠 쏜다',
    slots: ['onHit'],
    nodes: [
        { slot: 'onHit' },
        { slot: 'onDebuff', optional: true },
        { type: 'DespawnAfterHits', params: { Value: 1 } },
        {
            type: 'LaunchStraight',
            params: {
                Speed: '@stat.Speed',
                MaxDistance: '@stat.MaxDistance',
                BurstCount: '@stat.BurstCount',
                BurstInterval: '@stat.BurstInterval',
                BurstSpread: '@stat.BurstSpread',
                Visual: '@visual.Projectile', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient'
            }
        }
    ]
};
`);

// ── 새 스킬 정의 ───────────────────────────────────────────
// opt: 이 파츠가 받는 옵션 (trait: [표시이름, 값, 최대중첩])
const NEW = [
    // 주인공 기본 공격 — owner = RunSelection.Heroes 순서
    { id: 2201, key: 'basic_bass_drop', name: 'Bass Drop', disp: '베이스 드롭', owner: 1, family: 'Wave', slot: 'Back',
      method: 'projectile/piercing', fx: 'Fx_NoteBass', orient: 'Billboard',
      copy: '묵직한 저음 음표가 적 2명을 뚫고 공격력 150% 피해. 0.9초마다',
      target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
      stat: { Cooldown: 0.9, Range: 9, Damage: 150, Radius: 0.8, Speed: 11, MaxDistance: 11, MaxPerTarget: 2 },
      hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      opt: { Damage: ['앰프 게인', 0.3, 3], Cooldown: ['빠른 피킹', 0.1, 3], Count: ['슬랩 관통', 1, 2], CritDamage: ['드롭 킥', 0.25, 2], Area: ['서브 베이스', 0.2, 2] } },

    { id: 2202, key: 'basic_drum_roll', name: 'Drum Roll', disp: '드럼 롤', owner: 2, family: 'Throw', slot: 'Back',
      method: 'projectile/burst', fx: 'Fx_NoteBeat', orient: 'Billboard',
      copy: '16분음표 5연타, 각 공격력 80% 피해. 0.9초마다',
      target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
      stat: { Cooldown: 0.9, Range: 9, Damage: 80, Radius: 0.6, Speed: 15, MaxDistance: 11, BurstCount: 5, BurstInterval: 0.07, BurstSpread: 9 },
      hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      opt: { Damage: ['스네어 튜닝', 0.3, 3], Cooldown: ['BPM 업', 0.1, 3], Count: ['한 박 더', 1, 2], CritChance: ['림샷', 0.1, 2] } },

    { id: 2203, key: 'basic_string_beam', name: 'String Beam', disp: '현의 선율', owner: 3, family: 'Gaze', slot: 'Head',
      method: 'beam', fx: 'Fx_StaffBeam', orient: 'Ground',
      copy: '오선지 레이저를 1초간 켜서 0.15초마다 공격력 34% 피해. 1.3초마다',
      target: { type: 'TargetNearestForward', params: { Range: '@stat.Range', HalfAngle: 60 } },
      stat: { Cooldown: 1.3, Range: 9, Damage: 34, Radius: 0.8, Length: 7, Width: 0.9, Duration: 1.0, DamageInterval: 0.15 },
      hit: { type: 'HitBeam', params: { Damage: '@stat.Damage', Length: '@stat.Length', Width: '@stat.Width', DamageInterval: '@stat.DamageInterval' } },
      opt: { Damage: ['송진 바르기', 0.3, 3], Cooldown: ['스피카토', 0.1, 3], Duration: ['롱 보잉', 0.25, 2], Area: ['넓은 활', 0.2, 2], CritDamage: ['비브라토', 0.25, 2] } },

    { id: 2204, key: 'basic_doremi', name: 'Do Re Mi', disp: '도레미 스케일', owner: 4, family: 'Melee', slot: 'Mouth',
      method: 'projectile/burst', fx: 'Fx_NoteScale', orient: 'Billboard',
      copy: '도 · 레 · 미 세 음이 부채꼴로 연달아 날아가 각 공격력 72% 피해. 1.1초마다',
      target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
      stat: { Cooldown: 1.1, Range: 7, Damage: 72, Radius: 0.7, Speed: 12, MaxDistance: 8, BurstCount: 3, BurstInterval: 0.07, BurstSpread: 16 },
      hit: { type: 'HitSingle', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      opt: { Damage: ['고음 발성', 0.3, 3], Cooldown: ['빠른 템포', 0.1, 3], Count: ['파 · 솔 추가', 1, 2], CritChance: ['절대음감', 0.1, 2] } },

    { id: 2205, key: 'basic_lullaby', name: 'Lullaby', disp: '자장가 온음표', owner: 5, family: 'Field', slot: 'Neck',
      method: 'projectile/homing', fx: 'Fx_NoteWhole', orient: 'Billboard',
      copy: '느린 온음표가 적을 쫓아가 터지며 반경 1.4m 공격력 125% 피해 + 졸음. 1.4초마다',
      target: { type: 'TargetNearest', params: { Range: '@stat.Range' } },
      stat: { Cooldown: 1.4, Range: 10, Damage: 125, Radius: 1.4, Speed: 6.5, MaxDistance: 13, TurnRate: 160 },
      hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      debuff: '#debuff/drowsy',
      opt: { Damage: ['포근한 음색', 0.3, 3], Cooldown: ['돌림노래', 0.1, 3], Area: ['메아리 이불', 0.2, 2], Duration: ['깊은 잠', 0.25, 2] } },

    // 새 무기
    { id: 2025, key: 'c_major_chord', name: 'C Major Chord', disp: '다장조 화음', owner: 0, family: 'Field', slot: 'Neck',
      method: 'instant', fx: 'Fx_Chord', orient: 'Billboard', weight: 10,
      copy: '도 · 미 · 솔 세 음이 무작위 적 3명 위로 떨어져 반경 1.4m 공격력 90% 피해. 3초마다',
      target: { type: 'TargetRandom', params: { Range: '@stat.Range', Index: 3, MaxPerTarget: 3 } },
      stat: { Cooldown: 3, Range: 10, Damage: 90, Radius: 1.4 },
      hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      opt: { Damage: ['장3도', 0.3, 3], Cooldown: ['빠른 코드 체인지', 0.1, 3], Area: ['오픈 보이싱', 0.2, 2], Count: ['7화음', 1, 2], Lifesteal: ['힐링 하모니', 1.4, 3], MoveSpeed: ['경쾌한 리듬', 0.06, 2] } },

    { id: 2026, key: 'metronome', name: 'Metronome', disp: '메트로놈', owner: 0, family: 'Wave', slot: 'Feet',
      method: 'instant', fx: 'Fx_Sonar', orient: 'Ground', weight: 10,
      copy: '1초 박자마다 발밑에서 반경 2.4m 공격력 35% 피해',
      target: { type: 'TargetSelf' },
      stat: { Cooldown: 1, Range: 2.4, Damage: 35, Radius: 2.4 },
      hit: { type: 'HitArea', params: { Damage: '@stat.Damage', Radius: '@stat.Radius' } },
      opt: { Damage: ['강박', 0.3, 3], Area: ['큰 진자', 0.2, 2], CritChance: ['정박', 0.1, 2], Lifesteal: ['심장 박동', 1.4, 3], MaxHp: ['튼튼한 태엽', 0.15, 2] } }
];

const TRAIT_DESC = {
    Damage: v => `피해량 +${Math.round(v * 100)}%`, Cooldown: v => `쿨타임 -${Math.round(v * 100)}%`,
    CritChance: v => `치명타 확률 +${Math.round(v * 100)}%p`, CritDamage: v => `치명타 피해량 +${Math.round(v * 100)}%p`,
    Area: v => `범위 +${Math.round(v * 100)}%`, Count: () => '개수 +1', Duration: v => `지속시간 +${Math.round(v * 100)}%`,
    Lifesteal: v => `처치 시 체력 +${v}`, MaxHp: v => `최대 체력 +${Math.round(v * 100)}%`, MoveSpeed: v => `이동속도 +${Math.round(v * 100)}%`
};

let optId = 7001;
let synId = 8101;
for (const s of NEW)
{
    const isBasic = s.owner > 0;
    const color = FAM_COLOR[s.family];
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

    const values = Object.assign({ Projectile: s.fx, Impact: s.fx, Cast: s.fx, Orient: s.orient, Color: color }, s.stat);
    write(`Skill/Visual/${s.key}.js`,
`/** ${s.disp} — ${s.copy} */
module.exports = {
    color: '${color}',
    desc: "${s.disp} — ${s.copy}",
    values: ${JSON.stringify(values, null, 8).replace(/\n}$/, '\n    }')},
};
`);

    write(`Skill/Part/${s.key}.js`,
`/** ${isBasic ? '주인공 기본 공격 — 고유 파츠 (owner ' + s.owner + '). 스피커에서 «앰프»로 다시 나오면 옵션이 합쳐진다' : '무기 파츠'} */
module.exports = {
    id: ${s.id},
    name: '${s.name}',
    desc: '${s.copy}',
    displayName: '${s.disp}',
    slot: '${s.slot}',
    family: '${s.family}',
    appliesDebuff: '${s.debuff ? 'SlowReduce' : 'None'}',
    skillId: ${s.id},
    weight: ${s.weight || 10},
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
`/** ${s.disp} 통달 — 이 ${isBasic ? '기본 공격' : '무기'}의 증강을 4개 이상 모으면 자동 발동. */
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

// ── 기존 무기: 음악 테마 이름 · 예전 고유는 풀로 ──────────────
const RENAME = {
    bark_cone: '포르테 샤우트', growl_aoe: '베이스 그로울', yawn: '자장가 하품', static_fur: '일렉 스파크',
    ear_sonar: '서브우퍼 펄스', pack_chain: '아르페지오 연쇄',
    kibble_rain: '음표 소나기', kibble_bomb: '심벌 크래시', frisbee: 'LP 판 날리기', tennis_ball: '탬버린 부메랑', slipper: '캐스터네츠 딱!',
    dig_roll: '브레이크 댄스', bone_mine: '베이스 드럼 매설', tail_copter: '턴테이블 스핀', afterimage_dash: '글리산도 대시',
    bite_heal: '스타카토 바이트', claw_volley: '기타 피크 연사', tail_pierce: '현 가르기', tail_whip: '지휘봉 채찍', bread_slice: '건반 슬램',
    eye_beam: '스포트라이트 빔', scent_homing: '추적 멜로디', third_eye: '하이 C 관통음',
    zoomies: '업템포', alpha_mode: '클라이맥스 모드', fur_barrier: '앰프 보호막', ignore_taunt: '스테이지 장악'
};
// 음악 테마 설명 — 동작 · 수치 문구는 그대로 두고 소재 단어만 바꾼다
const WORDS = [
    ['사료가 낙하해', '음표가 쏟아져'], ['사료를 던져', '심벌을 던져'], ['원반이', 'LP 판이'], ['테니스공', '탬버린'],
    ['슬리퍼를 던져', '캐스터네츠를 튕겨'], ['구르는 동안', '춤추는 동안'], ['뼈다귀를 묻는다', '베이스 드럼을 깐다'],
    ['꼬리를 돌려', '턴테이블을 돌려'], ['발톱을 날려', '기타 피크를 날려'], ['반월형 기를', '현의 파동을'], ['꼬리를 후려쳐', '지휘봉을 휘둘러'],
    ['엉덩이를 흔들어', '건반을 내리쳐'], ['눈빛 빔을', '스포트라이트를'], ['울음이', '아르페지오가'], ['털에서 정전기가', '일렉 기타에서 스파크가'],
    ['앞의 적을 물어', '스타카토로 앞의 적을 물어']
];

let renamed = 0;
for (const [key, disp] of Object.entries(RENAME))
{
    const p = path.join(ROOT, 'Skill/Part', key + '.js');
    if (fs.existsSync(p) === false)
        continue;
    let t = fs.readFileSync(p, 'utf8');
    t = t.replace(/displayName: '[^']*'/, `displayName: '${disp}'`);
    t = t.replace(/owner: [1-9]/, 'owner: 0');
    for (const [a, b] of WORDS)
        t = t.split(a).join(b);
    fs.writeFileSync(p, t, 'utf8');

    for (const dir of ['Skill/Master', 'Skill/Visual'])
    {
        const q = path.join(ROOT, dir, key + '.js');
        if (fs.existsSync(q) === false)
            continue;
        let u = fs.readFileSync(q, 'utf8');
        for (const [a, b] of WORDS)
            u = u.split(a).join(b);
        fs.writeFileSync(q, u, 'utf8');
    }
    // 통달 시너지 이름도 따라간다 — 안 바꾸면 «왈왈 통달» 같은 옛 이름이 HUD 에 뜬다 (QA1 발견)
    const m = path.join(ROOT, 'Tables/synergy', 'mas_' + key + '.js');
    if (fs.existsSync(m))
    {
        let s = fs.readFileSync(m, 'utf8');
        s = s.replace(/displayName: '[^']*'/, `displayName: '${disp} 통달'`);
        s = s.replace(/desc: '[^—']*—/, `desc: '${disp} 증강 4개 —`);
        s = s.replace(/^\/\*\* [^—]*—/, `/** ${disp} 통달 —`);
        fs.writeFileSync(m, s, 'utf8');
    }
    renamed++;
}

// ── 계열 · 연계 시너지: 음악 이름 ─────────────────────────────
const SYN_RENAME = {
    fam_aura_2: '롱 톤', fam_aura_3: '무대 체력', fam_aura_4: '앙코르',
    fam_field_2: '서스테인', fam_field_3: '스텝 댄스', fam_field_4: '플로어 장악',
    fam_gaze_2: '포커스 라이트', fam_gaze_3: '시선 강탈', fam_gaze_4: '솔로 파트',
    fam_melee_2: '스타카토', fam_melee_3: '열창', fam_melee_4: '헤드뱅잉',
    fam_throw_2: '연습 무대', fam_throw_3: '가벼운 스텝', fam_throw_4: '풀 스윙',
    link_burn: '뜨거운 무대', link_expose: '불협화음', link_slow: '슬로우 템포'
};
for (const [key, disp] of Object.entries(SYN_RENAME))
{
    const f = path.join(ROOT, 'Tables/synergy', key + '.js');
    if (fs.existsSync(f) === false)
        continue;
    let s = fs.readFileSync(f, 'utf8');
    s = s.replace(/displayName: '[^']*'/, `displayName: '${disp}'`);
    fs.writeFileSync(f, s, 'utf8');
}

// ── 옵션 설명 앞머리를 파츠의 지금 이름으로 — «사료 유성우 — 피해량 +30%» 같은 옛 이름이 레벨업 카드에 뜬다 (10-04) ──
{
    const nameById = {};
    for (const f of fs.readdirSync(path.join(ROOT, 'Skill/Part')))
    {
        const s = fs.readFileSync(path.join(ROOT, 'Skill/Part', f), 'utf8');
        const id = /id: (\d+)/.exec(s), dn = /displayName: '([^']*)'/.exec(s);
        if (id && dn)
            nameById[id[1]] = dn[1];
    }
    let fixed = 0;
    for (const f of fs.readdirSync(path.join(ROOT, 'Skill/Option')))
    {
        const q = path.join(ROOT, 'Skill/Option', f);
        let s = fs.readFileSync(q, 'utf8');
        const pid = /partId: (\d+)/.exec(s);
        if (!pid || !nameById[pid[1]])
            continue;
        const u = s.replace(/desc: '[^—']*—/, `desc: '${nameById[pid[1]]} —`);
        if (u !== s) { fs.writeFileSync(q, u, 'utf8'); fixed++; }
    }
    console.log(`[OK] 옵션 설명 이름 갱신 ${fixed}개`);
}

console.log(`[OK] 새 스킬 ${NEW.length}종 (기본 공격 5 · 무기 2) · 옵션 ${optId - 7001}개 · 통달 ${synId - 8101}개 · 이름 바꾼 무기 ${renamed}종`);
