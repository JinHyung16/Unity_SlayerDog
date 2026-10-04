/** 베이스 드롭 — 묵직한 저음 음표가 적 2명을 뚫고 공격력 150% 피해. 0.9초마다 */
module.exports = {
    id: 2201,
    name: 'Bass Drop',
    desc: '묵직한 저음 음표가 적 2명을 뚫고 공격력 150% 피해. 0.9초마다',
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
