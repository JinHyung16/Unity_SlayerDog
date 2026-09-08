/** 원반 물어오기 — 원반이 흔들리며 날아가 공격력 110% 피해. 2초마다 */
module.exports = {
    id: 2022,
    name: 'Frisbee',
    desc: '원반이 흔들리며 날아가 공격력 110% 피해. 2초마다',
    maxLevel: 1,
    method: 'projectile/wave',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetRandom',
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
