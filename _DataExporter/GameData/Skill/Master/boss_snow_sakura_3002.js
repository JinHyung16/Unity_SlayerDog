/** 보스 패턴 도깨비 불 — 불덩이 5발 부채꼴 (맵 snow_sakura) */
module.exports = {
    id: 3002,
    name: 'boss_snow_sakura_3002',
    desc: '도깨비 불 — 불덩이 5발 부채꼴',
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
