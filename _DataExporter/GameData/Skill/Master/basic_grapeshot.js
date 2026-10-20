/** 산탄 포격 — 산탄 5발을 부채꼴로 뿌려 각 공격력 60% 피해. 1.1초마다 */
module.exports = {
    id: 2210,
    name: 'Grapeshot',
    desc: '산탄 5발을 부채꼴로 뿌려 각 공격력 60% 피해. 1.1초마다',
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
