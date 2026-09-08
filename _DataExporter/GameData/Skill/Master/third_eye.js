/** 제3의 눈 — 가장 가까운 적을 관통하는 시선으로 공격력 180% 피해. 3초마다 */
module.exports = {
    id: 2016,
    name: 'Third Eye',
    desc: '가장 가까운 적을 관통하는 시선으로 공격력 180% 피해. 3초마다',
    maxLevel: 1,
    method: 'projectile/piercing',

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
