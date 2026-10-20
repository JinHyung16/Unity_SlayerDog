/** 보스 패턴 철곤 내려찍기 — 철곤을 던져 반경 2m 폭발 (맵 bamboo_temple) */
module.exports = {
    id: 3008,
    name: 'boss_bamboo_temple_3008',
    desc: '철곤 내려찍기 — 철곤을 던져 반경 2m 폭발',
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
