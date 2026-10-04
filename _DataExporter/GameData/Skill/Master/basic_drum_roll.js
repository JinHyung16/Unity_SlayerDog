/** 드럼 롤 — 16분음표 5연타, 각 공격력 80% 피해. 0.9초마다 */
module.exports = {
    id: 2202,
    name: 'Drum Roll',
    desc: '16분음표 5연타, 각 공격력 80% 피해. 0.9초마다',
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
