/** 편전 — 짧은 화살 4발을 부채꼴로 쏘아 각 공격력 75% 피해. 1초마다 */
module.exports = {
    id: 2202,
    name: 'Pyeonjeon',
    desc: '짧은 화살 4발을 부채꼴로 쏘아 각 공격력 75% 피해. 1초마다',
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
