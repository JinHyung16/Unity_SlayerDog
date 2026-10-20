/** 공명진 — 반경 4m 원형 기파로 공격력 60% 피해. 2.5초마다 */
module.exports = {
    id: 2015,
    name: 'Ear Sonar',
    desc: '반경 4m 원형 기파로 공격력 60% 피해. 2.5초마다',
    maxLevel: 1,
    method: 'instant',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetSelf'
},

    slots: {
    onHit: {
        type: 'HitArea',
        params: {
            Damage: '@stat.Damage',
            Radius: '@stat.Radius'
        }
    }
}
};
