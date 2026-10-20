/** 뇌운 — 뇌운에서 번개가 떨어져 모든 적에게 공격력 40% 피해. 6초마다 */
module.exports = {
    id: 2010,
    name: 'Static Fur',
    desc: '뇌운에서 번개가 떨어져 모든 적에게 공격력 40% 피해. 6초마다',
    maxLevel: 1,
    method: 'instant',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetAll',
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
