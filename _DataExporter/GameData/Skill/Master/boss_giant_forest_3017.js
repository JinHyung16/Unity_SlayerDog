/** 보스 패턴 독 포자 투척 — 포자 덩어리를 던져 반경 2.2m 폭발 (맵 giant_forest) */
module.exports = {
    id: 3017,
    name: 'boss_giant_forest_3017',
    desc: '독 포자 투척 — 포자 덩어리를 던져 반경 2.2m 폭발',
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
