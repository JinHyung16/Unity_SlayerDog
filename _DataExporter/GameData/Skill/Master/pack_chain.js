/** 연쇄 번개 — 번개가 가까운 적들을 연쇄로 타고 공격력 70% 피해. 2.5초마다 */
module.exports = {
    id: 2017,
    name: 'Pack Howl',
    desc: '번개가 가까운 적들을 연쇄로 타고 공격력 70% 피해. 2.5초마다',
    maxLevel: 1,
    method: 'instant',

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
        type: 'HitChain',
        params: {
            Damage: '@stat.Damage',
            Radius: '@stat.Radius',
            MaxBounces: 4
        }
    }
}
};
