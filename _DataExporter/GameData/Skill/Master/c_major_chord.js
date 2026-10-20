/** 유성 낙하 — 유성 셋이 무작위 적 3명 위로 떨어져 반경 1.4m 공격력 90% 피해. 3초마다 */
module.exports = {
    id: 2025,
    name: 'C Major Chord',
    desc: '유성 셋이 무작위 적 3명 위로 떨어져 반경 1.4m 공격력 90% 피해. 3초마다',
    maxLevel: 1,
    method: 'instant',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetRandom',
    params: {
        Range: '@stat.Range',
        Index: 3,
        MaxPerTarget: 3
    }
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
