/** 보스 패턴 여우 구슬 — 느리게 쫓아오는 여우불 (맵 palace_range) */
module.exports = {
    id: 3003,
    name: 'boss_palace_range_3003',
    desc: '여우 구슬 — 느리게 쫓아오는 여우불',
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
