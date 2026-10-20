/** 보스 패턴 빙산 투척 — 얼음 덩어리를 던져 반경 2.4m 폭발 (맵 glacier_fjord) */
module.exports = {
    id: 3015,
    name: 'boss_glacier_fjord_3015',
    desc: '빙산 투척 — 얼음 덩어리를 던져 반경 2.4m 폭발',
    maxLevel: 1,
    method: 'projectile/arc',

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
        type: 'HitArea',
        params: {
            Damage: '@stat.Damage',
            Radius: '@stat.Radius'
        }
    }
}
};
