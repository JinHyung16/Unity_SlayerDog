/** 보스 패턴 부적 회오리 — 사방 10발 원형 부적 (맵 mist_realm) */
module.exports = {
    id: 3010,
    name: 'boss_mist_realm_3010',
    desc: '부적 회오리 — 사방 10발 원형 부적',
    maxLevel: 1,
    method: 'projectile/burst',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetNearest',
    params: {
        Range: '@stat.Range'
    }
},

    slots: {
    onHit: {
        type: 'HitSingle',
        params: {
            Damage: '@stat.Damage',
            Radius: '@stat.Radius'
        }
    }
}
};
