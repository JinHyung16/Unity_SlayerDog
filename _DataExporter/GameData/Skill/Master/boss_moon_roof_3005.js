/** 보스 패턴 그림자 쿠나이 — 쿠나이 7발 부채꼴 (맵 moon_roof) */
module.exports = {
    id: 3005,
    name: 'boss_moon_roof_3005',
    desc: '그림자 쿠나이 — 쿠나이 7발 부채꼴',
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
