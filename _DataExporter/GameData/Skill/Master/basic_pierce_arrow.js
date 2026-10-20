/** 관통 화살 — 긴 화살이 일직선으로 적 4명을 꿰뚫어 공격력 120% 피해. 0.9초마다 */
module.exports = {
    id: 2209,
    name: 'Piercing Arrow',
    desc: '긴 화살이 일직선으로 적 4명을 꿰뚫어 공격력 120% 피해. 0.9초마다',
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
