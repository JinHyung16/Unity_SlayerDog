/** 수리검 — 흔들리며 날아가는 표창이 적 3명까지 맞혀 공격력 85% 피해. 0.8초마다 */
module.exports = {
    id: 2203,
    name: 'Shuriken',
    desc: '흔들리며 날아가는 표창이 적 3명까지 맞혀 공격력 85% 피해. 0.8초마다',
    maxLevel: 1,
    method: 'projectile/wave',

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
