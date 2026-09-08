/** 테니스공 물어와 — 가장 가까운 적을 쫓는 테니스공, 공격력 100% 피해. 1.8초마다 */
module.exports = {
    id: 2023,
    name: 'Tennis Ball',
    desc: '가장 가까운 적을 쫓는 테니스공, 공격력 100% 피해. 1.8초마다',
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
