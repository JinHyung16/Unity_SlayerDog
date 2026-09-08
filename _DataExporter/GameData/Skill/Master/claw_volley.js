/** 발톱 세례 — 전방으로 발톱을 날려 각 공격력 60% 피해. 2초마다 */
module.exports = {
    id: 2006,
    name: 'Claw Volley',
    desc: '전방으로 발톱을 날려 각 공격력 60% 피해. 2초마다',
    maxLevel: 1,
    method: 'projectile/straight',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetCone',
    params: {
        Range: '@stat.Range',
        HalfAngle: 30
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
