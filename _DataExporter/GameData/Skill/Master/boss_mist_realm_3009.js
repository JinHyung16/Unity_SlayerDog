/** 보스 패턴 추혼 부적 — 쫓아오는 부적 (맵 mist_realm) */
module.exports = {
    id: 3009,
    name: 'boss_mist_realm_3009',
    desc: '추혼 부적 — 쫓아오는 부적',
    maxLevel: 1,
    method: 'projectile/homing',

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
